using System.Text;
using InkWell.AuthService.Data;
using InkWell.AuthService.Repositories;
using InkWell.AuthService.Services;
using InkWell.Shared.Logging;
using InkWell.Shared.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseCustomSerilog("AuthService");

// 1. Configure Entity Framework Core with SQL Server
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Register Repositories & Services for Dependency Injection
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 3. Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Allow HTTP for development
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Configure MassTransit and RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<InkWell.AuthService.Consumers.PostCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitSettings = builder.Configuration.GetSection("RabbitMQ");
        var host = rabbitSettings["Host"];
        var username = rabbitSettings["Username"];
        var password = rabbitSettings["Password"];
        var virtualHost = rabbitSettings["VirtualHost"] ?? username;

        if (string.IsNullOrEmpty(host) || host == "localhost")
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });
        }
        else
        {
            // For Cloud or other external hosts
            cfg.Host(host, 5671, virtualHost!, h =>
            {
                h.Username(username!);
                h.Password(password!);
                h.UseSsl(s => s.Protocol = System.Security.Authentication.SslProtocols.Tls12);
            });
        }

        // Force explicit exchange name for notifications
        cfg.Message<NotificationEvent>(m => m.SetEntityName("inkwell-notification-exchange"));

        cfg.ConfigureEndpoints(context);
    });
});

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Add Security Definition for JWT Bearer Token to show the "Authorize" button
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter your token in the text input below.\r\n\r\nExample: \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Ensure Database schema is correct (Auto-fix for missing columns)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AuthDbContext>();
        
        // Comprehensive check for all core columns
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'Username' AND Object_ID = OBJECT_ID('Users')) BEGIN ALTER TABLE Users ADD Username NVARCHAR(MAX) NULL; END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'Email' AND Object_ID = OBJECT_ID('Users')) BEGIN ALTER TABLE Users ADD Email NVARCHAR(MAX) NULL; END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'FullName' AND Object_ID = OBJECT_ID('Users')) BEGIN ALTER TABLE Users ADD FullName NVARCHAR(MAX) NULL; END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'ProfilePictureUrl' AND Object_ID = OBJECT_ID('Users')) BEGIN ALTER TABLE Users ADD ProfilePictureUrl NVARCHAR(MAX) NULL; END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'PasswordHash' AND Object_ID = OBJECT_ID('Users')) BEGIN ALTER TABLE Users ADD PasswordHash NVARCHAR(MAX) NULL; END
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'CreatedAt' AND Object_ID = OBJECT_ID('Users')) BEGIN ALTER TABLE Users ADD CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(); END
        ";
        context.Database.ExecuteSqlRaw(sql);

        // Seed manual Admin user as requested
        var adminEmail = "shiva11@gmail.com";
        var adminUser = await context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (adminUser == null)
        {
            var newUser = new InkWell.AuthService.Models.User
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                Username = "admin_shiva",
                FullName = "Admin Shiva",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("@Shiva123"),
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(newUser);
            
            // Assign Admin Role (Id = 1)
            context.UserRoles.Add(new InkWell.AuthService.Models.UserRole { UserId = newUser.Id, RoleId = 1 });
            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while checking/fixing the database schema or seeding admin.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSharedLogging();

// REMOVED app.UseHttpsRedirection() as it causes 404/Connection Refused behind Gateway

// 4. Use Authentication & Authorization Middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Manually start MassTransit bus
var busControl = app.Services.GetRequiredService<IBusControl>();
await busControl.StartAsync();

app.Run();

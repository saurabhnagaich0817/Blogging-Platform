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
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? 
                       Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION") ??
                       builder.Configuration.GetConnectionString("DefaultConnection");

// 🔥 Ultra-Sanitize: Remove quotes, spaces, and invisible newlines
connectionString = connectionString?.Trim(' ', '"', '\'', '\r', '\n');

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("❌ ERROR: Connection String is COMPLETELY MISSING!");
}

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Register Repositories & Services for Dependency Injection
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 3. Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "InkWellSuperSecretKey2026_KeepItSafe!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// Configure MassTransit and RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<InkWell.AuthService.Consumers.PostCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitSettings = builder.Configuration.GetSection("RabbitMQ");
        var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ__HOST") ?? rabbitSettings["Host"] ?? "localhost";
        var rabbitUser = Environment.GetEnvironmentVariable("RABBITMQ__USERNAME") ?? rabbitSettings["Username"] ?? "guest";
        var rabbitPass = Environment.GetEnvironmentVariable("RABBITMQ__PASSWORD") ?? rabbitSettings["Password"] ?? "guest";
        var vHost = Environment.GetEnvironmentVariable("RABBITMQ__VIRTUALHOST") ?? rabbitSettings["VirtualHost"] ?? "/";

        var isLocal = rabbitHost == "localhost" || rabbitHost == "127.0.0.1";
        var vHostEncoded = (vHost == "/" || string.IsNullOrEmpty(vHost)) ? "%2f" : vHost.TrimStart('/');
        var port = isLocal ? 5672 : 5671;
        var scheme = isLocal ? "rabbitmq" : "amqps";
        var uriString = $"{scheme}://{rabbitHost}:{port}/{vHostEncoded}";

        cfg.Host(new Uri(uriString), h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);

            if (!isLocal)
            {
                h.UseSsl(s =>
                {
                    s.Protocol = System.Security.Authentication.SslProtocols.Tls12;
                });
            }
        });

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

// 5. Configure Swagger - Enabled for all environments on HF
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InkWell Auth API v1");
    c.RoutePrefix = string.Empty; // Set Swagger as the root page
});

// Ensure Database schema is correct
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // 🧪 DEBUG: Check string start
        if (!string.IsNullOrEmpty(connectionString))
        {
            var display = connectionString.Length > 20 ? connectionString.Substring(0, 20) + "..." : connectionString;
            Console.WriteLine($"🔍 DEBUG: Using Connection String starting with: [{display}]");
        }
        
        var context = services.GetRequiredService<AuthDbContext>();
        
        // 🚀 Step 1: Ensure Tables exist using a combination of EF and Raw SQL
        context.Database.EnsureCreated();
        
        var createTablesSql = @"
            IF OBJECT_ID('Roles', 'U') IS NULL BEGIN
                CREATE TABLE Roles (Id INT PRIMARY KEY, Name NVARCHAR(MAX) NOT NULL);
                INSERT INTO Roles (Id, Name) VALUES (1, 'Admin'), (2, 'Author'), (3, 'Reader');
            END
            IF OBJECT_ID('Users', 'U') IS NULL BEGIN
                CREATE TABLE Users (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    Username NVARCHAR(MAX) NULL,
                    Email NVARCHAR(450) UNIQUE NOT NULL,
                    FullName NVARCHAR(MAX) NULL,
                    PasswordHash NVARCHAR(MAX) NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    Bio NVARCHAR(MAX) NULL,
                    ProfilePictureUrl NVARCHAR(MAX) NULL,
                    GitHubUrl NVARCHAR(MAX) NULL,
                    LinkedInUrl NVARCHAR(MAX) NULL,
                    PhoneNumber NVARCHAR(MAX) NULL
                );
            END
            IF OBJECT_ID('UserRoles', 'U') IS NULL BEGIN
                CREATE TABLE UserRoles (
                    UserId UNIQUEIDENTIFIER NOT NULL,
                    RoleId INT NOT NULL,
                    PRIMARY KEY (UserId, RoleId)
                );
            END
        ";
        context.Database.ExecuteSqlRaw(createTablesSql);
        
        // Comprehensive check for all core columns (Already exist if newly created, but safe to run)
        var sql = @"
            IF OBJECT_ID('Users', 'U') IS NOT NULL BEGIN
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'Username' AND Object_ID = OBJECT_ID('Users')) BEGIN ALTER TABLE Users ADD Username NVARCHAR(MAX) NULL; END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'Email' AND Object_ID = OBJECT_ID('Users')) BEGIN ALTER TABLE Users ADD Email NVARCHAR(450) NULL; END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'FullName' AND Object_ID = OBJECT_ID('Users')) BEGIN ALTER TABLE Users ADD FullName NVARCHAR(MAX) NULL; END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'ProfilePictureUrl' AND Object_ID = OBJECT_ID('Users')) BEGIN ALTER TABLE Users ADD ProfilePictureUrl NVARCHAR(MAX) NULL; END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'PasswordHash' AND Object_ID = OBJECT_ID('Users')) BEGIN ALTER TABLE Users ADD PasswordHash NVARCHAR(MAX) NULL; END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'CreatedAt' AND Object_ID = OBJECT_ID('Users')) BEGIN ALTER TABLE Users ADD CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(); END
            END
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

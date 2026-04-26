using System.Text;
using InkWell.MediaService.Data;
using InkWell.MediaService.Repositories;
using InkWell.MediaService.Services;
using InkWell.Shared.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseCustomSerilog("MediaService");

// 1. Database Configuration
builder.Services.AddDbContext<MediaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Dependency Injection
builder.Services.AddHttpContextAccessor(); // Needed for URL generation
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IMediaService, MediaService>();

// Inject Storage Provider (Clean Architecture allows swapping Local for AWS S3 here)
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

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
    options.RequireHttpsMetadata = false; 
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
            cfg.Host(host, 5671, virtualHost!, h =>
            {
                h.Username(username!);
                h.Password(password!);
                h.UseSsl(s => 
                {
                    s.Protocol = System.Security.Authentication.SslProtocols.Tls12;
                });
            });
        }
    });
});


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();

// 4. Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "InkWell Media API", 
        Version = "v1",
        Description = "Microservice handling file uploads, serving static files, and S3-ready file storage logic." 
    });
    
    c.EnableAnnotations();

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Ensure Database schema is correct
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MediaDbContext>();
        // Ensure MediaItems table exists
        context.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE Name = 'MediaItems')
            BEGIN
                CREATE TABLE MediaItems (
                    MediaId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                    UploaderId UNIQUEIDENTIFIER NOT NULL,
                    FileName NVARCHAR(255) NOT NULL,
                    OriginalName NVARCHAR(255) NOT NULL,
                    Url NVARCHAR(MAX) NOT NULL,
                    MimeType NVARCHAR(100) NOT NULL,
                    SizeKb BIGINT NOT NULL,
                    AltText NVARCHAR(255),
                    LinkedPostId UNIQUEIDENTIFIER NULL,
                    UploadedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    IsDeleted BIT NOT NULL DEFAULT 0
                );
            END
        ");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while checking/fixing the database schema.");
    }
}




if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Correlation ID + request timing
app.UseSharedLogging();

// 5. Enable Static Files for Local Storage 
// This allows returning images via the browser (e.g. http://localhost:5050/uploads/uuid.jpg)
app.UseStaticFiles(); 

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

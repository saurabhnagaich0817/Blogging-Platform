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
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? 
                       Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION") ??
                       builder.Configuration.GetConnectionString("DefaultConnection");

// 🔥 Ultra-Sanitize: Remove quotes, spaces, and invisible newlines
connectionString = connectionString?.Trim(' ', '"', '\'', '\r', '\n');

builder.Services.AddDbContext<MediaDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Dependency Injection
builder.Services.AddHttpContextAccessor(); // Needed for URL generation
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IMediaService, MediaService>();

// Inject Storage Provider (Clean Architecture allows swapping Local for AWS S3 here)
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// 3. Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = builder.Configuration["Jwt:Key"] ?? "InkWellSuperSecretKey2026_KeepItSafe!";
var key = Encoding.ASCII.GetBytes(jwtKey);

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
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Configure MassTransit and RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ__HOST") ?? "localhost";
        var rabbitUser = Environment.GetEnvironmentVariable("RABBITMQ__USERNAME") ?? "guest";
        var rabbitPass = Environment.GetEnvironmentVariable("RABBITMQ__PASSWORD") ?? "guest";
        var vHost = (rabbitHost == "localhost" || string.IsNullOrEmpty(rabbitUser)) ? "/" : rabbitUser;

        // 🚀 Use Uri based approach for RabbitMQ
        var rabbitUri = rabbitHost == "localhost" 
            ? new Uri($"rabbitmq://{rabbitHost}/{vHost}")
            : new Uri($"rabbitmqs://{rabbitHost}/{vHost}");

        Console.WriteLine($"🌐 Attempting RabbitMQ Connection to: {rabbitUri}");

        cfg.Host(rabbitUri, h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });
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

// 5. Configure Swagger - Enabled for all environments on HF
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InkWell Media API v1");
    c.RoutePrefix = string.Empty; // Set Swagger as the root page
});

// Ensure Database schema is correct
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MediaDbContext>();
        
        // 🚀 Create tables if they don't exist
        context.Database.EnsureCreated();

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

app.UseSharedLogging();

// 6. Enable Static Files for Local Storage 
app.UseStaticFiles(); 

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

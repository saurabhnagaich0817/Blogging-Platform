using System.Text;
using InkWell.CommentService.Data;
using InkWell.CommentService.Repositories;
using InkWell.CommentService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using InkWell.Shared.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseCustomSerilog("CommentService");

// 1. Database Configuration
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? 
                       Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION") ??
                       builder.Configuration.GetConnectionString("DefaultConnection");

// 🔥 Ultra-Sanitize: Remove quotes, spaces, and invisible newlines
connectionString = connectionString?.Trim(' ', '"', '\'', '\r', '\n');

builder.Services.AddDbContext<CommentDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Dependency Injection
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentService, CommentService>();

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
    });
});


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();

// 4. Configure Swagger with JWT support & Annotations
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "InkWell Commenting & Community Service", 
        Version = "v1",
        Description = "Microservice handling user discussions, comment threading, moderation, and engagement metrics for stories.",
        Contact = new OpenApiContact { Name = "InkWell Dev", Email = "dev@inkwell.com" }
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InkWell Comment API v1");
    c.RoutePrefix = string.Empty; // Set Swagger as the root page
});

// Ensure Database schema is correct
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CommentDbContext>();
        
        // 🚀 Create tables if they don't exist
        context.Database.EnsureCreated();

        // Ensure Comments table exists and has correct schema (Robust Auto-Fix)
        context.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Comments')
            BEGIN
                CREATE TABLE [dbo].[Comments] (
                    [CommentId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                    [PostId] UNIQUEIDENTIFIER NOT NULL,
                    [AuthorId] UNIQUEIDENTIFIER NOT NULL,
                    [AuthorName] NVARCHAR(MAX) NULL,
                    [ParentCommentId] UNIQUEIDENTIFIER NULL,
                    [Content] NVARCHAR(MAX) NOT NULL,
                    [LikesCount] INT NOT NULL DEFAULT 0,
                    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Approved',
                    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedAt] DATETIME2 NULL
                );
            END
            ELSE
            BEGIN
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Comments]') AND name = 'AuthorName')
                BEGIN
                    ALTER TABLE [dbo].[Comments] ADD [AuthorName] NVARCHAR(MAX) NULL;
                END
                
                -- Fill existing NULLs with a default name
                EXEC('UPDATE [dbo].[Comments] SET [AuthorName] = ''User'' WHERE [AuthorName] IS NULL');
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

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<InkWell.Shared.Middlewares.GlobalExceptionMiddleware>();

app.MapControllers();

app.Run();

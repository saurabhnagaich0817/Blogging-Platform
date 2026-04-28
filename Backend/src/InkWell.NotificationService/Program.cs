using InkWell.Shared.Logging;
using InkWell.NotificationService.Consumers;
using InkWell.NotificationService.Data;
using InkWell.Shared.Events;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using InkWell.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseCustomSerilog("NotificationService");

// Add services to the container.
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "InkWell Notification API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

// 1. Database Configuration
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? 
                       Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION") ??
                       builder.Configuration.GetConnectionString("DefaultConnection");

// Ultra-Sanitize: Remove quotes, spaces, and invisible newlines
connectionString = connectionString?.Trim(' ', '"', '\'', '\r', '\n');

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "InkWellSuperSecretKey2026_KeepItSafe!";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<UserRegisteredConsumer>();
    x.AddConsumer<PostCreatedConsumer>();
    x.AddConsumer<CommentAddedConsumer>();
    x.AddConsumer<PostLikedConsumer>();
    x.AddConsumer<UserSubscribedConsumer>();
    x.AddConsumer<NotificationEventConsumer>();
    x.AddConsumer<UserLoggedInConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitSettings = builder.Configuration.GetSection("RabbitMQ");
        var rabbitUrl = Environment.GetEnvironmentVariable("RABBITMQ__URL");
        var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ__HOST") ?? rabbitSettings["Host"] ?? "localhost";
        var rabbitUser = Environment.GetEnvironmentVariable("RABBITMQ__USERNAME") ?? rabbitSettings["Username"] ?? "guest";
        var rabbitPass = Environment.GetEnvironmentVariable("RABBITMQ__PASSWORD") ?? rabbitSettings["Password"] ?? "guest";
        var vHost = Environment.GetEnvironmentVariable("RABBITMQ__VIRTUALHOST") ?? rabbitSettings["VirtualHost"] ?? "/";

        if (!string.IsNullOrEmpty(rabbitUrl))
        {
            cfg.Host(new Uri(rabbitUrl));
        }
        else
        {
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
        }

        // Force explicit exchange name for notifications
        cfg.Message<NotificationEvent>(m => m.SetEntityName("inkwell-notification-exchange"));

        // Use automatic endpoint configuration for better reliability
        cfg.ConfigureEndpoints(context);
    });
});


var app = builder.Build();

// 5. Configure Swagger - Enabled for all environments on HF
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InkWell Notification API v1");
    c.RoutePrefix = string.Empty; // Set Swagger as the root page
});

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<NotificationDbContext>();
        
        // 🚀 Create tables if they don't exist
        context.Database.EnsureCreated();

        var sql = @"
            IF OBJECT_ID(N'[NotificationUsers]', N'U') IS NULL
            BEGIN
                CREATE TABLE [NotificationUsers] (
                    [UserId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    [FullName] NVARCHAR(256) NULL,
                    [Role] NVARCHAR(32) NULL,
                    [LastSeen] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                );
            END;

            IF OBJECT_ID(N'[Notifications]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Notifications] (
                    [NotificationId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    [UserId] UNIQUEIDENTIFIER NOT NULL,
                    [Title] NVARCHAR(256) NULL,
                    [Message] NVARCHAR(MAX) NULL,
                    [Type] NVARCHAR(32) NULL,
                    [IsRead] BIT NOT NULL DEFAULT 0,
                    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [RelatedId] NVARCHAR(128) NULL
                );
            END;

            IF COL_LENGTH('Notifications', 'Title') IS NULL
            BEGIN
                ALTER TABLE Notifications ADD Title NVARCHAR(256) NULL;
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_UserId_CreatedAt' AND object_id = OBJECT_ID('Notifications'))
            BEGIN
                CREATE INDEX [IX_Notifications_UserId_CreatedAt] ON [Notifications]([UserId], [CreatedAt]);
            END;
        ";
        context.Database.ExecuteSqlRaw(sql);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while checking/fixing the notification database schema.");
    }
}

app.UseSharedLogging();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<InkWell.Shared.Middlewares.GlobalExceptionMiddleware>();

app.MapControllers();

app.Run();

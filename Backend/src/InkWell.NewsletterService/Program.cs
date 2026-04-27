using System.Text;
using InkWell.NewsletterService.Data;
using InkWell.NewsletterService.Repositories;
using InkWell.NewsletterService.Services;
using InkWell.Shared.Logging;
using InkWell.Shared.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using InkWell.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseCustomSerilog("NewsletterService");

// Add services to the container.
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

// 1. Database Configuration
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? 
                       Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION") ??
                       builder.Configuration.GetConnectionString("DefaultConnection");

// 🔥 Ultra-Sanitize: Remove quotes, spaces, and invisible newlines
connectionString = connectionString?.Trim(' ', '"', '\'', '\r', '\n');

builder.Services.AddDbContext<NewsletterDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<ISubscriberRepository, SubscriberRepository>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();

// Configure MassTransit and RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<InkWell.NewsletterService.Consumers.PostCreatedConsumer>();

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

        cfg.ReceiveEndpoint("newsletter-post-created", e =>
        {
            e.ConfigureConsumer<InkWell.NewsletterService.Consumers.PostCreatedConsumer>(context);
        });

        // Force explicit exchange name for notifications if needed
        cfg.Message<NotificationEvent>(m => m.SetEntityName("inkwell-notification-exchange"));
    });
});


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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 4. Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "InkWell Newsletter API", 
        Version = "v1",
        Description = "Microservice handling email subscriptions via a secure Double Opt-In workflow." 
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InkWell Newsletter API v1");
    c.RoutePrefix = string.Empty; // Set Swagger as the root page
});

// Ensure Database schema is correct
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<NewsletterDbContext>();
        
        // 🚀 Create tables if they don't exist
        context.Database.EnsureCreated();

        var sql = @"
            IF OBJECT_ID(N'[Subscribers]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Subscribers] (
                    [SubscriberId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    [Email] NVARCHAR(450) NOT NULL,
                    [FullName] NVARCHAR(MAX) NULL,
                    [UserId] UNIQUEIDENTIFIER NULL,
                    [Status] INT NOT NULL,
                    [Token] NVARCHAR(MAX) NULL,
                    [SubscribedAt] DATETIME2 NOT NULL,
                    [UnsubscribedAt] DATETIME2 NULL
                );
                CREATE INDEX [IX_Subscribers_Email] ON [Subscribers]([Email]);
            END;
        ";
        context.Database.ExecuteSqlRaw(sql);
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

app.MapControllers();

app.Run();

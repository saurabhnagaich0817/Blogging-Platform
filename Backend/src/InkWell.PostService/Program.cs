using System.Text;
using InkWell.PostService.Data;
using InkWell.PostService.Repositories;
using InkWell.PostService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using InkWell.Shared.Logging;
using InkWell.Shared.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseCustomSerilog("PostService");

// 1. Database Configuration
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? 
                       Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION") ??
                       builder.Configuration.GetConnectionString("DefaultConnection");

// 🔥 Ultra-Sanitize: Remove quotes, spaces, and invisible newlines
connectionString = connectionString?.Trim(' ', '"', '\'', '\r', '\n');

builder.Services.AddDbContext<PostDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Dependency Injection
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostService, PostService>();

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

        cfg.Host(new Uri(rabbitHost == "localhost" ? $"rabbitmq://{rabbitHost}/{vHost}" : $"amqps://{rabbitHost}:5671/{vHost}"), h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
            
            if (rabbitHost != "localhost")
            {
                h.UseSsl(s => 
                {
                    s.Protocol = System.Security.Authentication.SslProtocols.Tls12;
                });
            }
        });

        // Force explicit exchange name for notifications
        cfg.Message<NotificationEvent>(m => m.SetEntityName("inkwell-notification-exchange"));
    });
});


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();

// 4. Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InkWell Post API v1");
    c.RoutePrefix = string.Empty; // Set Swagger as the root page
});

// Ensure Database schema is correct
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<PostDbContext>();
        
        // 🚀 Create tables if they don't exist
        context.Database.EnsureCreated();

        var sql = @"
            IF OBJECT_ID(N'[Posts]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Posts] (
                    [PostId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    [AuthorId] UNIQUEIDENTIFIER NOT NULL,
                    [AuthorName] NVARCHAR(MAX) NULL,
                    [Title] NVARCHAR(160) NOT NULL,
                    [Slug] NVARCHAR(180) NOT NULL,
                    [Content] NVARCHAR(MAX) NOT NULL,
                    [ImageUrl] NVARCHAR(2048) NOT NULL DEFAULT '',
                    [LikesCount] INT NOT NULL DEFAULT 0,
                    [Status] NVARCHAR(32) NOT NULL DEFAULT 'Published',
                    [CreatedAt] DATETIME2 NOT NULL,
                    [UpdatedAt] DATETIME2 NULL
                );
            END;

            IF OBJECT_ID(N'[Likes]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Likes] (
                    [LikeId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    [PostId] UNIQUEIDENTIFIER NOT NULL,
                    [UserId] UNIQUEIDENTIFIER NOT NULL,
                    [LikedAt] DATETIME2 NOT NULL
                );
            END;

            IF OBJECT_ID(N'[SavedPosts]', N'U') IS NULL
            BEGIN
                CREATE TABLE [SavedPosts] (
                    [SavedPostId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    [PostId] UNIQUEIDENTIFIER NOT NULL,
                    [UserId] UNIQUEIDENTIFIER NOT NULL,
                    [SavedAt] DATETIME2 NOT NULL
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'CategoryId' AND Object_ID = OBJECT_ID('Posts'))
                BEGIN ALTER TABLE [Posts] ADD [CategoryId] UNIQUEIDENTIFIER NULL; END;
            ELSE
                BEGIN 
                    -- If it's INT, drop it and recreate as UNIQUEIDENTIFIER
                    IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types t ON c.system_type_id = t.system_type_id WHERE c.Name = 'CategoryId' AND c.Object_ID = OBJECT_ID('Posts') AND t.name = 'int')
                    BEGIN
                        ALTER TABLE [Posts] DROP COLUMN [CategoryId];
                        ALTER TABLE [Posts] ADD [CategoryId] UNIQUEIDENTIFIER NULL;
                    END
                END;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'CategoryName' AND Object_ID = OBJECT_ID('Posts'))
                BEGIN ALTER TABLE [Posts] ADD [CategoryName] NVARCHAR(MAX) NULL; END;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'Slug' AND Object_ID = OBJECT_ID('Posts'))
                BEGIN ALTER TABLE [Posts] ADD [Slug] NVARCHAR(180) NOT NULL DEFAULT ''; END;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'LikesCount' AND Object_ID = OBJECT_ID('Posts'))
                BEGIN ALTER TABLE [Posts] ADD [LikesCount] INT NOT NULL DEFAULT 0; END;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'Status' AND Object_ID = OBJECT_ID('Posts'))
                BEGIN ALTER TABLE [Posts] ADD [Status] NVARCHAR(32) NOT NULL DEFAULT 'Published'; END;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'ImageUrl' AND Object_ID = OBJECT_ID('Posts'))
                BEGIN ALTER TABLE [Posts] ADD [ImageUrl] NVARCHAR(2048) NOT NULL DEFAULT ''; END;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'CreatedAt' AND Object_ID = OBJECT_ID('Posts'))
                BEGIN ALTER TABLE [Posts] ADD [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(); END;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'UpdatedAt' AND Object_ID = OBJECT_ID('Posts'))
                BEGIN ALTER TABLE [Posts] ADD [UpdatedAt] DATETIME2 NULL; END;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'AuthorName' AND Object_ID = OBJECT_ID('Posts'))
                BEGIN ALTER TABLE [Posts] ADD [AuthorName] NVARCHAR(MAX) NULL; END;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'AuthorEmail' AND Object_ID = OBJECT_ID('Posts'))
                BEGIN ALTER TABLE [Posts] ADD [AuthorEmail] NVARCHAR(MAX) NULL; END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Posts_Slug' AND object_id = OBJECT_ID('Posts'))
            BEGIN
                CREATE UNIQUE INDEX [IX_Posts_Slug] ON [Posts]([Slug]);
            END;

            -- Update existing posts with a default email for testing
            UPDATE [Posts] SET [AuthorEmail] = 'saurabhnagaich27@gmail.com' WHERE [AuthorEmail] IS NULL OR [AuthorEmail] = '';
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

// Map Middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<InkWell.Shared.Middlewares.GlobalExceptionMiddleware>();

app.MapControllers();

app.Run();

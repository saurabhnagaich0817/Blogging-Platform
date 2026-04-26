using System.Text;
using InkWell.Shared.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// InkWell API Gateway - Powered by Microsoft YARP (Yet Another Reverse Proxy)
/// This gateway serves as the single entry point for all frontend requests.
/// It handles Authentication (JWT), CORS, and dynamic routing to backend microservices.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// 1. Configure Serilog for the Gateway
builder.Host.UseCustomSerilog("ApiGateway");

// 2. Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "InkWell API Gateway",
        Version = "v1",
        Description = "Centralized API Gateway for the InkWell Blogging Platform. " +
                      "Authentication is handled at Gateway level. " +
                      "All protected routes require a Bearer token."
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token. Gateway validates it and forwards the request to the correct microservice."
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
            Array.Empty<string>()
        }
    });
});

// 3. JWT Authentication — uses the SAME key/issuer/audience as AuthService
var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key") ?? 
             builder.Configuration["Jwt:Key"] ?? 
             "InkWellSuperSecretKey2026_KeepItSafe!";
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

builder.Services.AddAuthorization();

// 4a. CORS — Allow Angular frontend
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? 
                    new[] { "http://localhost:4200", "https://inkwell-frontend.netlify.app" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 4b. YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// 5. Swagger UI - Enabled for all environments on HF
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InkWell API Gateway v1");
    c.RoutePrefix = string.Empty; // Set Swagger as the root page
});

// 6. Middleware pipeline
app.UseCors("AllowAngular");
app.UseSharedLogging();
app.UseAuthentication();
app.UseAuthorization();

// 7. Health check / test endpoint — verify gateway is running
app.MapGet("/test", () => Results.Ok(new
{
    status = "Gateway Running ✅",
    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
    live_services = new[]
    {
        "AuthService     → https://saurabh0817-inkwell-auth-service.hf.space",
        "PostService     → https://saurabh0817-inkwell-post-service.hf.space",
        "CommentService  → https://saurabh0817-inkwell-comment-service.hf.space",
        "CategoryService → https://saurabh0817-inkwell-category-service.hf.space",
        "MediaService    → https://saurabh0817-inkwell-media-service.hf.space",
        "NewsletterService → https://saurabh0817-inkwell-newsletter-service.hf.space",
        "NotificationService → https://saurabh0817-inkwell-notification-service.hf.space"
    },
    gateway_endpoints = new[]
    {
        "Posts      → /api/posts",
        "Categories → /api/categories",
        "Auth Test  → /api/auth/test"
    }
}));

app.MapReverseProxy();
app.Run();

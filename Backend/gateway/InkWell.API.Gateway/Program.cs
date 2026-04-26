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
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // HTTP is fine in development
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

builder.Services.AddAuthorization();

// 4a. CORS — Allow Angular frontend (localhost:4200) to call the Gateway
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins)   // Angular dev server + Netlify
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 4b. YARP Reverse Proxy — loads routes from appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// 5. Swagger UI (development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "InkWell API Gateway v1");
        c.RoutePrefix = "swagger";
    });
}

// 6. Middleware pipeline (ORDER MATTERS!)
// UseHttpsRedirection is NOT used — services run on HTTP locally
app.UseCors("AllowAngular");     // Must be BEFORE Auth — allow Angular requests
app.UseSharedLogging();          // Correlation ID + request timing logs
app.UseAuthentication();         // Validate JWT token
app.UseAuthorization();          // Check [Authorize] policies

// 7. Health check / test endpoint — verify gateway is running
app.MapGet("/test", () => Results.Ok(new
{
    status = "Gateway Running ✅",
    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
    services = new[]
    {
        "AuthService     → http://localhost:5010/swagger",
        "PostService     → http://localhost:5020/swagger",
        "CommentService  → http://localhost:5030/swagger",
        "CategoryService → http://localhost:5040/swagger",
        "MediaService    → http://localhost:5050/swagger",
        "NewsletterService → http://localhost:5070/swagger",
        "Gateway Swagger → http://localhost:5000/swagger"
    },
    note = "Use each service Swagger to test. Route all calls via http://localhost:5000/api/..."
}));

// The Reverse Proxy takes incoming requests, matches them against configuration in appsettings.json,
// and forwards them to the appropriate backend microservice.
app.MapReverseProxy();

app.Run();

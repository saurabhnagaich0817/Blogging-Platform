using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace InkWell.Shared.Logging;

public static class LoggingExtensions
{
    public static IHostBuilder UseCustomSerilog(this IHostBuilder hostBuilder, string serviceName)
    {
        return hostBuilder.UseSerilog((context, loggerConfiguration) =>
        {
            var connectionString = context.Configuration.GetConnectionString("DefaultConnection") ?? 
                                   context.Configuration.GetConnectionString("AuthDbConnection") ??
                                   context.Configuration.GetConnectionString("PostDbConnection") ??
                                   context.Configuration.GetConnectionString("CommentDbConnection") ??
                                   context.Configuration.GetConnectionString("CategoryDbConnection");

            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)
                .WriteTo.Console()
                .WriteTo.File(
                    path: $"Logs/log-{serviceName}-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] [{CorrelationId}] [{ServiceName}] {Message:lj}{NewLine}{Exception}"
                );

            if (!string.IsNullOrEmpty(connectionString))
            {
                loggerConfiguration.WriteTo.MSSqlServer(
                    connectionString: connectionString,
                    sinkOptions: new MSSqlServerSinkOptions
                    {
                        TableName = "Logs",
                        AutoCreateSqlTable = true
                    });
            }
        });
    }

    public static IApplicationBuilder UseSharedLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        return app;
    }
}

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
            // 🛡️ Robust Connection String Retrieval
            var connectionString = context.Configuration.GetConnectionString("DefaultConnection") ?? 
                                   context.Configuration.GetConnectionString("AuthDbConnection") ??
                                   context.Configuration.GetConnectionString("PostDbConnection");

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

            // Only attempt DB logging if the string looks valid
            if (!string.IsNullOrWhiteSpace(connectionString) && connectionString.Contains("="))
            {
                try 
                {
                    loggerConfiguration.WriteTo.MSSqlServer(
                        connectionString: connectionString,
                        sinkOptions: new MSSqlServerSinkOptions
                        {
                            TableName = "Logs",
                            AutoCreateSqlTable = true
                        });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Serilog MSSqlServer Sink failed to initialize: {ex.Message}");
                }
            }
            else 
            {
                Console.WriteLine("ℹ️ Database logging disabled: Connection string is missing or invalid.");
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

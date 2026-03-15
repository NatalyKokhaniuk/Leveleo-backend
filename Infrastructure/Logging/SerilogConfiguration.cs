using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace LeveLEO.Infrastructure.Logging;

/// <summary>
/// Конфігурація Serilog для логування
/// </summary>
public static class SerilogConfiguration
{
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
       
        builder.Logging.ClearProviders();

        
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "LeveLEO")
            //.Enrich.WithThreadId()

            
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
            )

            
            .WriteTo.File(
                path: "logs/leveleo-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: 10_000_000 // 10MB per file
            )

            
            .WriteTo.File(
                path: "logs/errors/leveleo-errors-.log",
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90, 
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
            )

            
            .WriteTo.File(
                new CompactJsonFormatter(),
                path: "logs/json/leveleo-.json",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7 
            )

            // ДОДАТКОВО: Seq для Production 
            // .WriteTo.Seq("http://localhost:5341")

            .CreateLogger();

        Log.Logger = logger;

        builder.Host.UseSerilog();

        Log.Information("✅ Serilog configured successfully");
    }

    /// <summary>
    /// Middleware для логування HTTP запитів
    /// </summary>
    public static void UseSerilogRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                var requestHost = httpContext.Request.Host.Value;
                if (!string.IsNullOrEmpty(requestHost))
                {
                    diagnosticContext.Set("RequestHost", requestHost);
                }
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());

                var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    diagnosticContext.Set("UserId", userId);
                }
            };

            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null)
                    return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 500)
                    return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 400)
                    return LogEventLevel.Warning;
                if (elapsed > 5000)
                    return LogEventLevel.Warning; 
                return LogEventLevel.Information;
            };
        });
    }

    /// <summary>
    /// Закрити та відправити всі логи при завершенні програми
    /// </summary>
    public static void EnsureSerilogFlushed()
    {
        Log.CloseAndFlush();
    }
}

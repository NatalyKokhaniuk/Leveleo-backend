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
        // Очищаємо дефолтні провайдери
        builder.Logging.ClearProviders();

        // Налаштовуємо Serilog
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "LeveLEO")
            //.Enrich.WithThreadId()

            // Консоль - для Development (зручний формат)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
            )

            // Файли - для всіх середовищ (детальний формат)
            .WriteTo.File(
                path: "logs/leveleo-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: 10_000_000 // 10MB per file
            )

            // Окремий файл для помилок (Error та Critical)
            .WriteTo.File(
                path: "logs/errors/leveleo-errors-.log",
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90, // Зберігаємо помилки довше
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
            )

            // JSON формат для машинної обробки (опціонально)
            .WriteTo.File(
                new CompactJsonFormatter(),
                path: "logs/json/leveleo-.json",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7 // Тримаємо тиждень JSON логів
            )

            // ДОДАТКОВО: Seq для Production (розкоментуй якщо використовуєш Seq)
            // .WriteTo.Seq("http://localhost:5341")

            .CreateLogger();

        // Встановлюємо як глобальний logger
        Log.Logger = logger;

        // Використовуємо Serilog як провайдер логування
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
            // Кастомізуємо повідомлення
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            // Збагачуємо лог додатковою інформацією
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                var requestHost = httpContext.Request.Host.Value;
                if (!string.IsNullOrEmpty(requestHost))
                {
                    diagnosticContext.Set("RequestHost", requestHost);
                }
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());

                // Якщо є UserId в claims
                var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    diagnosticContext.Set("UserId", userId);
                }
            };

            // Рівень логування в залежності від статусу відповіді
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null)
                    return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 500)
                    return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 400)
                    return LogEventLevel.Warning;
                if (elapsed > 5000)
                    return LogEventLevel.Warning; // Повільні запити > 5 сек
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
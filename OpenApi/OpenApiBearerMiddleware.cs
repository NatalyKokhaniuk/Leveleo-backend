using System.Text.Json.Nodes;

namespace LeveLEO.OpenApi;

public class OpenApiBearerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Перехоплюємо тільки OpenAPI JSON
        if (context.Request.Path.StartsWithSegments("/openapi/v1.json"))
        {
            // Читаємо оригінальний JSON
            var originalBody = context.Response.Body;

            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            await next(context); // викликаємо наступний middleware, щоб згенерувати JSON

            memStream.Position = 0;
            var json = await new StreamReader(memStream).ReadToEndAsync();

            // Парсимо в JsonNode
            var doc = JsonNode.Parse(json)!.AsObject();

            // Додаємо securityScheme
            var components = doc["components"] ?? new JsonObject();
            var schemas = components["securitySchemes"] ?? new JsonObject();
            components["securitySchemes"] = schemas;

            var secSchemes = schemas.AsObject();
            secSchemes["Bearer"] = new JsonObject
            {
                ["type"] = "http",
                ["scheme"] = "bearer",
                ["bearerFormat"] = "JWT",
                ["description"] = "JWT Authorization header using the Bearer scheme."
            };

            doc["components"] = components;

            // Додаємо глобальний security requirement
            doc["security"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["Bearer"] = new JsonArray()
                    }
                };

            // Відправляємо назад модифікований JSON
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(doc.ToJsonString());

            return;
        }

        await next(context);
    }
}

// Extension метод для простого додавання middleware
public static class OpenApiBearerMiddlewareExtensions
{
    public static IApplicationBuilder UseOpenApiBearerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OpenApiBearerMiddleware>();
    }
}
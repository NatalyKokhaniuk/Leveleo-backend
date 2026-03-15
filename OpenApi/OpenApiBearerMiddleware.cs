using System.Text.Json.Nodes;

namespace LeveLEO.OpenApi;

public class OpenApiBearerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/openapi/v1.json"))
        {
            var originalBody = context.Response.Body;

            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            await next(context);

            memStream.Position = 0;
            var json = await new StreamReader(memStream).ReadToEndAsync();

            var doc = JsonNode.Parse(json)!.AsObject();

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

            doc["security"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["Bearer"] = new JsonArray()
                    }
                };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(doc.ToJsonString());

            return;
        }

        await next(context);
    }
}

// Extension метод додавання middleware
public static class OpenApiBearerMiddlewareExtensions
{
    public static IApplicationBuilder UseOpenApiBearerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OpenApiBearerMiddleware>();
    }
}

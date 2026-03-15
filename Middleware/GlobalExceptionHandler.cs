using LeveLEO.Exceptions;
using System.Net;
using System.Text.Json;

namespace LeveLEO.Middleware;

public class GlobalExceptionHandler(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            await HandleApiExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleUnexpectedExceptionAsync(context, ex);
        }
    }

    private static Task HandleApiExceptionAsync(HttpContext context, ApiException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception.StatusCode;

        var response = new
        {
            errorCode = exception.ErrorCode,
            message = exception.Message,
            errors = exception.Errors
        };

        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }

    private static Task HandleUnexpectedExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            errorCode = "INTERNAL_ERROR",
            message = exception.Message,   
            stackTrace = exception.StackTrace 
        };

        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
}

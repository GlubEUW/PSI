using Api.Exceptions;
using Api.Utils;

namespace Api.Middleware;

public class ExceptionLoggingMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestPath = context.Request.Path;
        ExceptionLogger.LogException(exception, $"Request: {requestPath}");

        var (statusCode, userMessage) = exception switch
        {
            InvalidMoveException => (StatusCodes.Status400BadRequest, exception.Message),
            GameNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            LobbyFullException => (StatusCodes.Status409Conflict, exception.Message),
            PlayerNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            GameException => (StatusCodes.Status400BadRequest, exception.Message),
            LobbyException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new
        {
            error = userMessage,
            timestamp = DateTime.UtcNow,
            path = context.Request.Path.ToString()
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}
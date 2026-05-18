using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Bmz.LabTests.API.Middlewares;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, ex);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Некорректный запрос"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Не найдено"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Не авторизован"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Конфликт"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Конфликт конкурентного изменения"),
            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var details = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode >= 500 ? "Произошла непредвиденная ошибка сервера." : exception.Message
        };

        var json = JsonSerializer.Serialize(details);
        await context.Response.WriteAsync(json);
    }
}

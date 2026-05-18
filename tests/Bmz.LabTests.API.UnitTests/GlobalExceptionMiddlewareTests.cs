using Bmz.LabTests.API.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace Bmz.LabTests.API.UnitTests;

public sealed class GlobalExceptionMiddlewareTests
{
    [Theory]
    [InlineData(typeof(ArgumentException), StatusCodes.Status400BadRequest, "Некорректный запрос")]
    [InlineData(typeof(KeyNotFoundException), StatusCodes.Status404NotFound, "Не найдено")]
    [InlineData(typeof(UnauthorizedAccessException), StatusCodes.Status401Unauthorized, "Не авторизован")]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status409Conflict, "Конфликт")]
    [InlineData(typeof(Exception), StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера")]
    public async Task Invoke_WhenExceptionThrown_WritesProblemDetails(Type exceptionType, int statusCode, string expectedTitle)
    {
        var message = "test-message";
        var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw exception;
        var middleware = new GlobalExceptionMiddleware(next, NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(statusCode);
        context.Response.ContentType.Should().Be("application/problem+json");

        context.Response.Body.Position = 0;
        var details = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        details.Should().NotBeNull();
        details!.Status.Should().Be(statusCode);
        details.Title.Should().Be(expectedTitle);

        if (statusCode >= 500)
        {
            details.Detail.Should().Be("Произошла непредвиденная ошибка сервера.");
        }
        else
        {
            details.Detail.Should().Be(message);
        }
    }
}


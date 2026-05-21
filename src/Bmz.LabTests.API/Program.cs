using Bmz.LabTests.Application;
using Bmz.LabTests.Application.Abstractions.Persistence;
using Bmz.LabTests.API.Middlewares;
using Bmz.LabTests.Infrastructure.Auth;
using Bmz.LabTests.Infrastructure.DependencyInjection;
using Bmz.LabTests.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

/// <summary>
/// Точка входа в приложение API.
/// Настраивает сервисы, аутентификацию, CORS, логирование и конвейер обработки запросов.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "Frontend";

// --- Настройка сервисов (DI) ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Настройка автоматической валидации через FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Регистрация слоев приложения и инфраструктуры
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- Настройка JWT аутентификации ---
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Параметры JWT не настроены.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };
    });

builder.Services.AddAuthorization();

// --- Настройка CORS (Cross-Origin Resource Sharing) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var allowedOrigins = configuredOrigins
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // По умолчанию для разработки разрешаем Vite-сервер
        if (allowedOrigins.Length == 0 && builder.Environment.IsDevelopment())
        {
            allowedOrigins =
            [
                "http://localhost:5173",
                "https://localhost:5173"
            ];
        }

        if (allowedOrigins.Length == 0)
            throw new InvalidOperationException("Разрешенные origin для CORS не настроены.");

        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins(allowedOrigins);
    });
});

// --- Настройка Health Checks (мониторинг состояния) ---
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database", tags: ["db", "sql"]);

var app = builder.Build();

// --- Конфигурация конвейера обработки (Middleware) ---

// Эндпоинт для проверки здоровья (Health Check)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                exception = e.Value.Exception?.Message,
                duration = e.Value.Duration.TotalMilliseconds
            })
        }));
    }
});

// "Живой" эндпоинт для Kubernetes/Docker (без проверки БД)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Включаем спецификацию OpenAPI только в режиме разработки
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Автоматический запуск сидеров (Seeding) базы данных при старте
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.SeedAsync();
}

app.UseHttpsRedirection();
// Глобальный обработчик исключений (возвращает Error DTO вместо StackTrace)
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

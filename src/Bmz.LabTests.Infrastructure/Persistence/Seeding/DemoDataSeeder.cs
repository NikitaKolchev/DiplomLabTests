using Bmz.LabTests.Application.Abstractions.Auth;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Persistence.Seeding;

public sealed class DemoDataSeeder(IPasswordHasher passwordHasher) : IEntitySeeder
{
    private const string DemoUserPassword = "VeryHardPassword";

    public int Order => 3;

    private sealed record LaboratorySeed(string Name, string Code);
    private sealed record WireCodeSeed(string Code, string Marking, decimal Diameter);
    private sealed record ParameterSeed(string Name, string? Unit);
    private sealed record LimitSeed(decimal Min, decimal Max);

    private static readonly LaboratorySeed[] DemoLaboratories =
    {
        new("Лаборатория механических испытаний", "mech"),
        new("Лаборатория металлографии", "metal"),
        new("Лаборатория входного контроля", "input")
    };

    private static readonly WireCodeSeed[] DemoWireCodes =
    {
        new("WRC-01", "Стандарт A", 1.20m),
        new("WRC-02", "Стандарт B", 1.45m),
        new("WRC-03", "Стандарт C", 1.75m)
    };

    private static readonly ParameterSeed[] DemoParameters =
    {
        new("Прочность на разрыв", "МПа"),
        new("Удлинение", "%"),
        new("Отклонение диаметра", "мм")
    };

    private static readonly Dictionary<string, Dictionary<string, LimitSeed>> DemoLimits = new()
    {
        ["WRC-01"] = new()
        {
            ["Прочность на разрыв"] = new(540m, 620m),
            ["Удлинение"] = new(12.5m, 18.5m),
            ["Отклонение диаметра"] = new(-0.06m, 0.06m)
        },
        ["WRC-02"] = new()
        {
            ["Прочность на разрыв"] = new(510m, 590m),
            ["Удлинение"] = new(11.0m, 16.5m),
            ["Отклонение диаметра"] = new(-0.05m, 0.05m)
        },
        ["WRC-03"] = new()
        {
            ["Прочность на разрыв"] = new(470m, 560m),
            ["Удлинение"] = new(9.0m, 14.5m),
            ["Отклонение диаметра"] = new(-0.07m, 0.07m)
        }
    };

    private static readonly (string Reason, double Weight)[] RejectReasons =
    {
        ("Низкая прочность на разрыв", 0.34),
        ("Превышение отклонения диаметра", 0.24),
        ("Недостаточное удлинение", 0.20),
        ("Дефекты поверхности", 0.13),
        ("Несоответствие партии документам", 0.09)
    };

    public async Task<Result> SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
                return Result.Success();

            var existingTests = await context.TestResults.CountAsync(cancellationToken);
            const int targetTestsCount = 120;
            if (existingTests >= targetTestsCount)
                return Result.Success();

            var roleMap = await context.Roles
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);

            if (!roleMap.TryGetValue(Roles.Engineer, out var engineerRoleId) ||
                !roleMap.TryGetValue(Roles.Assistant, out var assistantRoleId))
            {
                return Result.Failure("Не найдены роли инженера или лаборанта для наполнения демо-данными.");
            }

        var random = new Random(20260306);

        foreach (var labSeed in DemoLaboratories)
        {
            if (!await context.Laboratories.AnyAsync(x => x.Name == labSeed.Name, cancellationToken))
            {
                context.Laboratories.Add(new Laboratory { Name = labSeed.Name });
            }
        }

        if (!await context.Countries.AnyAsync(x => x.Name == "Беларусь", cancellationToken))
        {
            context.Countries.Add(new Country { Name = "Беларусь" });
        }

        await context.SaveChangesAsync(cancellationToken);

        var countryId = await context.Countries
            .Where(x => x.Name == "Беларусь")
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);

        var customerSeeds = new[] { "БелМетПрокат", "ПромСталь", "ЭнергоКабель", "МеталлТрейд" };
        foreach (var customerName in customerSeeds)
        {
            if (!await context.Customers.AnyAsync(x => x.Name == customerName, cancellationToken))
            {
                context.Customers.Add(new Customer { Name = customerName, CountryId = countryId });
            }
        }

        foreach (var wireSeed in DemoWireCodes)
        {
            if (!await context.WireCodes.AnyAsync(x => x.Code == wireSeed.Code, cancellationToken))
            {
                context.WireCodes.Add(new WireCode
                {
                    Code = wireSeed.Code,
                    Marking = wireSeed.Marking,
                    Diameter = wireSeed.Diameter
                });
            }
        }

        foreach (var parameterSeed in DemoParameters)
        {
            if (!await context.Parameters.AnyAsync(x => x.Name == parameterSeed.Name, cancellationToken))
            {
                context.Parameters.Add(new Parameter
                {
                    Name = parameterSeed.Name,
                    Unit = parameterSeed.Unit,
                    DataType = ParameterDataType.Number
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var demoLabNames = DemoLaboratories.Select(x => x.Name).ToArray();
        var demoWireCodes = DemoWireCodes.Select(x => x.Code).ToArray();
        var demoParameterNames = DemoParameters.Select(x => x.Name).ToArray();

        var laboratories = await context.Laboratories
            .Where(x => demoLabNames.Contains(x.Name))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var customers = await context.Customers
            .Where(x => customerSeeds.Contains(x.Name))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var wireCodes = await context.WireCodes
            .Where(x => demoWireCodes.Contains(x.Code))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var parameters = await context.Parameters
            .Where(x => demoParameterNames.Contains(x.Name))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var wireCodeMap = wireCodes.ToDictionary(x => x.Code, x => x);
        var parameterMap = parameters.ToDictionary(x => x.Name, x => x);

        foreach (var wireSeed in DemoWireCodes)
        {
            foreach (var parameterSeed in DemoParameters)
            {
                if (!DemoLimits.TryGetValue(wireSeed.Code, out var parameterLimits) ||
                    !parameterLimits.TryGetValue(parameterSeed.Name, out var limitSeed))
                {
                    continue;
                }

                var wireCodeId = wireCodeMap[wireSeed.Code].Id;
                var parameterId = parameterMap[parameterSeed.Name].Id;

                if (await context.Limits.AnyAsync(x => x.WireCodeId == wireCodeId && x.ParameterId == parameterId, cancellationToken))
                {
                    continue;
                }

                context.Limits.Add(new WireCodeLimit
                {
                    WireCodeId = wireCodeId,
                    ParameterId = parameterId,
                    MinValue = limitSeed.Min,
                    MaxValue = limitSeed.Max,
                    IsRequired = true
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < laboratories.Count; i++)
        {
            var lab = laboratories[i];
            var labCode = DemoLaboratories[i].Code;

            var engineerLogin = $"eng-{labCode}";
            if (!await context.Users.AnyAsync(x => x.Login == engineerLogin, cancellationToken))
            {
                context.Users.Add(new User
                {
                    Login = engineerLogin,
                    FullName = $"Инженер {lab.Name}",
                    Sid = $"LOCAL-ENG-{labCode.ToUpperInvariant()}",
                    IsLocalAccount = true,
                    PasswordHash = passwordHasher.Hash(DemoUserPassword),
                    RoleId = engineerRoleId,
                    LaboratoryId = lab.Id
                });
            }

            for (var assistantIndex = 1; assistantIndex <= 2; assistantIndex++)
            {
                var assistantLogin = $"asst-{labCode}-{assistantIndex}";
                if (await context.Users.AnyAsync(x => x.Login == assistantLogin, cancellationToken))
                {
                    continue;
                }

                context.Users.Add(new User
                {
                    Login = assistantLogin,
                    FullName = $"Лаборант {labCode.ToUpperInvariant()}-{assistantIndex}",
                    Sid = $"LOCAL-ASST-{labCode.ToUpperInvariant()}-{assistantIndex}",
                    IsLocalAccount = true,
                    PasswordHash = passwordHasher.Hash(DemoUserPassword),
                    RoleId = assistantRoleId,
                    LaboratoryId = lab.Id
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var assistants = await context.Users
            .Where(x => x.RoleId == assistantRoleId && x.LaboratoryId != null)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var assistantsByLab = assistants
            .GroupBy(x => x.LaboratoryId!.Value)
            .ToDictionary(x => x.Key, x => x.ToList());

        var demoWireCodeIds = wireCodes.Select(x => x.Id).ToArray();
        var demoParameterIds = parameters.Select(x => x.Id).ToArray();
        var limits = await context.Limits
            .Where(x => demoWireCodeIds.Contains(x.WireCodeId) && demoParameterIds.Contains(x.ParameterId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var limitMap = limits.ToDictionary(x => (x.WireCodeId, x.ParameterId), x => x);

        var tests = new List<TestResult>();
        var testStates = new List<(TestResult Test, bool Rejected, string? Reason, int? BadParameterId)>();
        var startDate = DateTime.UtcNow.Date.AddDays(-90);
        var totalTestsToGenerate = targetTestsCount - existingTests;

        for (var i = 0; i < totalTestsToGenerate; i++)
        {
            var date = startDate
                .AddDays(random.Next(0, 90))
                .AddHours(random.Next(0, 24))
                .AddMinutes(random.Next(0, 60));

            var lab = laboratories[random.Next(laboratories.Count)];
            var assistantsForLab = assistantsByLab.GetValueOrDefault(lab.Id) ?? assistants;
            var assistant = assistantsForLab[random.Next(assistantsForLab.Count)];

            var wireRoll = random.NextDouble();
            var wireCode = wireRoll < 0.45
                ? wireCodes[0]
                : wireRoll < 0.80
                    ? wireCodes[1]
                    : wireCodes[2];

            var customer = random.NextDouble() < 0.85 ? customers[random.Next(customers.Count)] : null;
            var isCompleted = random.NextDouble() > 0.18;
            var updatedAt = isCompleted
                ? date.AddHours(4 + random.Next(2, 120))
                : date.AddHours(1 + random.Next(1, 36));
            if (updatedAt > DateTime.UtcNow)
                updatedAt = DateTime.UtcNow;

            var testResult = new TestResult(
                DateTime.SpecifyKind(date, DateTimeKind.Utc),
                DateTime.SpecifyKind(updatedAt, DateTimeKind.Utc),
                assistant.Id,
                wireCode.Id,
                lab.Id,
                $"DEMO-{DateTime.UtcNow:yyyyMMdd}-{(existingTests + i + 1):0000}",
                customer?.Id,
                isCompleted ? TestResultStatus.Completed : TestResultStatus.InProgress);

            tests.Add(testResult);

            var wirePenalty = wireCode.Code switch
            {
                "WRC-03" => 0.10,
                "WRC-02" => 0.05,
                _ => 0.00
            };
            var labPenalty = lab.Name.Contains("входного", StringComparison.OrdinalIgnoreCase) ? 0.06 : 0.00;
            var rejectProbability = 0.06 + wirePenalty + labPenalty;
            var rejected = isCompleted && random.NextDouble() < rejectProbability;

            string? reason = null;
            int? badParameterId = null;
            if (rejected)
            {
                reason = PickWeightedReason(random);
                badParameterId = parameters[random.Next(parameters.Count)].Id;
            }

            testStates.Add((testResult, rejected, reason, badParameterId));
        }

        context.TestResults.AddRange(tests);
        await context.SaveChangesAsync(cancellationToken);

        var testValues = new List<TestValue>();
        var rejects = new List<Reject>();
        var finalProducts = new List<FinalProduct>();

        foreach (var state in testStates)
        {
            foreach (var parameter in parameters)
            {
                if (!limitMap.TryGetValue((state.Test.WireCodeId, parameter.Id), out var limit))
                    continue;

                var value = CreateValue(
                    random,
                    limit.MinValue ?? 0m,
                    limit.MaxValue ?? 1m,
                    state.Rejected && state.BadParameterId == parameter.Id);

                testValues.Add(new TestValue(state.Test.Id, parameter.Id, value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }

            if (state.Test.Status != TestResultStatus.Completed)
                continue;

            if (state.Rejected)
            {
                rejects.Add(new Reject(
                    state.Test.Id,
                    state.Reason ?? "Отклонение параметров от нормы",
                    state.Test.UpdatedAtUtc));
            }
            else
            {
                finalProducts.Add(new FinalProduct(
                    state.Test.Id,
                    state.Test.UpdatedAtUtc));
            }
        }

        context.TestValues.AddRange(testValues);
        context.Rejects.AddRange(rejects);
        context.FinalProducts.AddRange(finalProducts);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
    catch (Exception ex)
    {
        return Result.Failure($"Ошибка при генерации демо-данных: {ex.Message}");
    }
}

    private static string PickWeightedReason(Random random)
    {
        var roll = random.NextDouble();
        var cumulative = 0.0;
        foreach (var (reason, weight) in RejectReasons)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return reason;
        }
        return RejectReasons[0].Reason;
    }

    private static decimal CreateValue(Random random, decimal min, decimal max, bool isOut)
    {
        var range = max - min;
        if (isOut)
        {
            return random.Next(2) == 0
                ? min - range * (decimal)(0.02 + random.NextDouble() * 0.1)
                : max + range * (decimal)(0.02 + random.NextDouble() * 0.1);
        }

        return min + range * (decimal)(0.1 + random.NextDouble() * 0.8);
    }
}

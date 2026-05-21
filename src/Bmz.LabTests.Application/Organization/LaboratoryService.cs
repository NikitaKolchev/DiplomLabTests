using Bmz.LabTests.Application.Abstractions.Audit;
using Bmz.LabTests.Application.Abstractions.Organization;
using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Entities;

namespace Bmz.LabTests.Application.Organization;

/// <summary>
/// Сервис для управления лабораториями (подразделениями).
/// Обеспечивает операции создания, обновления и удаления лабораторий, а также управление персоналом (назначение инженеров).
/// </summary>
public sealed class LaboratoryService(
    IOrganizationRepository repository,
    IAuditService auditService) : ILaboratoryService
{
    /// <summary>
    /// Возвращает список всех лабораторий с информацией о закрепленных инженерах.
    /// </summary>
    public async Task<Result<IReadOnlyCollection<LaboratorySummaryDto>>> GetLaboratoriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var laboratories = await repository.GetLaboratoriesAsync(cancellationToken);
            var dtos = laboratories
                .Select(x => new LaboratorySummaryDto(x.Id, x.Name, x.GetEngineer()?.Id, x.GetEngineer()?.FullName))
                .ToArray();

            return Result.Success<IReadOnlyCollection<LaboratorySummaryDto>>(dtos);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyCollection<LaboratorySummaryDto>>($"Ошибка при получении списка лабораторий: {ex.Message}");
        }
    }

    /// <summary>
    /// Создает новую лабораторию и опционально назначает в нее инженера.
    /// </summary>
    public async Task<Result<LaboratorySummaryDto>> CreateLaboratoryAsync(int actorUserId, string? actorLogin, string name, int? engineerId, CancellationToken cancellationToken)
    {
        try
        {
            User? engineer = null;
            if (engineerId.HasValue)
            {
                engineer = await repository.GetEngineerByIdAsync(engineerId.Value, cancellationToken);
                if (engineer is null)
                {
                    return Result.Failure<LaboratorySummaryDto>("Инженер не найден.");
                }

                if (await repository.IsEngineerAssignedToAnotherLaboratoryAsync(engineer.Id, null, cancellationToken))
                {
                    return Result.Failure<LaboratorySummaryDto>("Инженер уже назначен в другую лабораторию.");
                }
            }

            var laboratory = new Laboratory
            {
                Name = name.Trim()
            };

            await repository.AddLaboratoryAsync(laboratory, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            if (engineer is not null)
            {
                engineer.LaboratoryId = laboratory.Id;
                await repository.SaveChangesAsync(cancellationToken);
            }

            await auditService.WriteAsync(actorUserId, actorLogin, "Create", "Laboratory", laboratory.Id.ToString(), $"Created laboratory '{laboratory.Name}'", cancellationToken);

            return Result.Success(new LaboratorySummaryDto(laboratory.Id, laboratory.Name, engineer?.Id, engineer?.FullName));
        }
        catch (Exception ex)
        {
            return Result.Failure<LaboratorySummaryDto>($"Ошибка при создании лаборатории: {ex.Message}");
        }
    }

    /// <summary>
    /// Назначает инженера ответственным за указанную лабораторию.
    /// </summary>
    public async Task<Result<LaboratorySummaryDto>> AssignEngineerAsync(int actorUserId, string? actorLogin, int laboratoryId, int engineerId, CancellationToken cancellationToken)
    {
        try
        {
            var laboratory = await repository.GetLaboratoryByIdAsync(laboratoryId, cancellationToken);
            if (laboratory is null)
            {
                return Result.Failure<LaboratorySummaryDto>("Лаборатория не найдена.");
            }

            var engineer = await repository.GetEngineerByIdAsync(engineerId, cancellationToken);
            if (engineer is null)
            {
                return Result.Failure<LaboratorySummaryDto>("Инженер не найден.");
            }

            if (await repository.IsEngineerAssignedToAnotherLaboratoryAsync(engineerId, laboratoryId, cancellationToken))
            {
                return Result.Failure<LaboratorySummaryDto>("Инженер уже назначен в другую лабораторию.");
            }

            var currentEngineer = laboratory.GetEngineer();
            if (currentEngineer is not null && currentEngineer.Id != engineerId)
            {
                currentEngineer.LaboratoryId = null;
            }

            engineer.LaboratoryId = laboratory.Id;
            await repository.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(actorUserId, actorLogin, "AssignEngineer", "Laboratory", laboratory.Id.ToString(), $"Assigned engineer '{engineer.Login}'", cancellationToken);

            return Result.Success(new LaboratorySummaryDto(laboratory.Id, laboratory.Name, engineer.Id, engineer.FullName));
        }
        catch (Exception ex)
        {
            return Result.Failure<LaboratorySummaryDto>($"Ошибка при назначении инженера: {ex.Message}");
        }
    }

    /// <summary>
    /// Обновляет данные лаборатории (название и ответственного инженера).
    /// </summary>
    public async Task<Result<LaboratorySummaryDto>> UpdateLaboratoryAsync(int actorUserId, string? actorLogin, int id, string name, int? engineerId, CancellationToken cancellationToken)
    {
        try
        {
            var laboratory = await repository.GetLaboratoryByIdAsync(id, cancellationToken);
            if (laboratory is null)
            {
                return Result.Failure<LaboratorySummaryDto>("Лаборатория не найдена.");
            }

            laboratory.Name = name.Trim();

            if (engineerId.HasValue)
            {
                var engineer = await repository.GetEngineerByIdAsync(engineerId.Value, cancellationToken);
                if (engineer is null)
                {
                    return Result.Failure<LaboratorySummaryDto>("Инженер не найден.");
                }

                if (await repository.IsEngineerAssignedToAnotherLaboratoryAsync(engineer.Id, id, cancellationToken))
                {
                    return Result.Failure<LaboratorySummaryDto>("Инженер уже назначен в другую лабораторию.");
                }

                var currentEngineer = laboratory.GetEngineer();
                if (currentEngineer is not null && currentEngineer.Id != engineer.Id)
                {
                    currentEngineer.LaboratoryId = null;
                }

                engineer.LaboratoryId = laboratory.Id;
            }
            else
            {
                var currentEngineer = laboratory.GetEngineer();
                if (currentEngineer is not null)
                {
                    currentEngineer.LaboratoryId = null;
                }
            }

            await repository.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(actorUserId, actorLogin, "Update", "Laboratory", id.ToString(), $"Updated laboratory '{laboratory.Name}'", cancellationToken);

            return Result.Success(new LaboratorySummaryDto(laboratory.Id, laboratory.Name, laboratory.GetEngineer()?.Id, laboratory.GetEngineer()?.FullName));
        }
        catch (Exception ex)
        {
            return Result.Failure<LaboratorySummaryDto>($"Ошибка при обновлении лаборатории: {ex.Message}");
        }
    }

    /// <summary>
    /// Удаляет лабораторию из системы и отвязывает от нее персонал.
    /// </summary>
    public async Task<Result> DeleteLaboratoryAsync(int actorUserId, string? actorLogin, int id, CancellationToken cancellationToken)
    {
        try
        {
            var laboratory = await repository.GetLaboratoryByIdAsync(id, cancellationToken);
            if (laboratory is null)
            {
                return Result.Failure("Лаборатория не найдена.");
            }

            var engineer = laboratory.GetEngineer();
            if (engineer is not null)
            {
                engineer.LaboratoryId = null;
            }

            await repository.ClearLaboratoryFromUsersAsync(id, cancellationToken);
            repository.RemoveLaboratory(laboratory);
            await repository.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(actorUserId, actorLogin, "Delete", "Laboratory", id.ToString(), $"Deleted laboratory '{laboratory.Name}'", cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Ошибка при удалении лаборатории: {ex.Message}");
        }
    }
}

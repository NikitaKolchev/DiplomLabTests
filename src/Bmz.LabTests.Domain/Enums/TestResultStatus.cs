namespace Bmz.LabTests.Domain.Enums;

/// <summary>
/// Возможные статусы протокола испытаний.
/// </summary>
public enum TestResultStatus
{
    /// <summary>Испытание проводится, данные вносятся лаборантом.</summary>
    InProgress = 1,

    /// <summary>Испытание завершено, данные заморожены, протокол доступен для отчетов.</summary>
    Completed = 2
}

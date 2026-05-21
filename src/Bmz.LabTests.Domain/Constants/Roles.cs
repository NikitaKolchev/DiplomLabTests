namespace Bmz.LabTests.Domain.Constants;

/// <summary>
/// Статические константы названий ролей в системе.
/// </summary>
public static class Roles
{
    /// <summary>Администратор системы: полный доступ к настройкам и пользователям.</summary>
    public const string Admin = "Admin";

    /// <summary>Инженер: управление справочниками и лабораториями.</summary>
    public const string Engineer = "Engineer";

    /// <summary>Ассистент (Лаборант): создание протоколов и внесение данных измерений.</summary>
    public const string Assistant = "Assistant";

    /// <summary>Гость: только просмотр журнала испытаний без возможности редактирования.</summary>
    public const string Guest = "Guest";
}

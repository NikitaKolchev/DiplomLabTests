namespace Bmz.LabTests.Domain.Common;

/// <summary>
/// Базовый класс для всех сущностей доменной модели.
/// Содержит общие поля, такие как идентификатор и версию строки для оптимистичной блокировки.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Первичный ключ сущности.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Версия строки (Timestamp/RowVersion) для реализации механизма оптимистичной блокировки (Optimistic Concurrency).
    /// Предотвращает перезапись данных при одновременном редактировании разными пользователями.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}

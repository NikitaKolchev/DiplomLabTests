using System.Linq.Expressions;

namespace Bmz.LabTests.Domain.Common;

/// <summary>
/// Реализация паттерна "Спецификация" (Specification Pattern).
/// Используется для инкапсуляции логики фильтрации, загрузки связанных данных (Include), сортировки и пагинации.
/// Позволяет отделить бизнес-правила выборки данных от инфраструктуры (Entity Framework).
/// </summary>
/// <typeparam name="T">Тип сущности, к которой применяется спецификация.</typeparam>
public abstract class Specification<T>
{
    /// <summary>Список условий фильтрации (WHERE).</summary>
    public List<Expression<Func<T, bool>>> Criteria { get; } = new();

    /// <summary>Список навигационных свойств для жадной загрузки (Eager Loading / INCLUDE).</summary>
    public List<Expression<Func<T, object>>> Includes { get; } = new();

    /// <summary>Выражение для сортировки по возрастанию.</summary>
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <summary>Выражение для сортировки по убыванию.</summary>
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <summary>Количество записей для выборки.</summary>
    public int Take { get; private set; }

    /// <summary>Количество пропускаемых записей.</summary>
    public int Skip { get; private set; }

    /// <summary>Признак включенной пагинации.</summary>
    public bool IsPagingEnabled { get; private set; }

    /// <summary>Добавляет условие фильтрации.</summary>
    protected virtual void AddCriteria(Expression<Func<T, bool>> criteriaExpression)
    {
        Criteria.Add(criteriaExpression);
    }

    /// <summary>Добавляет связанную сущность для загрузки.</summary>
    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>Устанавливает сортировку по возрастанию.</summary>
    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
        OrderByDescending = null;
    }

    /// <summary>Устанавливает сортировку по убыванию.</summary>
    protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
        OrderBy = null;
    }

    /// <summary>Настраивает параметры пагинации.</summary>
    protected virtual void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}
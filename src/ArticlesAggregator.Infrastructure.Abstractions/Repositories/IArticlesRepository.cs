using ArticlesAggregator.Infrastructure.Abstractions.Entities;

namespace ArticlesAggregator.Infrastructure.Abstractions.Repositories;

public interface IArticleRepository
{
    /// <summary>
    ///     Добавляет статью и возвращает её новый ID.
    /// </summary>
    Task<Guid> AddAsync(ArticleEntity entity, CancellationToken ct = default);

    /// <summary>
    ///     Обновляет поля существующей статьи.
    ///     Возвращает true, если запись найдена и обновлена.
    /// </summary>
    Task<bool> UpdateAsync(ArticleEntity entity, CancellationToken ct = default);

    /// <summary>
    ///     Удаляет статью по ID. Возвращает true, если что-то удалено.
    /// </summary>
    Task<bool> RemoveAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    ///     Получает статью по её ID.
    /// </summary>
    Task<ArticleEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    ///     Ищет статьи по точному или частичному совпадению названия.
    /// </summary>
    Task<IEnumerable<ArticleEntity>> SearchByTitleAsync(string title, CancellationToken ct = default);
}

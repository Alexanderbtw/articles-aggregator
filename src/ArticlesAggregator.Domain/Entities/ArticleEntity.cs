namespace ArticlesAggregator.Domain.Entities;

public sealed class ArticleEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Uri Url { get; init; }

    public required string Title { get; init; }

    public required string Content { get; init; }

    // Tag system maybe...
}

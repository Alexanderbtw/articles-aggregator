namespace ArticlesAggregator.Infrastructure.Abstractions.Entities;

public sealed class ArticleEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Uri Url { get; init; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    // Tag system maybe...
}

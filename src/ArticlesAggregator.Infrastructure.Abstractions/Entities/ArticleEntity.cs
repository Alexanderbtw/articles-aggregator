namespace ArticlesAggregator.Infrastructure.Abstractions.Entities;

public sealed class ArticleEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; init; }

    public required string Content { get; init; }

    public required string SourceUrl { get; init; }
}

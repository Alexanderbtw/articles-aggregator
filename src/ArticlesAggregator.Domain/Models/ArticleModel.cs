using System.Text.Json.Serialization;

namespace ArticlesAggregator.Domain.Models;

public sealed class ArticleModel
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; init; }

    public required string Content { get; init; }

    [JsonPropertyName("source_url")]
    public required Uri SourceUrl { get; init; }

    // Tag system maybe...
}

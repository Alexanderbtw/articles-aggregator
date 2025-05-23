namespace ArticlesAggregator.ExternalServices.Parser.Contracts;

public sealed record ParsedArticleDto(
    string Title,
    string Data);

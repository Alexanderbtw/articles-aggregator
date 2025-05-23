namespace ArticlesAggregator.ExternalServices.Parser.Options;

internal sealed class ExternalParserOptions
{
    public const string SectionName = "ExternalParser";
    public required string BaseUrl { get; init; }
}

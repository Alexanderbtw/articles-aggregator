using System.Net.Http.Json;

using ArticlesAggregator.Domain.Models;
using ArticlesAggregator.ExternalServices.Parser;
using ArticlesAggregator.ExternalServices.Parser.Contracts;

namespace ArticlesAggregator.ExternalServices.WikiApi;

internal sealed class ExternalParserClient(
    ExternalParserHttpClient http) : IExternalParserClient
{
    public async Task<ArticleModel> GetArticle(Uri link, CancellationToken ct)
    {
        ParsedArticleDto? dto = await http.GetArticles(link, ct);

        if (dto is null)
        {
            throw new InvalidOperationException("Parser returned null");
        }

        return new ArticleModel
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Content = dto.Data,
            SourceUrl = link
        };
    }
}

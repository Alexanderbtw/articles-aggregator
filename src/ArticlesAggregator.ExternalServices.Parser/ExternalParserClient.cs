using System.Net.Http.Json;

using ArticlesAggregator.Domain.Entities;
using ArticlesAggregator.ExternalServices.Parser;
using ArticlesAggregator.ExternalServices.Parser.Contracts;

namespace ArticlesAggregator.ExternalServices.WikiApi;

internal sealed class ExternalParserClient(
    ExternalParserHttpClient http) : IExternalParserClient
{
    public async Task<ArticleEntity> GetArticle(Uri link, CancellationToken ct)
    {
        ParsedArticleDto? dto = await http.GetArticles(link, ct);

        if (dto is null)
        {
            throw new InvalidOperationException("Parser returned null");
        }

        return new ArticleEntity
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Content = dto.Data,
            Url = link
        };
    }
}

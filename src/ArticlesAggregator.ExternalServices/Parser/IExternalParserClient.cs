using ArticlesAggregator.Domain.Models;

namespace ArticlesAggregator.ExternalServices.Parser;

public interface IExternalParserClient
{
    public Task<ArticleModel> GetArticle(Uri link, CancellationToken ct);
}

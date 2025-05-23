using ArticlesAggregator.Domain.Entities;

namespace ArticlesAggregator.ExternalServices.Parser;

public interface IExternalParserClient
{
    public Task<ArticleEntity> GetArticle(Uri link, CancellationToken ct);
}

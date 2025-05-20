using ArticlesAggregator.Infrastructure.Abstractions.Entities;

using MediatR;

namespace ArticlesAggregator.Application;

public sealed record SearchArticleQuery(string Title) : IRequest<IEnumerable<ArticleEntity>>;
public sealed record SearchArticleQueryResponse(IEnumerable<ArticleEntity> Articles);

internal sealed class SearchArticleQueryHandler
{

}

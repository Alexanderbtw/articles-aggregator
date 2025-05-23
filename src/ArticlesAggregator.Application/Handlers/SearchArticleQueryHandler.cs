using ArticlesAggregator.Domain.Entities;

using MediatR;

namespace ArticlesAggregator.Application.Handlers;

public sealed record SearchArticleQuery(string Title) : IRequest<SearchArticleQueryResponse>;

public sealed record SearchArticleQueryResponse(List<ArticleEntity> Articles);

internal sealed class SearchArticleQueryHandler
{
}

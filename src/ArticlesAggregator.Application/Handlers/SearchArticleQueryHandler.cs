using ArticlesAggregator.Application.Handlers.Converters;
using ArticlesAggregator.Domain.Models;
using ArticlesAggregator.Infrastructure.Abstractions.Entities;
using ArticlesAggregator.Infrastructure.Abstractions.Repositories;

using MediatR;

namespace ArticlesAggregator.Application.Handlers;

public sealed record SearchArticleQuery(string Title) : IRequest<SearchArticleQueryResponse>;

public sealed record SearchArticleQueryResponse(List<ArticleModel> Articles);

internal sealed class SearchArticleQueryHandler(
    IArticleRepository repository)
    : IRequestHandler<SearchArticleQuery, SearchArticleQueryResponse>
{
    public async Task<SearchArticleQueryResponse> Handle(SearchArticleQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<ArticleEntity> articles = await repository.SearchByTitleAsync(request.Title, cancellationToken);

        return new SearchArticleQueryResponse(articles.Select(a => a.ToDomainModel()).ToList());
    }
}

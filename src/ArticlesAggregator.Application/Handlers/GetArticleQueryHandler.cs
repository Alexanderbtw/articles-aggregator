using ArticlesAggregator.Domain.Entities;
using ArticlesAggregator.Infrastructure.Abstractions.Repositories;

using MediatR;

namespace ArticlesAggregator.Application.Handlers;

public sealed record GetArticleQuery(Guid Id) : IRequest<GetArticleQueryResponse>;

public sealed record GetArticleQueryResponse(ArticleEntity Article);

internal sealed class GetArticleQueryHandler(IArticleRepository repository)
    : IRequestHandler<GetArticleQuery, GetArticleQueryResponse>
{
    public async Task<GetArticleQueryResponse> Handle(GetArticleQuery request, CancellationToken cancellationToken)
    {
        ArticleEntity? article = await repository.GetByIdAsync(request.Id, cancellationToken);

        if (article is null)
        {
            throw new Exception("Article not found");
        }

        return new GetArticleQueryResponse(article);
    }
}

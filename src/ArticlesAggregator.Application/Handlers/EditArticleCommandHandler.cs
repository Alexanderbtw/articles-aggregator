using ArticlesAggregator.Application.Handlers.Converters;
using ArticlesAggregator.Domain.Models;
using ArticlesAggregator.Infrastructure.Abstractions.Entities;
using ArticlesAggregator.Infrastructure.Abstractions.Repositories;

using MediatR;

namespace ArticlesAggregator.Application.Handlers;

public sealed record EditArticleCommand(ArticleModel Article) : IRequest<EditArticleCommandResponse>;

public sealed record EditArticleCommandResponse(bool Success);

internal sealed class EditArticleCommandHandler(
    IArticleRepository repository)
    : IRequestHandler<EditArticleCommand, EditArticleCommandResponse>
{
    public async Task<EditArticleCommandResponse> Handle(EditArticleCommand request, CancellationToken cancellationToken)
    {
        ArticleEntity article = request.Article.ToEntity();
        bool result = await repository.UpdateAsync(article, cancellationToken);

        return new EditArticleCommandResponse(result);
    }
}

using ArticlesAggregator.Domain.Entities;
using ArticlesAggregator.Infrastructure.Abstractions.Repositories;

using MediatR;

namespace ArticlesAggregator.Application.Handlers;

public sealed record EditArticleCommand(ArticleEntity Article) : IRequest<EditArticleCommandResponse>;

public sealed record EditArticleCommandResponse(bool Success);

internal sealed class EditArticleCommandHandler(
    IArticleRepository repository)
    : IRequestHandler<EditArticleCommand, EditArticleCommandResponse>
{
    public async Task<EditArticleCommandResponse> Handle(EditArticleCommand request, CancellationToken cancellationToken)
    {
        bool result = await repository.UpdateAsync(request.Article, cancellationToken);

        return new EditArticleCommandResponse(result);
    }
}

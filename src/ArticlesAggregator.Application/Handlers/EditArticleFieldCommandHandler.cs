using ArticlesAggregator.Domain.Models;
using ArticlesAggregator.Infrastructure.Abstractions.Entities;
using ArticlesAggregator.Infrastructure.Abstractions.Repositories;

using MediatR;

namespace ArticlesAggregator.Application.Handlers;

public sealed record EditArticleFieldCommand(
    Guid Id,
    string FieldName,
    string FieldValue)
    : IRequest;

internal sealed class EditArticleFieldCommandHandler(IArticleRepository repository) : IRequestHandler<EditArticleFieldCommand>
{
    public async Task Handle(EditArticleFieldCommand request, CancellationToken cancellationToken)
    {
        ArticleEntity? article = await repository.GetByIdAsync(request.Id, cancellationToken);

        if (article is null)
        {
            throw new Exception("Article not found");
        }

        article.GetType().GetProperty(request.FieldName)?.SetValue(article, request.FieldValue); // HAHA, very bad code

        await repository.UpdateAsync(article, cancellationToken);
    }
}

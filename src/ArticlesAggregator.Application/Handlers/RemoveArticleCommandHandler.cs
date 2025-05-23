using ArticlesAggregator.Infrastructure.Abstractions.Repositories;

using MediatR;

namespace ArticlesAggregator.Application.Handlers;

public sealed record RemoveArticleCommand(Guid Id) : IRequest<RemoveArticleCommandResponse>;

public sealed record RemoveArticleCommandResponse(bool Success);

internal sealed class RemoveArticleCommandHandler(
    IArticleRepository repo)
    : IRequestHandler<RemoveArticleCommand, RemoveArticleCommandResponse>
{
    public async Task<RemoveArticleCommandResponse> Handle(RemoveArticleCommand req, CancellationToken ct)
    {
        bool success = await repo.RemoveAsync(req.Id, ct);
        return new RemoveArticleCommandResponse(success);
    }
}

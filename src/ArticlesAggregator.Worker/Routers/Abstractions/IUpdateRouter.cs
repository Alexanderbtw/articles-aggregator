using Telegram.Bot.Types;

namespace ArticlesAggregator.Worker.Routers.Abstractions;

public interface IUpdateRouter
{
    Task RouteAsync(Update u, CancellationToken ct);
}

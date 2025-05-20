using ArticlesAggregator.Worker.Routers.Abstractions;

using Telegram.Bot;
using Telegram.Bot.Types;

namespace ArticlesAggregator.Worker.Workers;

public sealed class UpdateWorker(
    ITelegramBotClient bot,
    ILogger<UpdateWorker> log,
    IServiceProvider sp) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await bot.ReceiveAsync(
            HandleUpdateAsync,
            HandleErrorAsync,
            cancellationToken: ct);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update u, CancellationToken ct)
    {
        using IServiceScope scope = sp.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IUpdateRouter>();
        await handler.RouteAsync(u, ct);
    }

    private Task HandleErrorAsync(ITelegramBotClient client, Exception ex, CancellationToken cancellationToken)
    {
        log.LogError(ex, "Telegram error");

        return Task.CompletedTask;
    }
}

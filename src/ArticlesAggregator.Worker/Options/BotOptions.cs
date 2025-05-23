namespace ArticlesAggregator.Worker.Options;

internal sealed class BotOptions
{
    public required string Token { get; init; } = null!;

    public required long[] Admins { get; init; } = [];
}

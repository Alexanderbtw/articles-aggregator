namespace ArticlesAggregator.Worker.Options;

public sealed record BotOptions
{
    public string Token { get; init; } = null!;

    public long[] Admins { get; init; } = [];
}

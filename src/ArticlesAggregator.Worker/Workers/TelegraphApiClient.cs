using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Telegraph.Sharp;
using Telegraph.Sharp.Types;

namespace ArticlesAggregator.Worker.Workers;

internal sealed class TelegraphApiClient
{
    private readonly TelegraphClient _client;
    private readonly ILogger<TelegraphApiClient> _log;

    public TelegraphApiClient(
        IOptions<TelegraphOptions> options,
        ILogger<TelegraphApiClient> log)
    {
        _log = log;

        string token = options.Value.Token ??
            throw new InvalidOperationException("Telegraph access‑token is not configured");

        _client = new TelegraphClient(token);
    }

    public async Task<string> PublishAsync(
        string title,
        string body,
        bool isHtml = false,
        CancellationToken ct = default)
    {
        try
        {
            const int limitBytes = 64 * 1024; // 64 KiB Telegraph hard‑limit
            var nodes = new List<Node>();
            Encoding utf8 = Encoding.UTF8;

            if (isHtml)
            {
                // пробуем одним узлом
                string slice = body;
                nodes.Add(new Node { Tag = TagEnum.P, Value = slice });

                while (utf8.GetByteCount(JsonSerializer.Serialize(nodes)) > limitBytes && slice.Length > 0)
                {
                    // обрезаем по 1024 символа, пока не влезет
                    slice = slice[..Math.Max(0, slice.Length - 1024)];
                    nodes[0].Value = slice + "…";
                }
            }
            else
            {
                foreach (string para in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    // временно добавляем абзац
                    nodes.Add(new Node { Tag = TagEnum.P, Children = [para] });

                    // считаем реальный размер сериализованного контента
                    int jsonSize = utf8.GetByteCount(JsonSerializer.Serialize(nodes));

                    if (jsonSize > limitBytes)
                    {
                        // удаляем последний, он не поместился
                        nodes.RemoveAt(nodes.Count - 1);
                        nodes.Add(new Node { Tag = TagEnum.P, Value = "…" });
                        break;
                    }
                }
            }

            Page page = await _client.CreatePageAsync(title, nodes, cancellationToken: ct);

            return page.Url;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to publish article to Telegra.ph");

            throw;
        }
    }
}

internal sealed class TelegraphOptions
{
    public string Token { get; init; } = string.Empty;
}

using System.Net.Http.Json;

using ArticlesAggregator.ExternalServices.Parser.Contracts;
using ArticlesAggregator.ExternalServices.Parser.Options;

using Microsoft.Extensions.Options;

namespace ArticlesAggregator.ExternalServices.WikiApi;

internal sealed class ExternalParserHttpClient : HttpClient
{
    public ExternalParserHttpClient(
        HttpMessageHandler handler,
        IOptions<ExternalParserOptions> options) : base(handler, false) => BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/'));

    public async Task<ParsedArticleDto?> GetArticles(Uri link, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/getArticles");
        req.Content = new StringContent(link.ToString(), System.Text.Encoding.UTF8, "text/plain");

        using HttpResponseMessage resp = await SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var wrapper = await resp.Content.ReadFromJsonAsync<ResponseWrapper>(cancellationToken: ct);
        return wrapper?.Message;
    }

    private sealed record ResponseWrapper(
        [property: System.Text.Json.Serialization.JsonPropertyName("message")]
        ParsedArticleDto? Message);
}

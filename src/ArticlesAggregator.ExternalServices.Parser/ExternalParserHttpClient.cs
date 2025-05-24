using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

using ArticlesAggregator.ExternalServices.Parser.Contracts;
using ArticlesAggregator.ExternalServices.Parser.Options;

using Microsoft.Extensions.Options;

namespace ArticlesAggregator.ExternalServices.WikiApi;

internal sealed class ExternalParserHttpClient
{
    private readonly HttpClient _http;

    public ExternalParserHttpClient(
        HttpClient http, // <- именно HttpClient!
        IOptions<ExternalParserOptions> opt)
    {
        _http = http;
        _http.BaseAddress = new Uri(opt.Value.BaseUrl.TrimEnd('/'));
    }

    public async Task<ParsedArticleDto?> GetArticles(Uri link, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/getArticles");
        req.Content = new StringContent(link.ToString(), Encoding.UTF8, "text/plain");

        using HttpResponseMessage resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var wrapper = await resp.Content
            .ReadFromJsonAsync<ResponseWrapper>(ct);

        return wrapper?.Message;
    }

    private sealed record ResponseWrapper(
        [property: JsonPropertyName("message")]
        ParsedArticleDto? Message);
}

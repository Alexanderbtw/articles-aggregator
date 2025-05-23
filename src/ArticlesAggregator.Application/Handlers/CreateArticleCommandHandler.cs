using ArticlesAggregator.Domain.Entities;
using ArticlesAggregator.ExternalServices.Parser;
using ArticlesAggregator.Infrastructure.Abstractions.Repositories;

using MediatR;

namespace ArticlesAggregator.Application.Handlers;

public sealed record CreateArticleCommand(string UrlString) : IRequest<CreateArticleCommandResponse>;

public sealed record CreateArticleCommandResponse(
    List<string> Errors,
    ArticleEntity? Article);

internal sealed class CreateArticleCommandHandler(
    IArticleRepository repo,
    IExternalParserClient externalParser)
    : IRequestHandler<CreateArticleCommand, CreateArticleCommandResponse>
{
    public async Task<CreateArticleCommandResponse> Handle(CreateArticleCommand req, CancellationToken ct)
    {
        List<string> errors = ValidateCommand(req).ToList();

        if (errors.Count > 0)
        {
            return new CreateArticleCommandResponse(errors, null);
        }

        var uri = new Uri(req.UrlString);
        ArticleEntity article = await externalParser.GetArticle(uri, ct);

        var response = new CreateArticleCommandResponse(errors, null);

        try
        {
            Guid newId = await repo.AddAsync(article, ct);
            response = response with { Article = article };
        }
        catch (Exception ex)
        {
            errors.Add($"🚨 Не удалось добавить статью: {ex.Message}");
        }

        return response;
    }

    private static IEnumerable<string> ValidateCommand(CreateArticleCommand req)
    {
        if (string.IsNullOrWhiteSpace(req.UrlString))
        {
            yield return "❗️ Пожалуйста, укажи ссылку: /link <url>";
        }

        if (!Uri.TryCreate(req.UrlString, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            yield return "❗️ Неверный URL. Убедись, что он начинается с http:// или https://";
        }
    }
}

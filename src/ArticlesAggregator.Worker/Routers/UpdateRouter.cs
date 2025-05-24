using System.Text;

using ArticlesAggregator.Application.Handlers;
using ArticlesAggregator.Domain.Models;
using ArticlesAggregator.Worker.Options;
using ArticlesAggregator.Worker.Routers.Abstractions;
using ArticlesAggregator.Worker.Workers;

using MediatR;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ArticlesAggregator.Worker.Routers;

internal sealed class UpdateRouter(
    ITelegramBotClient bot,
    IMediator mediator,
    ILogger<UpdateRouter> logger,
    IOptionsSnapshot<BotOptions> options,
    IMemoryCache cache,
    TelegraphApiClient telegraph)
    : IUpdateRouter
{
    public async Task RouteAsync(Update update, CancellationToken ct)
    {
        switch (update.Type)
        {
            case UpdateType.Message:
                await HandleMessageAsync(update.Message!, ct);

            break;
            case UpdateType.CallbackQuery:
                await HandleCallbackAsync(update.CallbackQuery!, ct);

            break;
            default:
                logger.LogInformation("Unsupported update type: {type}", update.Type);

            break;
        }
    }

    #region Command routing

    private async Task HandleCommandAsync(
        string cmd,
        string? arg,
        long chatId,
        long userId,
        bool isAdmin,
        CancellationToken ct)
    {
        switch (cmd)
        {
            case "/start":
                await HandleStartAsync(chatId, isAdmin, ct);

            break;
            case "/link" when isAdmin:
                await HandleLinkAsync(chatId, arg, ct);

            break;
            default:
                await HandleSearchAsync(chatId, cmd + (arg is null ? string.Empty : $" {arg}"), ct);

            break;
        }
    }

    #endregion

    #region State helpers

    private sealed record PendingEdit(Guid ArticleId, string Field);

    #endregion

    #region UI-helpers

    private InlineKeyboardMarkup BuildEditMenu(Guid articleId) => new InlineKeyboardMarkup(
    [
        [
            InlineKeyboardButton.WithCallbackData("✏️ Заголовок", $"edit:{articleId}:Title"),
            InlineKeyboardButton.WithCallbackData("✏️ Описание", $"edit:{articleId}:Description")
        ],
        [InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"del:{articleId}")]
    ]);

    private const int TelegramLimit = 3800;

    private static string FirstChunk(string text)
    {
        string first = text.Split('\n').First();
        return first.Length > TelegramLimit ? first[..TelegramLimit] + "..." : first;
    }

    private string PrepareArticle(ArticleModel article, string? telegraphUrl = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📰 <b>{article.Title}</b>");
        sb.AppendLine($"<i>{FirstChunk(article.Content)}</i>");
        if (telegraphUrl is not null)
        {
            sb.AppendLine($"\n<a href=\"{telegraphUrl}\">📓 Читать дальше</a>");
        }

        sb.AppendLine($"\n<a href=\"{article.SourceUrl.OriginalString}\">🦷 Источник</a>");

        return sb.ToString();
    }

    private static string GetEditKey(long chatId) => $"edit:{chatId}";

    #endregion

    #region Update-type handlers (Message/Callback)

    private async Task HandleMessageAsync(Message message, CancellationToken ct)
    {
        if (message.Text is not { } text || message.From is not { } user)
        {
            throw new Exception("Message without text or user");
        }

        long chatId = message.Chat.Id;
        bool isAdmin = options.Value.Admins.Contains(user.Id);

        if (isAdmin && cache.TryGetValue(GetEditKey(chatId), out PendingEdit? pending) && pending != null)
        {
            await HandleEditSubmit(pending, text, chatId, ct);

            return;
        }

        string[] parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0].ToLowerInvariant();
        string? arg = parts.Length > 1 ? parts[1] : null;

        await HandleCommandAsync(cmd, arg, chatId, user.Id, isAdmin, ct);
    }

    private async Task HandleEditSubmit(PendingEdit pendingEdit, string textValue, long chatId, CancellationToken ct)
    {
        await mediator.Send(new EditArticleFieldCommand(pendingEdit.ArticleId, pendingEdit.Field, textValue), ct);
        GetArticleQueryResponse resp = await mediator.Send(new GetArticleQuery(pendingEdit.ArticleId), ct);

        string? url = await PublishArticleContentIfNeeded(resp.Article, ct);
        string preparedContent = PrepareArticle(resp.Article, url);

        await bot.SendMessage(
            chatId,
            preparedContent,
            replyMarkup: BuildEditMenu(resp.Article.Id),
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        cache.Remove(GetEditKey(chatId));
    }

    private async Task HandleCallbackAsync(CallbackQuery callback, CancellationToken ct)
    {
        if (callback.Message is not { } text || callback.From is not { } user)
        {
            throw new Exception("Callback without message or user");
        }

        bool isAdmin = options.Value.Admins.Contains(user.Id);
        long chatId = callback.Message.Chat.Id;
        int messageId = callback.Message.MessageId;

        string[] chunks = callback.Data!.Split(':');
        string cmd = chunks[0];
        Guid articleId = Guid.Parse(chunks[1]);

        switch (cmd)
        {
            case "del" when isAdmin:
                await HandleDeleteAsync(chatId, articleId, messageId, ct);

            break;
            case "edit" when isAdmin:
                await HandleEditAsync(chatId, articleId, messageId, chunks[2], ct);

            break;
            case "show":
                await HandleShowAsync(chatId, articleId, isAdmin, ct);

            break;
        }

        await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
    }

    #endregion

    #region Command handlers

    private async Task HandleEditAsync(long chatId, Guid articleId, int messageId, string field, CancellationToken ct)
    {
        cache.Set(
            GetEditKey(chatId),
            new PendingEdit(articleId, field),
            TimeSpan.FromMinutes(10));

        await bot.EditMessageText(
            chatId,
            messageId,
            $"Введите новое значение для <b>{field}</b>:",
            ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task HandleStartAsync(long chatId, bool isAdmin, CancellationToken ct)
    {
        string welcome = await mediator.Send(new StartQuery(isAdmin), ct);
        await bot.SendMessage(chatId, welcome, cancellationToken: ct);
    }

    private async Task HandleLinkAsync(long chatId, string? urlArg, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(urlArg))
        {
            await bot.SendMessage(chatId, "❗️ Укажите URL после /link", cancellationToken: ct);

            return;
        }

        CreateArticleCommandResponse creation = await mediator.Send(new CreateArticleCommand(urlArg), ct);

        if (creation.Errors.Count > 0)
        {
            string errors = string.Join(Environment.NewLine, creation.Errors);
            await bot.SendMessage(chatId, errors, cancellationToken: ct);

            return;
        }

        ArticleModel article = creation.Article!;
        string? telegraphUrl = await PublishArticleContentIfNeeded(article, ct);

        string preview = PrepareArticle(article, telegraphUrl);
        InlineKeyboardMarkup kb = BuildEditMenu(article.Id);
        await bot.SendMessage(chatId, preview, replyMarkup: kb, parseMode: ParseMode.Html, cancellationToken: ct);
    }

    private async Task HandleDeleteAsync(long chatId, Guid articleId, int messageId, CancellationToken ct)
    {
        RemoveArticleCommandResponse delRes =
            await mediator.Send(new RemoveArticleCommand(articleId), ct);

        string delMsg = delRes.Success ? "✅ Статья удалена." : "❗️ Не найдена.";
        await bot.EditMessageText(chatId, messageId, delMsg, cancellationToken: ct);
    }

    private async Task HandleShowAsync(long chatId, Guid articleId, bool isAdmin, CancellationToken ct)
    {
        GetArticleQueryResponse resp = await mediator.Send(new GetArticleQuery(articleId), ct);
        InlineKeyboardMarkup? kb = isAdmin ? BuildEditMenu(resp.Article.Id) : null;

        string? telegraphUrl = await PublishArticleContentIfNeeded(resp.Article, ct);

        string preview = PrepareArticle(resp.Article, telegraphUrl);
        await bot.SendMessage(chatId, preview, replyMarkup: kb, parseMode: ParseMode.Html, cancellationToken: ct);
    }

    private async Task<string?> PublishArticleContentIfNeeded(ArticleModel article, CancellationToken ct)
    {
        string? telegraphUrl = null;

        if (article.Content.Length > TelegramLimit)
        {
            telegraphUrl = await telegraph.PublishAsync(article.Title, article.Content, false, ct);
        }

        return telegraphUrl;
    }

    private async Task HandleSearchAsync(long chatId, string query, CancellationToken ct)
    {
        SearchArticleQueryResponse results = await mediator.Send(new SearchArticleQuery(query.Trim()), ct);

        if (results.Articles.Count == 0)
        {
            await bot.SendMessage(chatId, "Ничего не нашёл 😔", cancellationToken: ct);

            return;
        }

        var kb = new List<InlineKeyboardButton[]>();

        foreach (ArticleModel a in results.Articles.Take(20)) // TODO: Pagination
        {
            string title = a.Title.Length > 50 ? a.Title[..47] + "…" : a.Title;

            kb.Add(
            [
                InlineKeyboardButton.WithCallbackData(
                    title,
                    $"show:{a.Id}")
            ]);
        }

        await bot.SendMessage(
            chatId,
            "Нашёл следующие статьи:",
            replyMarkup: new InlineKeyboardMarkup(kb),
            cancellationToken: ct);
    }

    #endregion
}

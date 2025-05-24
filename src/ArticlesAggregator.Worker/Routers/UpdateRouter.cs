using System.Text;

using ArticlesAggregator.Application.Handlers;
using ArticlesAggregator.Domain.Entities;
using ArticlesAggregator.Worker.Options;
using ArticlesAggregator.Worker.Routers.Abstractions;

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
    IMemoryCache cache)
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
            InlineKeyboardButton.WithCallbackData("✏️ Заголовок", $"edit:Title:{articleId}"),
            InlineKeyboardButton.WithCallbackData("✏️ Описание", $"edit:Description:{articleId}")
        ],
        [InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"del:{articleId}")]
    ]);

    private string PrepareArticle(ArticleEntity article)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📰 <b>{article.Title}</b>");
        sb.AppendLine($"<i>{article.Content}</i>");
        sb.AppendLine($"\n<code>{article.Url}</code>");

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
            await mediator.Send(new EditArticleFieldCommand(pending.ArticleId, pending.Field, text), ct);
            GetArticleQueryResponse resp = await mediator.Send(new GetArticleQuery(pending.ArticleId), ct);

            await bot.SendMessage(
                chatId,
                PrepareArticle(resp.Article),
                replyMarkup: BuildEditMenu(resp.Article.Id),
                cancellationToken: ct);

            cache.Remove(GetEditKey(chatId));

            return;
        }

        string[] parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0].ToLowerInvariant();
        string? arg = parts.Length > 1 ? parts[1] : null;

        await HandleCommandAsync(cmd, arg, chatId, user.Id, isAdmin, ct);
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
            case "/show":
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

        string preview = PrepareArticle(creation.Article!);
        InlineKeyboardMarkup kb = BuildEditMenu(creation.Article!.Id);
        await bot.SendMessage(chatId, preview, replyMarkup: kb, cancellationToken: ct);
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
        string preview = PrepareArticle(resp.Article);
        await bot.SendMessage(chatId, preview, replyMarkup: kb, cancellationToken: ct);
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

        foreach (ArticleEntity a in results.Articles.Take(20)) // TODO: Pagination
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

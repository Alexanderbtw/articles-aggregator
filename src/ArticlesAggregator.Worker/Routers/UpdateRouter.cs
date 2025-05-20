using System.Text;
using ArticlesAggregator.Application;
using ArticlesAggregator.Infrastructure.Abstractions.Entities;
using ArticlesAggregator.Worker.Options;
using ArticlesAggregator.Worker.Routers.Abstractions;

using MediatR;

using Microsoft.Extensions.Options;

using Telegram.Bot;
using Telegram.Bot.Types;

namespace ArticlesAggregator.Worker.Routers;

public sealed class UpdateRouter(
    ITelegramBotClient bot,
    IMediator mediator,
    ILogger<UpdateRouter> logger,
    IOptionsSnapshot<BotOptions> options) : IUpdateRouter
{
    public async Task RouteAsync(Update update, CancellationToken ct)
    {
        if (update.Message?.Text is not { } reqTitle || update.Message?.From is not { } user)
        {
            logger.LogInformation("Empty message or user");
            return;
        }

        long chatId = update.Message.Chat.Id;
        bool isAdmin = options.Value.Admins.Contains(user.Id);

        string[] parts = reqTitle.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0].ToLowerInvariant();
        string? arg = parts.Length > 1 ? parts[1] : null;

        switch (cmd)
        {
            case "/start":
                string welcome = await mediator.Send(new StartQuery(isAdmin), ct);
                await bot.SendMessage(
                    chatId: chatId,
                    text: welcome,
                    cancellationToken: ct);
                break;

            case "/link" when isAdmin:
                CreateArticleCommandResponse creationResult = await mediator.Send(new CreateArticleCommand(UrlString: arg!), ct);
                if (creationResult.Errors.Count > 0)
                {
                    var sb = new StringBuilder();
                    foreach (string error in creationResult.Errors)
                    {
                        sb.AppendLine(error);
                    }

                    await bot.SendMessage(chatId, sb.ToString(), cancellationToken: ct);
                    break;
                }
                var text = PrepareArticle(creationResult.Article!);
                await bot.SendMessage(chatId, text, cancellationToken: ct); // TODO: кнопка "Удалить" и "Редактировать" для админов

                break;

            case "/delete" when isAdmin:
                RemoveArticleCommandResponse res = await mediator.Send(new RemoveArticleCommand(Guid.Parse(arg!)), ct);
                string msg = res.Success ? "✅ Статья удалена." : "❗️ Статья не найдена.";
                await bot.SendMessage(chatId, msg, cancellationToken: ct);
                break;

            default:
                // TODO: Когда возвращаем список, то обрабатываем его как кучку кнопок, а по кнопке пересылаем запрос и если 1 статья, то показываем
                IEnumerable<ArticleEntity> results = await mediator.Send(new SearchArticleQuery(reqTitle.Trim()), ct);
                var content = PrepareMessage(results);
                await bot.SendMessage(chatId, content, cancellationToken: ct);
                break;
        }
    }
}

using System.Text;

using MediatR;

namespace ArticlesAggregator.Application.Handlers;

public sealed record StartQuery(bool IsAdmin) : IRequest<string>;

internal sealed class StartQueryHandler : IRequestHandler<StartQuery, string>
{
    public Task<string> Handle(StartQuery req, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("👋 Привет! Это бот для поиска исторических статей.");
        sb.AppendLine();
        sb.AppendLine("— Пиши название статьи, и я найду её в базе.");

        if (req.IsAdmin)
        {
            sb.AppendLine();
            sb.AppendLine("\n🛠 Вы администратор.");
            sb.AppendLine("— Админы могут использовать /link и удалять|редактировать статьи.");
        }

        return Task.FromResult(sb.ToString());
    }
}

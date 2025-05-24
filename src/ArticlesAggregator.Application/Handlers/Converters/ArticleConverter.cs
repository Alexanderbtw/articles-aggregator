using ArticlesAggregator.Domain.Models;
using ArticlesAggregator.Infrastructure.Abstractions.Entities;

namespace ArticlesAggregator.Application.Handlers.Converters;

internal static class ArticleConverter
{
    public static ArticleModel ToDomainModel(this ArticleEntity entity) => new ArticleModel
    {
        Id = entity.Id,
        Title = entity.Title,
        Content = entity.Content,
        SourceUrl = new Uri(entity.SourceUrl)
    };

    public static ArticleEntity ToEntity(this ArticleModel model) => new ArticleEntity
    {
        Id = model.Id,
        Title = model.Title,
        Content = model.Content,
        SourceUrl = model.SourceUrl.OriginalString
    };
}

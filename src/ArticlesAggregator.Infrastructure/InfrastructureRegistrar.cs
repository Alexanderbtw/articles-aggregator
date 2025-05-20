using ArticlesAggregator.Infrastructure.Abstractions;
using ArticlesAggregator.Infrastructure.Abstractions.Repositories;
using ArticlesAggregator.Infrastructure.Repositories;

using Microsoft.Extensions.DependencyInjection;

namespace ArticlesAggregator.Infrastructure;

public static class InfrastructureRegistrar
{
    public static void Congigure(IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
    }
}

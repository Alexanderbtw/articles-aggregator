using ArticlesAggregator.Infrastructure.Abstractions;
using ArticlesAggregator.Infrastructure.Abstractions.Repositories;
using ArticlesAggregator.Infrastructure.Repositories;

using Dapper;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArticlesAggregator.Infrastructure;

public static class InfrastructureRegistrar
{
    public static void Congigure(IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>(provider =>
        {
            string connectionString = provider
                .GetRequiredService<IConfiguration>()
                .GetConnectionString("articles-aggregator-db")
                ?? throw new InvalidOperationException();

            return new SqlConnectionFactory(connectionString);
        });
        services.AddScoped<IArticleRepository, ArticleRepository>();
    }
}

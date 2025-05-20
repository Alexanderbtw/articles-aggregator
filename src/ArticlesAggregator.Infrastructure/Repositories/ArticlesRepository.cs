using System.Data;

using ArticlesAggregator.Infrastructure.Abstractions;
using ArticlesAggregator.Infrastructure.Abstractions.Entities;
using ArticlesAggregator.Infrastructure.Abstractions.Repositories;

using Dapper;

namespace ArticlesAggregator.Infrastructure.Repositories;

internal sealed class ArticleRepository(IDbConnectionFactory dbConnectionFactory) : IArticleRepository
{
    public async Task<bool> RemoveAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM Articles WHERE Id = @Id";
        using IDbConnection conn = await dbConnectionFactory.OpenAsync(ct);
        int affected = await conn.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<Guid> AddAsync(ArticleEntity entity, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO Articles (Id, Url, Title, Description, AddedBy, CreatedAt)
            VALUES (@Id, @Url, @Title, @Description, @AddedBy, @CreatedAt);
        ";

        using IDbConnection conn = await dbConnectionFactory.OpenAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Url,
            entity.Title,
            entity.Description,
        });

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(ArticleEntity entity, CancellationToken ct = default)
    {
        const string sql = @"
                UPDATE Articles
                   SET Url         = @Url,
                       Title       = @Title,
                       Description = @Description
                 WHERE Id = @Id;
            ";

        using IDbConnection conn = await dbConnectionFactory.OpenAsync(ct);

        int affected = await conn.ExecuteAsync(
            sql,
            new
            {
                entity.Url,
                entity.Title,
                entity.Description,
                entity.Id
            });

        return affected > 0;
    }

    public async Task<ArticleEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = @"
                SELECT Id, Url, Title, Description, AddedBy, CreatedAt
                FROM Articles
                WHERE Id = @Id
            ";
        using IDbConnection conn = await dbConnectionFactory.OpenAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ArticleEntity>(sql, new { Id = id });
    }

    public async Task<IEnumerable<ArticleEntity>> SearchByTitleAsync(string title, CancellationToken ct = default)
    {
        const string sql = @"
                SELECT Id, Url, Title, Description, AddedBy, CreatedAt
                FROM Articles
                WHERE Title LIKE @Pattern
                ORDER BY CreatedAt DESC
            ";

        var pattern = $"%{title}%";
        using IDbConnection conn = await dbConnectionFactory.OpenAsync(ct);

        return await conn.QueryAsync<ArticleEntity>(sql, new { Pattern = pattern });
    }
}

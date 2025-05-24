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
            INSERT INTO Articles (id, title, content, source_url)
            VALUES (@Id, @Title, @Content, @SourceUrl);
        ";

        using IDbConnection conn = await dbConnectionFactory.OpenAsync(ct);

        await conn.ExecuteAsync(
            sql,
            new
            {
                entity.Id,
                entity.Title,
                entity.Content,
                sourceurl = entity.SourceUrl
            });

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(ArticleEntity entity, CancellationToken ct = default)
    {
        const string sql = @"
                UPDATE Articles
                   SET title   = @Title,
                       content = @Content,
                       source_url = @SourceUrl
                 WHERE id = @Id;
            ";

        using IDbConnection conn = await dbConnectionFactory.OpenAsync(ct);

        int affected = await conn.ExecuteAsync(
            sql,
            new
            {
                entity.Title,
                entity.Content,
                sourceurl = entity.SourceUrl,
                entity.Id
            });

        return affected > 0;
    }

    public async Task<ArticleEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = @"
                SELECT id, title, content, source_url AS SourceUrl
                FROM Articles
                WHERE id = @Id
            ";

        using IDbConnection conn = await dbConnectionFactory.OpenAsync(ct);

        return await conn.QuerySingleOrDefaultAsync<ArticleEntity>(sql, new { Id = id });
    }

    public async Task<IEnumerable<ArticleEntity>> SearchByTitleAsync(string title, CancellationToken ct = default)
    {
        string[] terms = title
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Пример с AND: все слова должны присутствовать
        IEnumerable<string> whereClauses = terms
            .Select((_, i) => $"Title ILIKE @pattern{i}");

        var sql = $@"
            SELECT id, title, content, source_url AS SourceUrl
            FROM Articles
            WHERE {string.Join(" AND ", whereClauses)}
            ORDER BY Id DESC
        ";

        var dp = new DynamicParameters();

        for (var i = 0; i < terms.Length; i++)
        {
            dp.Add($"pattern{i}", $"%{terms[i]}%");
        }

        using IDbConnection conn = await dbConnectionFactory.OpenAsync(ct);

        return await conn.QueryAsync<ArticleEntity>(sql, dp);
    }
}

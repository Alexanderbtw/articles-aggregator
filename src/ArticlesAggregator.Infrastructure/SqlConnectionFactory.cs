using System.Data;

using ArticlesAggregator.Infrastructure.Abstractions;

using Microsoft.Data.SqlClient;

namespace ArticlesAggregator.Infrastructure;

internal sealed class SqlConnectionFactory(string connectionString)
    : IDbConnectionFactory
{
    public async Task<IDbConnection> OpenAsync(CancellationToken ct = default)
    {
        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        return conn;
    }
}

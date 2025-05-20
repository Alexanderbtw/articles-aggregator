using System.Data;

namespace ArticlesAggregator.Infrastructure.Abstractions;

public interface IDbConnectionFactory
{
    Task<IDbConnection> OpenAsync(CancellationToken ct = default);
}

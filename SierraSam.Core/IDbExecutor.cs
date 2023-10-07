using System.Data;

namespace SierraSam.Core;

public interface IDbExecutor
{
    IReadOnlyCollection<T> ExecuteReader<T>(
        string sql,
        Func<IDataReader, T> rowMapper,
        IDbTransaction? transaction = null
    );

    void ExecuteNonQuery(string sql, IDbTransaction? transaction = null);

    T? ExecuteScalar<T>(string sql, IDbTransaction? transaction = null) where T : class?;
}
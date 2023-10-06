using System.Data;
using System.Data.Odbc;
using SierraSam.Core.Exceptions;

namespace SierraSam.Core;

public sealed class OdbcExecutor
{
    private readonly IDbConnection _connection;

    public OdbcExecutor(IDbConnection connection)
    {
        _connection = connection
            ?? throw new ArgumentNullException(nameof(connection));
    }

    public IReadOnlyCollection<T> ExecuteReader<T>(
        string sql,
        Func<IDataReader, T> rowMapper,
        IDbTransaction? transaction = null)
    {
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (transaction is not null) command.Transaction = transaction;

            using var dataReader = command.ExecuteReader();

            var rows = new List<T>();
            while (dataReader.Read())
            {
                rows.Add(rowMapper(dataReader));
            }

            return rows;
        }
        catch (OdbcException exception)
        {
            throw new OdbcExecutorException(
                $"Failed to execute SQL statement: '{sql}'",
                exception);
        }
    }

    public void ExecuteNonQuery(string sql, IDbTransaction? transaction = null)
    {
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (transaction is not null) command.Transaction = transaction;

            command.ExecuteNonQuery();
        }
        catch (OdbcException exception)
        {
            throw new OdbcExecutorException(
                $"Failed to execute SQL statement: '{sql}'",
                exception);
        }
    }

    public T? ExecuteScalar<T>(string sql, IDbTransaction? transaction = null) where T : class?
    {
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (transaction is not null) command.Transaction = transaction;

            return command.ExecuteScalar() as T;
        }
        catch (OdbcException exception)
        {
            throw new OdbcExecutorException(
                $"Failed to execute SQL statement: '{sql}'",
                exception);
        }
    }
}
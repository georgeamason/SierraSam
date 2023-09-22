using System.Data.Odbc;
using SierraSam.Core.Exceptions;

namespace SierraSam.Core;

public sealed class OdbcExecutor
{
    private readonly OdbcConnection _connection;

    public OdbcExecutor(OdbcConnection connection)
    {
        _connection = connection
            ?? throw new ArgumentNullException(nameof(connection));
    }

    public IReadOnlyCollection<T> ExecuteReader<T>(
        string sql,
        Func<OdbcDataReader, T> rowMapper,
        OdbcTransaction? transaction = null)
    {
        try
        {
            using var command = new OdbcCommand(sql, _connection);

            if (transaction is not null)
                command.Transaction = transaction;

            using var dataReader = command.ExecuteReader();

            if (!dataReader.HasRows) return Array.Empty<T>();

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

    public void ExecuteNonQuery(string sql, OdbcTransaction? transaction = null)
    {
        try
        {
            using var command = new OdbcCommand(sql, _connection);

            if (transaction is not null)
                command.Transaction = transaction;

            command.ExecuteNonQuery();
        }
        catch (OdbcException exception)
        {
            throw new OdbcExecutorException(
                $"Failed to execute SQL statement: '{sql}'",
                exception);
        }
    }
}
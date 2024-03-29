﻿using System.Data;
using System.Data.Odbc;
using SierraSam.Core.Exceptions;

namespace SierraSam.Core;

public sealed class DbExecutor : IDbExecutor
{
    private readonly IDbConnection _connection;

    public DbExecutor(IDbConnection connection)
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

    public int ExecuteNonQuery(string sql, IDbTransaction? transaction = null)
    {
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (transaction is not null) command.Transaction = transaction;

            return command.ExecuteNonQuery();
        }
        catch (OdbcException exception)
        {
            throw new OdbcExecutorException(
                $"Failed to execute SQL statement: '{sql}'",
                exception);
        }
    }

    public T? ExecuteScalar<T>(string sql, IDbTransaction? transaction = null)
    {
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (transaction is not null) command.Transaction = transaction;

            return command.ExecuteScalar() switch
            {
                T result => result,
                DBNull => default,
                { } result and not T => throw new ArgumentException($"Return type '{result.GetType()}' did not match '{typeof(T)}'", nameof(T)),
                _ => throw new Exception("Unexpected error")
            };
        }
        catch (OdbcException exception)
        {
            throw new OdbcExecutorException(
                $"Failed to execute SQL statement: '{sql}'",
                exception);
        }
    }
}
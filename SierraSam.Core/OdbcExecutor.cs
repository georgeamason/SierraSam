﻿using System.Data;
using System.Data.Odbc;

namespace SierraSam.Core;

public sealed class OdbcExecutor
{
    private readonly OdbcConnection _connection;

    public OdbcExecutor(OdbcConnection connection)
    {
        _connection = connection
            ?? throw new ArgumentNullException(nameof(connection));
    }

    public IEnumerable<T> ExecuteReader<T>(string sql, Func<OdbcDataReader, T> rowMapper)
    {
        using var command = new OdbcCommand(sql, _connection);
        using var adapter = new OdbcDataAdapter(command);

        var dataReader = command.ExecuteReader();

        if (!dataReader.HasRows)
            yield break;

        while (dataReader.Read())
            yield return rowMapper(dataReader);
    }
}
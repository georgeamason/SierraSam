using System.Data;
using System.Data.Odbc;

namespace SierraSam.Core;

public sealed class OdbcCommandWrapper : IDisposable
{
    private readonly OdbcCommand _odbcCommand;

    public OdbcCommandWrapper
        (OdbcConnection odbcConnection,
         CommandType type,
         string text)
    {
        _odbcCommand = odbcConnection.CreateCommand();
        _odbcCommand.CommandType = type;
        _odbcCommand.CommandText = text;
    }

    public void Execute()
    {
        using var dataReader = _odbcCommand.ExecuteReader();
        
        if (!dataReader.HasRows) return;

        while (dataReader.Read())
        {
            var row = new Dictionary<string, object>();
            for (var i = 0; i < dataReader.FieldCount; i++)
            {
                var column = dataReader.GetName(i);
                var data = dataReader[column];
                row.Add(column, data);
            }

            foreach (var (key, value) in row)
            {
                Console.WriteLine($"{key}: {value}");
            }
        }
    }

    public void Dispose()
    {
        _odbcCommand.Dispose();
    }
}
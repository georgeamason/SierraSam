using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using Microsoft.Extensions.Logging;
using SierraSam.Core;

namespace SierraSam.Capabilities;

public sealed class Migrate : ICapability
{
    public Migrate(ILogger<Migrate> logger, Configuration configuration)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void Run(string[] args)
    {
        _logger.LogInformation($"{nameof(Migrate)} running");

        // Do the actual migrate based on options from config
        try
        {
            var builder = new DbConnectionStringBuilder(true)
            {
                ConnectionString = _configuration.Url
            };

            using var connection = new OdbcConnection(builder.ConnectionString)
            {
                ConnectionTimeout = _configuration.ConnectionTimeout
            };

            connection.Open();
            _logger.LogInformation($"Driver: {connection.Driver}");
            _logger.LogInformation($"Database: {connection.Database}");

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT TOP 10 * FROM master.sys.all_columns";
            command.CommandType = CommandType.Text;

            using var dataReader = command.ExecuteReader();

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

                foreach (var kvp in row)
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }
            }
        }
        catch (Exception exception)
        {
            var msg = exception switch
            {
                ArgumentException => "The connection string is not formatted correctly",
                OdbcException => "Unable to connect to the server",
                _ => "Unknown exception"
            };

            _logger.LogError(msg, exception);
        }
    }

    private readonly ILogger _logger;

    private readonly Configuration _configuration;
}
using System.Data.Odbc;
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.Exceptions;

namespace SierraSam.Capabilities;

internal sealed class Clean : ICapability
{
    private readonly ILogger<Clean> _logger;
    private readonly IDatabase _database;
    private readonly IConfiguration _configuration;

    public Clean(ILogger<Clean> logger, IDatabase database, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Clean)} is running");

        var schemas = _configuration.Schemas.Any()
            ? _configuration.Schemas.ToArray()
            : new[] { _configuration.DefaultSchema };

        using var transaction = _database.Connection.BeginTransaction();
        try
        {
            foreach (var schema in schemas)
            {
                var i = 1;
                foreach (var obj in _database.GetSchemaObjects(schema, transaction))
                {
                    _logger.LogInformation("{index}: {objectName}", i, obj.Name);
                    _database.DropSchemaObject(obj, transaction);
                    i++;
                }
            }

            transaction.Commit();

            ColorConsole.SuccessLine
                ($"Cleaned schema(s) \"{string.Join(", ", schemas)}\"");
        }
        catch (Exception exception)
        when (exception is OdbcException or ArgumentOutOfRangeException)
        {
            transaction.Rollback();
            throw new CleanException($"Failed to clean schema(s)", exception);
        }
    }
}
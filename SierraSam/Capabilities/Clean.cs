using System.Data.Odbc;
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.Exceptions;
using Spectre.Console;

namespace SierraSam.Capabilities;

internal sealed class Clean : ICapability
{
    private readonly ILogger<Clean> _logger;
    private readonly IDatabase _database;
    private readonly IConfiguration _configuration;
    private readonly IAnsiConsole _console;

    public Clean(
        ILogger<Clean> logger,
        IDatabase database,
        IConfiguration configuration,
        IAnsiConsole console)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _console = console ?? throw new ArgumentNullException(nameof(console));
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
                _database.Clean(schema, transaction);
            }

            transaction.Commit();

            _console.MarkupLine(
                $"[green]Cleaned schema(s) \"{string.Join(", ", schemas)}\"[/]"
            );
        }
        catch (OdbcExecutorException exception)
        {
            transaction.Rollback();

            throw new CleanException(
                $"Failed to clean schema(s)",
                exception
            );
        }
    }
}
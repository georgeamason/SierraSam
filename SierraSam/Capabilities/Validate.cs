using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SierraSam.Core.MigrationValidators;
using Spectre.Console;

namespace SierraSam.Capabilities;

public sealed class Validate : ICapability
{
    private readonly ILogger<Validate> _logger;
    private readonly IMigrationValidator _migrationValidator;
    private readonly IAnsiConsole _console;

    public Validate(
        ILogger<Validate> logger,
        IMigrationValidator migrationValidator,
        IAnsiConsole console)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _migrationValidator = migrationValidator
            ?? throw new ArgumentNullException(nameof(migrationValidator));

        _console = console
            ?? throw new ArgumentNullException(nameof(console));
    }

    public Task Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Validate)} running");

        // if (_database.Connection.State is not ConnectionState.Open) _database.Connection.Open();

        var stopwatch = Stopwatch.StartNew();
        var validated = _migrationValidator.Validate();
        stopwatch.Stop();

        _console.MarkupLine(
            $"[green]Successfully validated {validated} migrations " +
            $@"(execution time {stopwatch.Elapsed:mm\:ss\.fff}s)[/]"
        );

        return Task.CompletedTask;
    }
}
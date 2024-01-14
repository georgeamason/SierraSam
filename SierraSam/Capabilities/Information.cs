using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.Serializers;
using Spectre.Console;
namespace SierraSam.Capabilities;

internal sealed class Information : ICapability
{
    private readonly ILogger<Information> _logger;
    private readonly IMigrationAggregator _migrationAggregator;
    private readonly ISerializer _serializer;
    private readonly IAnsiConsole _console;

    public Information(
        ILogger<Information> logger,
        IMigrationAggregator migrationAggregator,
        ISerializer serializer,
        IAnsiConsole console)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _migrationAggregator = migrationAggregator
            ?? throw new ArgumentNullException(nameof(migrationAggregator));

        _serializer = serializer
            ?? throw new ArgumentNullException(nameof(serializer));

        _console = console
            ?? throw new ArgumentNullException(nameof(console));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Information)} running");

        var migrations = _migrationAggregator.GetAllMigrations();

        if (migrations.Count == 0)
        {
            _console.WriteLine("No migrations found");

            return;
        }

        var content = _serializer.Serialize(migrations);

        _console.Write(content);
    }
}
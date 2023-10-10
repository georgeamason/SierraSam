using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.Serializers;
using Spectre.Console;
namespace SierraSam.Capabilities;

internal sealed class Information : ICapability
{
    private readonly ILogger<Information> _logger;
    private readonly IMigrationMerger _migrationMerger;
    private readonly ISerializer _serializer;

    public Information(
        ILogger<Information> logger,
        IMigrationMerger migrationMerger,
        ISerializer serializer)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _migrationMerger = migrationMerger
            ?? throw new ArgumentNullException(nameof(migrationMerger));

        _serializer = serializer
            ?? throw new ArgumentNullException(nameof(serializer));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Information)} running");

        var migrations = _migrationMerger.Merge();

        if (!migrations.Any())
        {
            AnsiConsole.WriteLine("No migrations found");

            return;
        }

        var content = _serializer.Serialize(migrations);

        AnsiConsole.Write(content);
    }
}
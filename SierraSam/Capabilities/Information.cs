using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.Enums;
using Spectre.Console;

namespace SierraSam.Capabilities;

internal sealed class Information : ICapability
{
    private readonly ILogger<Information> _logger;
    private readonly IMigrationMerger _migrationMerger;

    public Information(ILogger<Information> logger, IMigrationMerger migrationMerger)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _migrationMerger = migrationMerger
            ?? throw new ArgumentNullException(nameof(migrationMerger));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Information)} running");

        var migrations = _migrationMerger
            .Merge()
            .ToArray()
            .AsReadOnly();

        var table = new Table { Border = TableBorder.Ascii2 };

        var columns = new[] { "Category", "Version", "Description", "Type", "Installed On", "State" };

        foreach (var col in columns) table.AddColumn($"[{Color.Default}]{col}[/]");

        foreach (var migration in migrations)
        {
            var rowColor = migration.State switch
            {
                MigrationState.Missing => Color.Red,
                MigrationState.Pending => Color.Yellow,
                _ => Color.Default
            };

            table.AddRow
                ($"[{rowColor}]{migration.MigrationType}[/]",
                 $"[{rowColor}]{migration.Version ?? string.Empty}[/]",
                 $"[{rowColor}]{migration.Description}[/]",
                 $"[{rowColor}]{migration.Type}[/]",
                 $"[{rowColor}]{migration.InstalledOn?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty}[/]",
                 $"[{rowColor}]{migration.State}[/]");
        }

        AnsiConsole.Write(table);
    }
}
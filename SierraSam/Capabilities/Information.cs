using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.Enums;
using SierraSam.Core.MigrationSeekers;
using Spectre.Console;

namespace SierraSam.Capabilities;

internal sealed class Information : ICapability
{
    private readonly ILogger<Information> _logger;
    private readonly IDatabase _database;
    private readonly Configuration _configuration;
    private readonly IMigrationSeeker _migrationSeeker;

    public Information
        (ILogger<Information> logger,
         IDatabase database,
         Configuration configuration,
         IMigrationSeeker migrationSeeker)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _database = database
            ?? throw new ArgumentNullException(nameof(database));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _migrationSeeker = migrationSeeker
            ?? throw new ArgumentNullException(nameof(migrationSeeker));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Information)} running");

        var discoveredMigrations = _migrationSeeker
            .Find()
            .Select(m => new TerseMigration(
                m.MigrationType,
                m.Version,
                m.Description,
                "SQL",
                m.Checksum,
                null,
                MigrationState.Pending));

        var appliedMigrations = _database
            .GetSchemaHistory(_configuration.DefaultSchema, _configuration.SchemaTable)
            .Select(m =>
            {
                var isDiscovered = discoveredMigrations
                    .Select(x => x.Checksum)
                    .Contains(m.Checksum);

                return new TerseMigration(
                    m.MigrationType,
                    m.Version,
                    m.Description,
                    m.Type,
                    m.Checksum,
                    m.InstalledOn,
                    isDiscovered ? MigrationState.Applied : MigrationState.Missing);
            });

        var migrationsUnion = appliedMigrations
            .UnionBy(discoveredMigrations, migration => migration.Checksum)
            .OrderBy(m => m.InstalledOn ?? DateTime.MaxValue)
            .ThenBy(m => m.Version);

        var table = new Table { Border = TableBorder.Ascii2 };

        foreach (var col in new[] { "Category", "Version", "Description", "Type", "Installed On", "State" })
        {
            var column = new TableColumn($"[{Color.Default}]{col}[/]").NoWrap();
            table.AddColumn(column);
        }

        foreach (var migration in migrationsUnion)
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
                 $"[{rowColor}]{migration.InstalledOn?.ToString("u") ?? string.Empty}[/]",
                 $"[{rowColor}]{migration.State}[/]");
        }

        AnsiConsole.Write(table);
    }

    private record TerseMigration(
        MigrationType MigrationType,
        string? Version,
        string Description,
        string Type,
        string Checksum,
        DateTime? InstalledOn,
        MigrationState State);
}
using System.IO.Abstractions;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Core.MigrationValidators;
using Spectre.Console;

namespace SierraSam.Capabilities;

internal sealed class Rollup(
    ILogger<Rollup> logger,
    IDatabase database,
    IConfiguration configuration,
    IMigrationValidator validator,
    IMigrationSeeker migrationSeeker,
    IFileSystem fileSystem,
    IAnsiConsole console
) : ICapability
{
    private readonly ILogger<Rollup> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDatabase _database = database ?? throw new ArgumentNullException(nameof(database));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IMigrationValidator _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly IMigrationSeeker _migrationSeeker = migrationSeeker ?? throw new ArgumentNullException(nameof(migrationSeeker));
    private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private readonly IAnsiConsole _console = console ?? throw new ArgumentNullException(nameof(console));

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Rollup)} running");

        _validator.Validate();

        var appliedMigrations = _database.GetAppliedMigrations().ToArray();
        var discoveredMigrations = _migrationSeeker.GetPendingMigrations().ToArray();

        var stringBuilder = new StringBuilder();
        for (var i = 0; i < appliedMigrations.Length; i++)
        {
            if (i >= 1) stringBuilder.Append(Environment.NewLine);

            var appliedMigration = appliedMigrations[i];
            stringBuilder.Append($"--{appliedMigration.Script}--" + Environment.NewLine);

            var discoveredMigration = discoveredMigrations.SingleOrDefault(
                migration => migration.Checksum == appliedMigration.Checksum
            );

            // This could happen if `migrationsToIgnore` is used
            if (discoveredMigration is null)
            {
                throw new Exception($"{appliedMigration.Script} was not found in the configured search paths.");
            }

            stringBuilder.Append(discoveredMigration.Sql + Environment.NewLine);
        }

        var filePath = Path.Combine(_configuration.ExportDirectory, "rollup.sql");
        _fileSystem.File.WriteAllText(filePath, stringBuilder.ToString());

        _console.MarkupLine($"Rollup file written to [link]{filePath}[/]");
    }
}
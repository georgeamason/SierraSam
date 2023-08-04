using System.Data;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.Extensions;
using SierraSam.Core.MigrationSeekers;

namespace SierraSam.Capabilities;

public sealed class Validate : ICapability
{
    private readonly ILogger<Validate> _logger;

    private readonly IDatabase _database;

    private readonly Configuration _configuration;

    private readonly IMigrationSeeker _migrationSeeker;

    private readonly IFileSystem _fileSystem;

    public Validate
        (ILogger<Validate> logger,
         IDatabase database,
         Configuration configuration,
         IMigrationSeeker migrationSeeker,
         IFileSystem fileSystem)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _database = database
                    ?? throw new ArgumentNullException(nameof(database));

        _configuration = configuration
                         ?? throw new ArgumentNullException(nameof(configuration));

        _migrationSeeker = migrationSeeker
                           ?? throw new ArgumentNullException(nameof(migrationSeeker));

        _fileSystem = fileSystem
                      ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Validate)} running");

        if (_database.Connection.State is not ConnectionState.Open) _database.Connection.Open();

        var appliedMigrations = _database.GetSchemaHistory
            (_configuration.DefaultSchema, _configuration.SchemaTable);

        var discoveredMigrations = _migrationSeeker.Find();

        // TODO: Extract this out using decorator pattern
        var isValid = appliedMigrations
            .Select(appliedMigration =>
            {
                return discoveredMigrations.Any(discoveredMigration =>
                {
                    var migrationSql = _fileSystem.File.ReadAllText
                        (discoveredMigration.FilePath);

                    // - [x] differences in migration names, types or checksums are found
                    // - [ ] versions have been applied that haven't been discovered locally
                    // - [ ] versions have been discovered that haven't been applied yet (default is to ignore this, obvs)

                    return discoveredMigration.FileName == appliedMigration.Script &&
                           migrationSql.Checksum() == appliedMigration.Checksum;
                });
            })
            .All(validated => validated);

        if (!isValid)
        {
            throw new Exception("Validation checks failed.");
        }

        ColorConsole.WriteLine($"Successfully validated {discoveredMigrations.Count} migrations.");
    }
}
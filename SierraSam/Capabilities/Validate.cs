using System.Data;
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.MigrationSeekers;
using SierraSam.Core.MigrationValidators;
using Console = SierraSam.Core.ColorConsole;

namespace SierraSam.Capabilities;

public sealed class Validate : ICapability
{
    private readonly ILogger<Validate> _logger;

    private readonly IDatabase _database;

    private readonly Configuration _configuration;

    private readonly IMigrationSeeker _migrationSeeker;

    private readonly IMigrationValidator _migrationValidator;

    public Validate
        (ILogger<Validate> logger,
         IDatabase database,
         Configuration configuration,
         IMigrationSeeker migrationSeeker,
         IMigrationValidator migrationValidator)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _database = database
            ?? throw new ArgumentNullException(nameof(database));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _migrationSeeker = migrationSeeker
            ?? throw new ArgumentNullException(nameof(migrationSeeker));

        _migrationValidator = migrationValidator
            ?? throw new ArgumentNullException(nameof(migrationValidator));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Validate)} running");

        if (_database.Connection.State is not ConnectionState.Open) _database.Connection.Open();

        var appliedMigrations = _database.GetSchemaHistory
            (_configuration.DefaultSchema, _configuration.SchemaTable);

        var discoveredMigrations = _migrationSeeker.Find();

        // TODO: Should migration validator have a constructor and not take args in Validate?
        var executionTime = _migrationValidator.Validate
            (appliedMigrations, discoveredMigrations);

        Console.SuccessLine($"Successfully validated {discoveredMigrations.Count} migrations " +
                            $"(execution time: {executionTime:mm\\:ss\\.fff}s)");
    }
}
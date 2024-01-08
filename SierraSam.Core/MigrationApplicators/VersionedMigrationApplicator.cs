using System.Data;
using SierraSam.Core.Enums;
using SierraSam.Core.Exceptions;
using Spectre.Console;

namespace SierraSam.Core.MigrationApplicators;

public sealed class VersionedMigrationApplicator : IMigrationApplicator
{
    private readonly IDatabase _database;
    private readonly IConfiguration _configuration;
    private readonly IAnsiConsole _console;
    private readonly TimeProvider _timeProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public VersionedMigrationApplicator(
        IDatabase database,
        IConfiguration configuration,
        IAnsiConsole console,
        TimeProvider timeProvider
    )
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public int Apply(PendingMigration pendingMigration, IDbTransaction transaction)
    {
        if (pendingMigration.MigrationType is not MigrationType.Versioned)
        {
            throw new ArgumentException(
                $"Migration type \"{pendingMigration.MigrationType}\" is not supported by this applicator.",
                nameof(pendingMigration)
            );
        }

        try
        {
            _console.WriteLine($"Migrating schema \"{_configuration.DefaultSchema}\" " +
                               $"to version {pendingMigration.Version} - {pendingMigration.Description}");

            var installRank = _database.GetInstalledRank(transaction: transaction);

            var executionTime = _database.ExecuteMigration(pendingMigration.Sql, transaction);

            var migrationToApply = new AppliedMigration(
                ++installRank,
                pendingMigration.Version,
                pendingMigration.Description,
                "SQL",
                pendingMigration.FileName,
                pendingMigration.Checksum,
                _configuration.InstalledBy,
                _timeProvider.GetUtcNow().UtcDateTime,
                executionTime.TotalMilliseconds,
                true);

            return _database.InsertSchemaHistory(migrationToApply, transaction);
        }
        catch (OdbcExecutorException exception)
        {
            transaction.Rollback();

            throw new MigrationApplicatorException(
                $"Failed to apply migration \"{pendingMigration.FileName}\"; rolled back the transaction.",
                exception
            );
        }
    }
}
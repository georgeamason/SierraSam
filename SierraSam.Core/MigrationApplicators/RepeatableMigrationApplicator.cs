using System.Data;
using SierraSam.Core.Enums;
using SierraSam.Core.Exceptions;
using SierraSam.Core.Extensions;
using Spectre.Console;

namespace SierraSam.Core.MigrationApplicators;

public sealed class RepeatableMigrationApplicator : IMigrationApplicator
{
    private readonly IDatabase _database;
    private readonly IConfiguration _configuration;
    private readonly IAnsiConsole _console;
    private readonly TimeProvider _timeProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RepeatableMigrationApplicator(
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
        if (pendingMigration.MigrationType is not MigrationType.Repeatable)
        {
            throw new ArgumentException(
                $"Migration type \"{pendingMigration.MigrationType}\" is not supported by this applicator.",
                nameof(pendingMigration)
            );
        }

        try
        {
            var appliedMigrations = _database.GetAppliedMigrations(transaction: transaction);

            if (appliedMigrations.Any(m => m.Checksum == pendingMigration.Checksum)) return 0;

            _console.WriteLine($"Applying repeatable migration - {pendingMigration.Description}");

            var appliedMigration = appliedMigrations.SingleOrDefault(
                m => m.Script == pendingMigration.FileName
            );

            if (appliedMigration is not null)
            {
                var updatedMigration = appliedMigration
                    .WithChecksum(pendingMigration.Checksum)
                    .WithInstalledOn(_timeProvider.GetUtcNow().UtcDateTime);

                // https://stackoverflow.com/questions/63091283/flyway-always-execute-repeatable-migrations
                // https://stackoverflow.com/questions/42930738/flyway-and-initialization-of-repeatable-migrations
                _database.ExecuteMigration(pendingMigration.Sql, transaction);

                return _database.UpdateSchemaHistory(updatedMigration, transaction);
            }

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
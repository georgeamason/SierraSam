using System.Data;
using SierraSam.Core.Exceptions;
using SierraSam.Core.Extensions;
using Spectre.Console;

namespace SierraSam.Core.MigrationApplicators;

public sealed class RepeatableMigrationApplicator : IMigrationApplicator
{
    private readonly IDatabase _database;
    private readonly IConfiguration _configuration;
    private readonly IAnsiConsole _console;

    public RepeatableMigrationApplicator(
        IDatabase database,
        IConfiguration configuration,
        IAnsiConsole console
    )
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    public int Apply(PendingMigration pendingMigration, IDbTransaction transaction
    )
    {
        var appliedMigrations = _database.GetSchemaHistory();

        if (appliedMigrations.Any(m => m.Checksum == pendingMigration.Checksum)) return 0;

        _console.WriteLine($"Applying repeatable migration - {pendingMigration.Description}");

        var appliedMigration = appliedMigrations.SingleOrDefault(
            m => m.Script == pendingMigration.FileName
        );

        if (appliedMigration is not null)
        {
            var updatedMigration = appliedMigration
                .WithChecksum(pendingMigration.Checksum)
                .WithInstalledOn(DateTime.UtcNow);

            try
            {
                // TODO: Is this correct behaviour? Not sure we should be rewriting history
                _database.UpdateSchemaHistory(updatedMigration, transaction);
                _database.ExecuteMigration(pendingMigration.Sql, transaction);

                return 0;
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

        try
        {
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
                DateTime.UtcNow,
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
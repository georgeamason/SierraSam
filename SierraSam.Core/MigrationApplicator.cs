using System.Data;
using System.Data.Odbc;
using System.IO.Abstractions;
using SierraSam.Core.Enums;
using SierraSam.Core.Exceptions;
using SierraSam.Core.Extensions;

namespace SierraSam.Core;

public sealed class MigrationApplicator : IMigrationApplicator
{
    private readonly IDatabase _database;
    private readonly IConfiguration _configuration;

    public MigrationApplicator(IDatabase database, IConfiguration configuration)
    {
        _database = database
            ?? throw new ArgumentNullException(nameof(database));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    // TODO: Can I refactor this further? Maybe an applicator for each of the migration types?
    public (int appliedMigrations, TimeSpan executionTime) Apply
        (IReadOnlyCollection<PendingMigration> pendingMigrations,
         IReadOnlyCollection<AppliedMigration> appliedMigrations)
    {
        var installRank = appliedMigrations
            .Select(m => m.InstalledRank)
            .DefaultIfEmpty(0)
            .Max();

        if (_database.Connection.State is not ConnectionState.Open) _database.Connection.Open();

        // TODO: Make use of IDbTransaction. Something like _database.BeginTransaction()?
        using var transaction = _database.Connection.BeginTransaction();
        var appliedCount = 0;
        var totalExecutionTime = TimeSpan.Zero;
        foreach (var pendingMigration in pendingMigrations)
        {
            try
            {
                if (pendingMigration.MigrationType is MigrationType.Versioned)
                {
                    Console.WriteLine($"Migrating schema \"{_configuration.DefaultSchema}\" " +
                                      $"to version {pendingMigration.Version} - {pendingMigration.Description}");
                }

                var checksum = pendingMigration.Checksum;
                if (pendingMigration.MigrationType is MigrationType.Repeatable)
                {
                    if (appliedMigrations.Any(m => m.Checksum == checksum)) continue;

                    Console.WriteLine($"Applying repeatable migration - {pendingMigration.Description}");

                    var appliedMigration = appliedMigrations.SingleOrDefault
                        (m => m.Script == pendingMigration.FileName);

                    if (appliedMigration is not null)
                    {
                        var updatedMigration = appliedMigration
                            .WithChecksum(checksum)
                            .WithInstalledOn(DateTime.UtcNow);

                        _database.UpdateSchemaHistory(updatedMigration, transaction);

                        totalExecutionTime += _database.ExecuteMigration(pendingMigration.Sql, transaction);

                        continue;
                    }
                }

                var executionTime = _database.ExecuteMigration(pendingMigration.Sql, transaction);
                totalExecutionTime += executionTime;

                var migrationToApply = new AppliedMigration(
                    ++installRank,
                    pendingMigration.Version,
                    pendingMigration.Description,
                    "SQL",
                    pendingMigration.FileName,
                    checksum,
                    _configuration.InstalledBy,
                    DateTime.UtcNow,
                    executionTime.TotalMilliseconds,
                    true);

                _database.InsertSchemaHistory(migrationToApply, transaction);

                appliedCount++;
            }
            catch (OdbcExecutorException exception)
            {
                transaction.Rollback();

                throw new MigrationApplicatorException
                    ($"Failed to apply migration \"{pendingMigration.FileName}\"; rolled back the transaction.",
                     exception);
            }
        }

        transaction.Commit();

        return (appliedCount, totalExecutionTime);
    }
}
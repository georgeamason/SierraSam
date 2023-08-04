using System.Data;
using System.Data.Odbc;
using System.IO.Abstractions;
using SierraSam.Core.Exceptions;
using SierraSam.Core.Extensions;

namespace SierraSam.Core;

public sealed class MigrationApplicator : IMigrationApplicator
{
    private readonly IDatabase _database;

    private readonly IFileSystem _fileSystem;

    private readonly Configuration _configuration;

    public MigrationApplicator
        (IDatabase database,
         IFileSystem fileSystem,
         Configuration configuration)
    {
        _database = database
            ?? throw new ArgumentNullException(nameof(database));

        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public (int appliedMigrations, TimeSpan executionTime) Apply
        (IReadOnlyCollection<PendingMigration> pendingMigrations,
         IReadOnlyCollection<AppliedMigration> appliedMigrations)
    {
        var installRank = appliedMigrations
            .Select(m => m.InstalledRank)
            .DefaultIfEmpty(0)
            .Max();

        if (_database.Connection.State is not ConnectionState.Open) _database.Connection.Open();

        using var transaction = _database.Connection.BeginTransaction();
        var appliedCount = 0;
        var executionTime = TimeSpan.Zero;
        foreach (var pendingMigration in pendingMigrations)
        {
            try
            {
                var migrationSql = _fileSystem.File.ReadAllText(pendingMigration.FilePath);

                if (pendingMigration.MigrationType is MigrationType.Versioned)
                {
                    Console.WriteLine($"Migrating schema \"{_configuration.DefaultSchema}\" " +
                                      $"to version {pendingMigration.Version} - {pendingMigration.Description}");
                }

                var checksum = migrationSql.Checksum();
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

                        _database.UpdateSchemaHistory(transaction, updatedMigration);

                        executionTime += _database.ExecuteMigration(transaction, migrationSql);

                        continue;
                    }
                }

                executionTime += _database.ExecuteMigration(transaction, migrationSql);

                var migration = new AppliedMigration(
                    ++installRank,
                    pendingMigration.Version,
                    pendingMigration.Description,
                    "SQL",
                    pendingMigration.FileName,
                    checksum,
                    _configuration.InstalledBy,
                    default,
                    executionTime.TotalMilliseconds,
                    true);

                _database.InsertSchemaHistory(transaction, migration);

                appliedCount++;
            }
            catch (OdbcException exception)
            {
                transaction.Rollback();

                throw new MigrationApplicatorException
                    ($"Failed to apply migration \"{pendingMigration}\"; rolled back the transaction.",
                     exception);
            }
        }

        transaction.Commit();

        return (appliedCount, executionTime);
    }
}
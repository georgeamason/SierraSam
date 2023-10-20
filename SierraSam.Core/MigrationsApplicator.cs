using SierraSam.Core.MigrationApplicators;
using static SierraSam.Core.Enums.MigrationType;

namespace SierraSam.Core;

public sealed class MigrationsApplicator : IMigrationsApplicator
{
    private readonly IDatabase _database;
    private readonly IMigrationApplicatorResolver _applicatorResolver;


    public MigrationsApplicator(IDatabase database, IMigrationApplicatorResolver applicatorResolver)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _applicatorResolver = applicatorResolver ?? throw new ArgumentNullException(nameof(applicatorResolver));
    }

    public int Apply(IEnumerable<PendingMigration> pendingMigrations)
    {
        using var transaction = _database.Connection.BeginTransaction();
        var appliedCount = 0;
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var pendingMigration in pendingMigrations)
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            var applicator = pendingMigration.MigrationType switch
            {
                Versioned => _applicatorResolver.Resolve(typeof(VersionedMigrationApplicator)),
                Repeatable => _applicatorResolver.Resolve(typeof(RepeatableMigrationApplicator)),
                _ => throw new ArgumentOutOfRangeException(nameof(pendingMigration.MigrationType))
            };

            appliedCount += applicator.Apply(pendingMigration, transaction);
        }

        transaction.Commit();

        return appliedCount;
    }
}
using SierraSam.Core.MigrationApplicators;
using static SierraSam.Core.Enums.MigrationType;

namespace SierraSam.Core;

public sealed class MigrationsApplicator : IMigrationsApplicator
{
    private readonly IDatabase _database;
    private readonly IMigrationApplicatorResolver _applicatorResolver;
    private readonly TimeProvider _timeProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public MigrationsApplicator(
        IDatabase database,
        IMigrationApplicatorResolver applicatorResolver,
        TimeProvider timeProvider
    )
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _applicatorResolver = applicatorResolver ?? throw new ArgumentNullException(nameof(applicatorResolver));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public int Apply(IEnumerable<PendingMigration> pendingMigrations, out TimeSpan executionTime)
    {
        var start = _timeProvider.GetTimestamp();
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

        executionTime = _timeProvider.GetElapsedTime(start);
        return appliedCount;
    }
}
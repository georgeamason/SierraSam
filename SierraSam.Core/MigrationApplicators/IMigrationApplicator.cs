using System.Data;

namespace SierraSam.Core.MigrationApplicators;

public interface IMigrationApplicator
{
    int Apply(PendingMigration pendingMigration, IDbTransaction transaction);
}
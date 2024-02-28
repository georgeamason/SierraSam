namespace SierraSam.Core;

public interface IMigrationRepairer
{
    void Repair(IDictionary<AppliedMigration, PendingMigration> repairs);
}
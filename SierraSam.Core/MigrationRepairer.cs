namespace SierraSam.Core;

public sealed class MigrationRepairer(IDatabase database) : IMigrationRepairer
{
    public void Repair(IDictionary<AppliedMigration, PendingMigration> repairs)
    {
        using var transaction = database.Connection.BeginTransaction();
        foreach (var (toRepair, repairWith) in repairs)
        {
            var repairedMigration = toRepair with
            {
                Description = repairWith.Description,
                Checksum = repairWith.Checksum
            };

            database.UpdateSchemaHistory(repairedMigration, transaction);
        }

        transaction.Commit();
    }
}
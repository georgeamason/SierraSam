namespace SierraSam.Core.MigrationApplicators;

public interface IMigrationApplicatorResolver
{
    IMigrationApplicator Resolve(Type type);
}
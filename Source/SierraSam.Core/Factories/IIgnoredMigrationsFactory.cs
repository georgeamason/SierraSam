using SierraSam.Core.Enums;

namespace SierraSam.Core.Factories;

public interface IIgnoredMigrationsFactory
{
    IReadOnlyCollection<(MigrationType Type, MigrationState State)> Create();
}
using SierraSam.Core.Enums;

namespace SierraSam.Core;

public record TerseMigration(
    MigrationType MigrationType,
    string? Version,
    string Description,
    string Type,
    string Checksum,
    DateTime? InstalledOn,
    MigrationState State);
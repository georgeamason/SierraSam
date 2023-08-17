using SierraSam.Core.Enums;

namespace SierraSam.Core;

public sealed class AppliedMigration
{
    public AppliedMigration
        (int installedRank,
         string? version,
         string description,
         string type,
         string script,
         string checksum,
         string installedBy,
         DateTime installedOn,
         double executionTime,
         bool success)
    {
        InstalledRank = installedRank;
        Version = version;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Script = script ?? throw new ArgumentNullException(nameof(script));
        Checksum = checksum ?? throw new ArgumentNullException(nameof(checksum));
        InstalledBy = installedBy ?? throw new ArgumentNullException(nameof(installedBy));
        InstalledOn = installedOn;
        ExecutionTime = executionTime;
        Success = success;
    }

    public int InstalledRank { get; }

    public string? Version { get; }

    public string Description { get; }

    public string Type { get; }

    public string Script { get; }

    public string Checksum { get; }

    public string InstalledBy { get; }

    public DateTime InstalledOn { get; }

    public double ExecutionTime { get; }

    public bool Success { get; }

    public MigrationType MigrationType => Version switch
    {
        null => MigrationType.Repeatable,
        _ => MigrationType.Versioned
    };
}
using SierraSam.Core.Enums;

namespace SierraSam.Core;

public sealed record AppliedMigration
{
    public AppliedMigration(
        int installedRank,
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
        if (version == string.Empty)
        {
            throw new ArgumentException("Cannot be empty", nameof(version));
        }

        if (description == string.Empty)
        {
            throw new ArgumentException("Cannot be empty", nameof(description));
        }

        if (type == string.Empty)
        {
            throw new ArgumentException("Cannot be empty", nameof(type));
        }

        if (script == string.Empty)
        {
            throw new ArgumentException("Cannot be empty", nameof(script));
        }

        if (checksum == string.Empty)
        {
            throw new ArgumentException("Cannot be empty", nameof(checksum));
        }

        if (installedBy == string.Empty)
        {
            throw new ArgumentException("Cannot be empty", nameof(installedBy));
        }

        if (installedOn.Kind is not DateTimeKind.Utc)
        {
            throw new ArgumentException("Must be UTC", nameof(installedOn));
        }

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

    public string Description { get; init; }

    public string Type { get; }

    public string Script { get; }

    public string Checksum { get; init; }

    public string InstalledBy { get; }

    public DateTime InstalledOn { get; init; }

    public double ExecutionTime { get; }

    public bool Success { get; }

    public MigrationType MigrationType => Version switch
    {
        null => MigrationType.Repeatable,
        _ => MigrationType.Versioned
    };
}
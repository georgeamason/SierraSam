namespace SierraSam.Core.Enums;

public enum MigrationState
{
    None = -1,
    Any = 0,
    Pending,
    Applied,
    Missing,
}
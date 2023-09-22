namespace SierraSam.Core;

public interface IConfiguration
{
    string Url { get; set; }
    string User { get; }
    int ConnectionTimeout { get; set; }
    int ConnectionRetries { get; set; }
    string DefaultSchema { get; set; }
    string InitialiseSql { get; set; }
    string SchemaTable { get; set; }
    IEnumerable<string> Locations { get; set; }
    IEnumerable<string> MigrationSuffixes { get; set; }
    string MigrationSeparator { get; set; }
    string MigrationPrefix { get; set; }
    string InstalledBy { get; set; }
    IEnumerable<string> Schemas { get; set; }
    string RepeatableMigrationPrefix { get; set; }
    string UndoMigrationPrefix { get; set; }
    IEnumerable<string> IgnoredMigrations { get; set; }
    string InitialiseVersion { get; set; }
}
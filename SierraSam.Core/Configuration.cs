using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SierraSam.Core;

// TODO: Does this need to be IEquatable?
public class Configuration : IConfiguration, IEquatable<Configuration>
{
    public Configuration()
    {
        Url = string.Empty;
        User = string.Empty;
        ConnectionTimeout = 15;
        ConnectionRetries = 1;
        DefaultSchema = string.Empty;
        InitialiseSql = string.Empty;
        SchemaTable = "flyway_schema_history";
        Locations = new[] { $"filesystem:{Path.Combine("db", "migration")}" };
        MigrationSuffixes = new[] { ".sql" };
        MigrationSeparator = "__";
        MigrationPrefix = "V";
        InstalledBy = string.Empty;
        Schemas = Enumerable.Empty<string>();
        RepeatableMigrationPrefix = "R";
        UndoMigrationPrefix = "U";
        IgnoredMigrations = new[] { "*:pending" };
    }

    public Configuration(
        string? url = null,
        string? user = null,
        int? connectionTimeout = null,
        int? connectionRetries = null,
        string? defaultSchema = null,
        string? initialiseSql = null,
        string? schemaTable = null,
        IEnumerable<string>? locations = null,
        IEnumerable<string>? migrationSuffixes = null,
        string? migrationSeparator = null,
        string? migrationPrefix = null,
        string? installedBy = null,
        IEnumerable<string>? schemas = null,
        string? repeatableMigrationPrefix = null,
        string? undoMigrationPrefix = null,
        IEnumerable<string>? ignoredMigrations = null)
    {
        Url = url ?? string.Empty;
        User = user ?? string.Empty;
        ConnectionTimeout = connectionTimeout ?? 15;
        ConnectionRetries = connectionRetries ?? 1;
        DefaultSchema = defaultSchema ?? string.Empty;
        InitialiseSql = initialiseSql ?? string.Empty;
        SchemaTable = schemaTable ?? "flyway_schema_history";
        Locations = locations ?? new[] { $"filesystem:{Path.Combine("db", "migration")}" };
        MigrationSuffixes = migrationSuffixes ?? new[] { ".sql" };
        MigrationSeparator = migrationSeparator ?? "__";
        MigrationPrefix = migrationPrefix ?? "V";
        InstalledBy = installedBy ?? string.Empty;
        Schemas = schemas ?? Enumerable.Empty<string>();
        RepeatableMigrationPrefix = repeatableMigrationPrefix ?? "R";
        UndoMigrationPrefix = undoMigrationPrefix ?? "U";
        IgnoredMigrations = ignoredMigrations ?? new[] { "*:pending" };
    }

    [JsonPropertyName("url"), JsonInclude]
    public string Url { get; set; }

    [JsonPropertyName("user"), JsonInclude]
    public string User { get; }

    [JsonPropertyName("connectionTimeout"), JsonInclude]
    public int ConnectionTimeout { get; set; }

    [JsonPropertyName("connectionRetries"), JsonInclude]
    public int ConnectionRetries { get; set; }

    [JsonPropertyName("defaultSchema"), JsonInclude]
    public string DefaultSchema { get; set; }

    [JsonPropertyName("initialiseSql"), JsonInclude]
    public string InitialiseSql { get; set; }

    [JsonPropertyName("schemaTable"), JsonInclude]
    public string SchemaTable { get; set; }

    [JsonPropertyName("locations"), JsonInclude]
    [Description("List of locations to scan for migrations")]
    public IEnumerable<string> Locations { get; set; }

    [JsonPropertyName("migrationSuffixes"), JsonInclude]
    public IEnumerable<string> MigrationSuffixes { get; set; }

    [JsonPropertyName("migrationSeparator"), JsonInclude]
    [RegularExpression("[^A-Za-z0-9]")]
    [MinLength(2)]
    public string MigrationSeparator { get; set; }

    [JsonPropertyName("migrationPrefix"), JsonInclude]
    [RegularExpression("[A-Za-z]")]
    public string MigrationPrefix { get; set; }

    [JsonPropertyName("installedBy"), JsonInclude]
    public string InstalledBy { get; set; }

    [JsonPropertyName("schemas"), JsonInclude]
    public IEnumerable<string> Schemas { get; set; }

    [JsonPropertyName("repeatableMigrationPrefix"), JsonInclude]
    [RegularExpression("[A-Za-z]")]
    public string RepeatableMigrationPrefix { get; set; }

    [JsonPropertyName("undoMigrationPrefix"), JsonInclude]
    [RegularExpression("[A-Za-z]")]
    public string UndoMigrationPrefix { get; set; }

    [JsonPropertyName("ignoredMigrations"), JsonInclude]
    public IEnumerable<string> IgnoredMigrations  { get; set; }

    #region IEquatable
    public bool Equals(Configuration? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return Url == other.Url 
            && User == other.User
            && ConnectionTimeout == other.ConnectionTimeout
            && ConnectionRetries == other.ConnectionRetries
            && DefaultSchema == other.DefaultSchema
            && InitialiseSql == other.InitialiseSql
            && SchemaTable == other.SchemaTable
            && Locations.SequenceEqual(other.Locations)
            && MigrationSuffixes.SequenceEqual(other.MigrationSuffixes)
            && MigrationSeparator == other.MigrationSeparator
            && MigrationPrefix == other.MigrationPrefix
            && InstalledBy == other.InstalledBy
            && Schemas.SequenceEqual(other.Schemas)
            && RepeatableMigrationPrefix == other.RepeatableMigrationPrefix
            && UndoMigrationPrefix == other.UndoMigrationPrefix
            && IgnoredMigrations.SequenceEqual(other.IgnoredMigrations);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;

        return obj.GetType() == typeof(Configuration) && Equals((Configuration)obj);
    }

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Url);
        hashCode.Add(User);
        hashCode.Add(ConnectionTimeout);
        hashCode.Add(ConnectionRetries);
        hashCode.Add(DefaultSchema);
        hashCode.Add(InitialiseSql);
        hashCode.Add(SchemaTable);
        hashCode.Add(Locations);
        hashCode.Add(MigrationSuffixes);
        hashCode.Add(MigrationSeparator);
        hashCode.Add(MigrationPrefix);
        hashCode.Add(InstalledBy);
        hashCode.Add(Schemas);
        hashCode.Add(RepeatableMigrationPrefix);
        hashCode.Add(UndoMigrationPrefix);
        hashCode.Add(IgnoredMigrations);
        return hashCode.ToHashCode();
    }
    #endregion
}
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SierraSam.Core;

public class Configuration : IEquatable<Configuration>
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;

    [JsonPropertyName("connectionTimeout")]
    public int ConnectionTimeout { get; set; } = 15;

    [JsonPropertyName("connectionRetries")]
    public int ConnectionRetries { get; set; } = 1;

    [JsonPropertyName("defaultSchema")]
    public string DefaultSchema { get; set; } = "dbo";

    [JsonPropertyName("initialiseSql")]
    public string InitialiseSql { get; set; } = string.Empty;

    [JsonPropertyName("schemaTable")]
    public string SchemaTable { get; set; } = "flyway_schema_history";

    [JsonPropertyName("locations")]
    [Description("List of locations to scan for migrations")]
    public IEnumerable<string> Locations { get; set; } = new []
    {
        $"filesystem:{Path.Combine("db", "migration")}"
    };

    [JsonPropertyName("migrationSuffixes")]
    public IEnumerable<string> MigrationSuffixes { get; set; } = new[]
    {
        ".sql"
    };

    [JsonPropertyName("migrationSeparator")]
    [RegularExpression("[^A-Za-z0-9]")]
    [MinLength(2)]
    public string MigrationSeparator { get; set; } = "__";

    [JsonPropertyName("migrationPrefix")]
    [RegularExpression("[A-Za-z]")]
    public string MigrationPrefix { get; set; } = "V";

    [JsonPropertyName("installedBy")]
    public string InstalledBy { get; set; } = string.Empty;

    public bool Equals(Configuration? other)
    {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;

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
            && InstalledBy == other.InstalledBy;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != typeof(Configuration))
            return false;

        return Equals((Configuration)obj);
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
        return hashCode.ToHashCode();
    }
}
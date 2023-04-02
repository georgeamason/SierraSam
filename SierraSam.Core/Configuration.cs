using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SierraSam.Core;

public record Configuration
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public string? User { get; set; }

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
    public IEnumerable<string> Locations { get; set; } = GetLocations();

    [JsonPropertyName("migrationSuffixes")]
    public IEnumerable<string> MigrationSuffixes { get; set; } = new[] { ".sql" };

    [JsonPropertyName("migrationSeparator")]
    public string MigrationSeparator { get; set; } = "__";

    [JsonPropertyName("migrationPrefix")]
    public string MigrationPrefix { get; set; } = "V";

    [JsonPropertyName("installedBy")]
    public string InstalledBy { get; set; } = string.Empty;

    private static IEnumerable<string> GetLocations()
    {
        return new []
        {
            $"filesystem:{Path.Combine("db", "migration")}"
        };
    }
}
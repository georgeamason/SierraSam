using System.Text.Json.Serialization;

namespace SierraSam.Core;

public record Configuration
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("connectionTimeout")]
    public int ConnectionTimeout { get; set; } = 15;

    [JsonPropertyName("connectionRetries")]
    public int ConnectionRetries { get; set; } = 1;

    [JsonPropertyName("defaultSchema")]
    public string DefaultSchema { get; set; } = string.Empty;

    [JsonPropertyName("initialiseSql")]
    public string InitialiseSql { get; set; } = string.Empty;

    [JsonPropertyName("schemaTable")]
    public string SchemaTable { get; set; } = string.Empty;
}
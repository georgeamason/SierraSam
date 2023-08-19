using System.IO.Abstractions;
using System.Text.Json;
using SierraSam.Core.Extensions;

namespace SierraSam.Core.ConfigurationReaders;

internal sealed class JsonConfigurationReader : IConfigurationReader
{
    private readonly IFileSystem _fileSystem;

    private readonly IEnumerable<string> _defaultConfigPaths;

    public JsonConfigurationReader(IFileSystem fileSystem,
                                    IEnumerable<string> defaultConfigPaths)
    {
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));

        _defaultConfigPaths = defaultConfigPaths
            ?? throw new ArgumentNullException(nameof(defaultConfigPaths));
    }

    public IConfiguration Read()
    {
        // Check flyway configuration file for info on how to complete migration
        // default location <base_location>\conf\flyway.config
        // if nothing there, return with info
        foreach (var configPath in _defaultConfigPaths)
        {
            // This whole config stuff will be used by other capabilities
            // and will therefore have to be injected using DI
            if (!_fileSystem.File.Exists(configPath)) continue;

            var jsonConfig = _fileSystem.File.ReadAllText(configPath);

            if (string.IsNullOrEmpty(jsonConfig)) continue;

            if (!jsonConfig.IsJson(out var exception)) throw exception!;

            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas         = true
            };

            return JsonSerializer.Deserialize<Configuration>
                (jsonConfig, jsonSerializerOptions)!;
        }

        return new Configuration();
    }
}
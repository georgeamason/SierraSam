﻿using System.IO.Abstractions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SierraSam.Core.Extensions;

namespace SierraSam.Core.ConfigurationBuilders;

internal sealed class JsonConfigurationBuilder : IConfigurationBuilder
{
    private readonly ILogger _logger;

    private readonly IFileSystem _fileSystem;

    private readonly IEnumerable<string> _defaultConfigPaths;

    public JsonConfigurationBuilder
        (ILogger logger,
         IFileSystem fileSystem,
         IEnumerable<string> defaultConfigPaths)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));

        _defaultConfigPaths = defaultConfigPaths
            ?? throw new ArgumentNullException(nameof(defaultConfigPaths));
    }

    public Configuration Build()
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
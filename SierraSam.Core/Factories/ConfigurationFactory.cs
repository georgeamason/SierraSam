using System.Text.Json;
using Microsoft.Extensions.Logging;
using SierraSam.Core.Extensions;
using SierraSam.Core.Providers;

namespace SierraSam.Core.Factories;

public sealed class ConfigurationFactory
{
    public ConfigurationFactory
        (ILogger<ConfigurationFactory>logger,
         IFileSystemProvider fileSystemProvider,
         IEnumerable<string> defaultConfigPaths)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _fileSystemProvider = fileSystemProvider
            ?? throw new ArgumentNullException(nameof(fileSystemProvider));

        _defaultConfigPaths = defaultConfigPaths
            ?? throw new ArgumentNullException(nameof(defaultConfigPaths));
    }

    public Configuration Create(IEnumerable<string> args)
    {
        // Check flyway configuration file for info on how to complete migration
        // default location <base_location>\conf\flyway.config
        // if nothing there, return with info
        foreach (var configPath in _defaultConfigPaths)
        {
            // This whole config stuff will be used by other capabilities
            // and will therefore have to be injected using DI
            if (!_fileSystemProvider.Exists(configPath)) continue;

            var jsonConfig = _fileSystemProvider.ReadAllText(configPath);

            if (string.IsNullOrEmpty(jsonConfig)) continue;

            if (!jsonConfig.IsJson(out var exception)) throw exception!;

            var jsonSerializerOptions = new JsonSerializerOptions 
                { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

            Configuration = JsonSerializer.Deserialize<Configuration>
                (jsonConfig, jsonSerializerOptions)!;
        }

        // here we can optionally override any of the configuration picked up from the configFile
        foreach (var arg in args)
        {
            var argSplit = arg.Split('=', 2).AsReadOnly();

            if (argSplit.Count is not 2)
                continue;

            var kvp = new KeyValuePair<string, string>
                (argSplit[0], argSplit[1]);

            switch (kvp.Key)
            {
                case "--url":
                    // https://flywaydb.org/documentation/configuration/parameters/url
                    _logger.LogInformation($"Configuration override {kvp.Key} to {kvp.Value}");
                    Configuration.Url = kvp.Value;
                    break;
                case "--connectionTimeout":
                    if (!int.TryParse(kvp.Value, out var connectionTimeout))
                    {
                        _logger.LogInformation($"connectionTimeout: {kvp.Value} is not an integer");
                        break;
                    }

                    if (connectionTimeout < 0)
                        throw new ArgumentException("Connection Timeout cannot be less that 0");

                    _logger.LogInformation($"Configuration override {kvp.Key} to {kvp.Value}");
                    Configuration.ConnectionTimeout = connectionTimeout;

                    break;
                case "--connectionRetries":
                    // https://flywaydb.org/documentation/configuration/parameters/connectRetries
                    if (int.TryParse(kvp.Value, out var connectionRetries))
                    {
                        _logger.LogInformation($"Configuration override {kvp.Key} to {kvp.Value}");
                        Configuration.ConnectionRetries = connectionRetries;
                    }
                    break;
                case "--defaultSchema":
                    // https://flywaydb.org/documentation/configuration/parameters/defaultSchema
                    _logger.LogInformation($"Configuration override {kvp.Key} to {kvp.Value}");
                    Configuration.DefaultSchema = kvp.Value;
                    break;
                case "--initSql":
                    // https://flywaydb.org/documentation/configuration/parameters/initSql
                    _logger.LogInformation($"Configuration override {kvp.Key} to {kvp.Value}");
                    Configuration.InitialiseSql = kvp.Value;
                    break;
                case "--table":
                    // https://flywaydb.org/documentation/configuration/parameters/table
                    _logger.LogInformation($"Configuration override {kvp.Key} to {kvp.Value}");
                    Configuration.SchemaTable = kvp.Value;
                    break;
                default:
                    _logger.LogInformation($"{arg} was not recognised.");
                    break;
            }
        }

        // If no default configuration exists, new a default up
        return Configuration;
    }

    private Configuration Configuration { get; set; } = new();

    private readonly ILogger<ConfigurationFactory> _logger;

    private readonly IFileSystemProvider _fileSystemProvider;

    private readonly IEnumerable<string> _defaultConfigPaths;
}
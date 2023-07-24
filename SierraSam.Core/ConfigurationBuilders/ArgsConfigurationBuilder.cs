using Microsoft.Extensions.Logging;

namespace SierraSam.Core.ConfigurationBuilders;

internal sealed class ArgsConfigurationBuilder : IConfigurationBuilder
{
    private readonly ILogger<ArgsConfigurationBuilder> _logger;

    private readonly IEnumerable<string> _args;

    private readonly IConfigurationBuilder _configurationReader;


    public ArgsConfigurationBuilder(ILoggerFactory loggerFactory,
                                    string[] args,
                                    IConfigurationBuilder configurationReader)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));

        _logger = loggerFactory.CreateLogger<ArgsConfigurationBuilder>();

        _args = args
                ?? throw new ArgumentNullException(nameof(args));

        _configurationReader = configurationReader
            ?? throw new ArgumentNullException(nameof(configurationReader));
    }

    public Configuration Build()
    {
        var configuration = _configurationReader.Build();

        // here we can optionally override any of the configuration picked up from the configFile
        foreach (var arg in _args)
        {
            var argSplit = arg.Split('=', 2);

            if (argSplit.Length is not 2)
                continue;

            var kvp = new KeyValuePair<string, string>
                (argSplit[0], argSplit[1]);

            _logger.LogInformation("Configuration override {key} to {value}...", kvp.Key, kvp.Value);
            switch (kvp.Key)
            {
                case "--url":
                    // https://flywaydb.org/documentation/configuration/parameters/url
                    configuration.SetUrl(kvp.Value);
                    break;
                case "--connectionTimeout":
                    if (!int.TryParse(kvp.Value, out var connectionTimeout))
                    {
                        _logger.LogInformation("connectionTimeout: {value} is not an integer", kvp.Value);
                        break;
                    }

                    if (connectionTimeout < 0)
                        throw new ArgumentException("Connection Timeout cannot be less that 0");

                    configuration.SetConnectionTimeout(connectionTimeout);

                    break;
                case "--connectionRetries":
                    // https://flywaydb.org/documentation/configuration/parameters/connectRetries
                    if (int.TryParse(kvp.Value, out var connectionRetries))
                    {
                        configuration.SetConnectionRetries(connectionRetries);
                    }
                    break;
                case "--defaultSchema":
                    // https://flywaydb.org/documentation/configuration/parameters/defaultSchema
                    configuration.SetDefaultSchema(kvp.Value);
                    break;
                case "--initSql":
                    // https://flywaydb.org/documentation/configuration/parameters/initSql
                    configuration.SetInitialiseSql(kvp.Value);
                    break;
                case "--table":
                    // https://flywaydb.org/documentation/configuration/parameters/table
                    configuration.SetSchemaTable(kvp.Value);
                    break;
                case "--locations":
                    configuration.SetLocations(kvp.Value.Split(','));
                    break;
                case "--migrationSuffixes":
                    configuration.SetMigrationSuffixes(kvp.Value.Split(','));
                    break;
                case "--migrationSeparator":
                    configuration.SetMigrationSeparator(kvp.Value);
                    break;
                case "--migrationPrefix":
                    configuration.SetMigrationPrefix(kvp.Value);
                    break;
                case "--installedBy":
                    configuration.SetInstalledBy(kvp.Value);
                    break;
                default:
                    Console.WriteLine($"Argument '{kvp.Key}' was not recognised.");
                    throw new Exception($"Invalid argument '{kvp.Key}'");
            }
        }

        return configuration;
    }
}
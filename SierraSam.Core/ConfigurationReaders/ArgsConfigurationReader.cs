using Microsoft.Extensions.Logging;

namespace SierraSam.Core.ConfigurationReaders;

internal sealed class ArgsConfigurationReader : IConfigurationReader
{
    private readonly ILogger<ArgsConfigurationReader> _logger;

    private readonly IEnumerable<string> _args;

    private readonly IConfigurationReader _configurationReader;


    public ArgsConfigurationReader(ILoggerFactory loggerFactory,
                                    string[] args,
                                    IConfigurationReader configurationReader)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));

        _logger = loggerFactory.CreateLogger<ArgsConfigurationReader>();

        _args = args
                ?? throw new ArgumentNullException(nameof(args));

        _configurationReader = configurationReader
            ?? throw new ArgumentNullException(nameof(configurationReader));
    }

    // TODO: Make sure we're setting all the properties here
    public Configuration Read()
    {
        var configuration = _configurationReader.Read();

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
                case "--repeatableMigrationPrefix":
                    configuration.SetRepeatableMigrationPrefix(kvp.Value);
                    break;
                case "--undoMigrationPrefix":
                    configuration.SetUndoMigrationPrefix(kvp.Value);
                    break;
                case "--ignoredMigrations":
                    configuration.SetIgnoredMigrations(kvp.Value.Split(','));
                    break;
                default:
                    throw new Exception($"Invalid argument '{kvp.Key}'");
            }
        }

        return configuration;
    }
}
using System.Data.Odbc;

namespace SierraSam.Core.ConfigurationReaders;

internal sealed class DefaultSchemaConfigurationReader : IConfigurationReader
{
    private readonly IConfigurationReader _configurationReader;

    public DefaultSchemaConfigurationReader(IConfigurationReader configurationReader)
    {
        _configurationReader = configurationReader ?? throw new ArgumentNullException(nameof(configurationReader));
    }

    public IConfiguration Read()
    {
        var configuration = _configurationReader.Read();

        if (!string.IsNullOrEmpty(configuration.DefaultSchema)) return configuration;

        if (configuration.Schemas.Any()) configuration.DefaultSchema = configuration.Schemas.First();

        // Getting the default schema inside each IDatabase implementation
        // with property 'DefaultSchema'.

        return configuration;
    }
}
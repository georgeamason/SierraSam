using System.Data.Odbc;

namespace SierraSam.Core.ConfigurationReaders;

internal sealed class DefaultSchemaConfigurationReader : IConfigurationReader
{
    private readonly IConfigurationReader _configurationReader;

    public DefaultSchemaConfigurationReader(IConfigurationReader configurationReader)
    {
        _configurationReader = configurationReader
            ?? throw new ArgumentNullException(nameof(configurationReader));
    }

    public Configuration Read()
    {
        var configuration = _configurationReader.Read();

        if (!string.IsNullOrEmpty(configuration.DefaultSchema)) return configuration;

        if (configuration.Schemas.Any()) configuration.SetDefaultSchema(configuration.Schemas.First());

        // TODO: Check what the default database schema is
        try
        {
            using var connection = new OdbcConnection(configuration.Url);

            connection.Open();

            var executor = new OdbcExecutor(connection);

            var defaultSchema = executor.ExecuteReader<string>
                ("SELECT SCHEMA_NAME()",
                 dataReader => dataReader.GetString(0));

            configuration.SetDefaultSchema(defaultSchema.Single());
        }
        catch (Exception exception)
        {
            throw new Exception
                ("Unable to determine schema for the schema history table. " +
                 "Set a default schema for the connection or specify one " +
                 "using the 'defaultSchema' property", exception);
        }

        return configuration;
    }
}
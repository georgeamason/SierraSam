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

    public IConfiguration Read()
    {
        var configuration = _configurationReader.Read();

        if (!string.IsNullOrEmpty(configuration.DefaultSchema)) return configuration;

        if (configuration.Schemas.Any()) configuration.DefaultSchema = configuration.Schemas.First();

        try
        {
            // TODO: No sure if this is the best way to do this?
            using var connection = new OdbcConnection(configuration.Url);

            connection.Open();

            var executor = new OdbcExecutor(connection);

            // TODO: Will this work for all databases?
            var defaultSchema = executor.ExecuteReader<string>
                ("SELECT SCHEMA_NAME()",
                 dataReader => dataReader.GetString(0));

            configuration.DefaultSchema = defaultSchema.Single();
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
using System.Data.Odbc;

namespace SierraSam.Core.ConfigurationBuilders;

internal sealed class DefaultSchemaConfigurationBuilder : IConfigurationBuilder
{
    private readonly IConfigurationBuilder _configurationBuilder;

    public DefaultSchemaConfigurationBuilder(IConfigurationBuilder configurationBuilder)
    {
        _configurationBuilder = configurationBuilder
            ?? throw new ArgumentNullException(nameof(configurationBuilder));
    }

    public Configuration Build()
    {
        var configuration = _configurationBuilder.Build();

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
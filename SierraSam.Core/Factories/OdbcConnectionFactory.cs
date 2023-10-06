using System.Data;
using System.Data.Odbc;
using Microsoft.Extensions.Logging;

namespace SierraSam.Core.Factories;

public static class OdbcConnectionFactory
{
    public static IDbConnection Create(ILogger logger, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        try
        {
            // ReSharper disable once ObjectCreationAsStatement
            new OdbcConnectionStringBuilder(configuration.Url);
        }
        catch (Exception exception)
        {
            logger.LogError(exception.Message, "Url not formatted correctly");
            throw;
        }

        var connection = new OdbcConnection(configuration.Url)
        {
            ConnectionTimeout = configuration.ConnectionTimeout
        };

        connection.Open();

        connection.InfoMessage += (_, eventArgs) =>
        {
            foreach (OdbcError exception in eventArgs.Errors)
            {
                logger.LogTrace(exception.Message);
            }
        };

        connection.StateChange += (_, args) =>
        {
            logger.LogTrace
                ("Connection state changed from {originalState} to {currentState}",
                 args.OriginalState,
                 args.CurrentState);
        };

        connection.Disposed += (_, _) => logger.LogTrace("Disposed of connection");

        return connection;
    }
}
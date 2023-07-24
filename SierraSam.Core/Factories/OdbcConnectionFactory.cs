using System.Data.Odbc;
using Microsoft.Extensions.Logging;

namespace SierraSam.Core.Factories;

public static class OdbcConnectionFactory
{
    public static OdbcConnection Create
        (ILogger logger, Configuration configuration)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

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
                ($"Connection state changed from {args.OriginalState} to {args.CurrentState}");
        };

        connection.Disposed += (_, _) => logger.LogTrace($"Disposed of connection");

        return connection;
    }
}
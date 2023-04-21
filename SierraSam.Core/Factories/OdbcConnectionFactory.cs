using System.Data.Odbc;
using Microsoft.Extensions.Logging;

namespace SierraSam.Core.Factories;

public sealed class OdbcConnectionFactory
{
    public OdbcConnectionFactory
        (ILogger<OdbcConnectionFactory> logger,
         Configuration configuration)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _configuration = configuration 
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public OdbcConnection Create()
    {
        try
        {
            var connectionStringBuilder =
                new OdbcConnectionStringBuilder(_configuration.Url);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception.Message, "Url not formatted correctly");
            throw;
        }

        var connection = new OdbcConnection(_configuration.Url)
        {
            ConnectionTimeout = _configuration.ConnectionTimeout
        };

        connection.InfoMessage += (_, eventArgs) =>
        {
            foreach (OdbcError exception in eventArgs.Errors)
            {
                _logger.LogTrace(exception.Message);
            }
        };

        connection.StateChange += (_, args) =>
        {
            _logger.LogTrace
                ($"Connection state changed from {args.OriginalState} to {args.CurrentState}");
        };

        connection.Disposed += (_, _) => _logger.LogTrace($"Disposed of connection");

        return connection;
    }

    private readonly ILogger<OdbcConnectionFactory> _logger;

    private readonly Configuration _configuration;
}
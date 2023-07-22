using System.Data.Odbc;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using SierraSam.Core.Extensions;

namespace SierraSam.Core.ConfigurationBuilders;

internal sealed class InstalledByConfigurationBuilder : IConfigurationBuilder
{
    private readonly ILogger _logger;

    private readonly IConfigurationBuilder _configurationBuilder;

    public InstalledByConfigurationBuilder
        (ILogger logger, IConfigurationBuilder configurationBuilder)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _configurationBuilder = configurationBuilder
            ?? throw new ArgumentNullException(nameof(configurationBuilder));
    }
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public Configuration Build()
    {
        var configuration = _configurationBuilder.Build();

        if (!string.IsNullOrEmpty(configuration.User)) return configuration;

        // TODO: Obtain the user from the connection string
        try
        {
            var connStrBuilder = new OdbcConnectionStringBuilder
                (configuration.Url);

            foreach (string key in connStrBuilder.Keys!)
            {
                switch (key.ToLower())
                {
                    case "trusted_connection":
                        configuration.SetInstalledBy(WindowsIdentity.GetCurrent().Name);
                        break;
                    case "user id":
                    case "user":
                    case "uid":
                        configuration.SetInstalledBy(connStrBuilder.GetValue(key));
                        break;
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception.Message);
            throw;
        }

        return configuration;
    }
}
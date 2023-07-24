using System.Data.Odbc;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using SierraSam.Core.Extensions;

namespace SierraSam.Core.ConfigurationBuilders;

internal sealed class InstalledByConfigurationBuilder : IConfigurationBuilder
{
    private readonly IConfigurationBuilder _configurationBuilder;

    public InstalledByConfigurationBuilder(IConfigurationBuilder configurationBuilder)
    {
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
            throw new Exception("The url is not a valid connection string", exception);
        }

        return configuration;
    }
}
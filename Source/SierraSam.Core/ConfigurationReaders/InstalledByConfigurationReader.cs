using System.Data.Odbc;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using SierraSam.Core.Extensions;

namespace SierraSam.Core.ConfigurationReaders;

internal sealed class InstalledByConfigurationReader : IConfigurationReader
{
    private readonly IConfigurationReader _configurationReader;

    public InstalledByConfigurationReader(IConfigurationReader configurationReader)
    {
        _configurationReader = configurationReader
            ?? throw new ArgumentNullException(nameof(configurationReader));
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public IConfiguration Read()
    {
        var configuration = _configurationReader.Read();

        if (!string.IsNullOrEmpty(configuration.User)) return configuration;

        try
        {
            var connStrBuilder = new OdbcConnectionStringBuilder
                (configuration.Url);

            foreach (string key in connStrBuilder.Keys!)
            {
                // TODO: Are these all the places where we can get the user from?
                switch (key.ToLower())
                {
                    case "trusted_connection":
                        configuration.InstalledBy = WindowsIdentity.GetCurrent().Name;
                        break;
                    case "user id":
                    case "user":
                    case "uid":
                        configuration.InstalledBy = connStrBuilder.GetValue(key);
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
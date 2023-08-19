using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using SierraSam.Core.ConfigurationReaders;

namespace SierraSam.Core.Factories;

public static class ConfigurationFactory
{

    public static IConfiguration Create(ILoggerFactory loggerFactory,
                                       IFileSystem fileSystem,
                                       IEnumerable<string> defaultConfigPaths,
                                       string[] args)
    {
        // Ordering is important here
        var reader = new InstalledByConfigurationReader
            (new DefaultSchemaConfigurationReader
                (new ArgsConfigurationReader
                    (loggerFactory, args, new JsonConfigurationReader
                        (fileSystem, defaultConfigPaths)
                    )
                )
            );

        return reader.Read();
    }
}
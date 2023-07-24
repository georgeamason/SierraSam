using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using SierraSam.Core.ConfigurationBuilders;

namespace SierraSam.Core.Factories;

public static class ConfigurationFactory
{

    public static Configuration Create(ILoggerFactory loggerFactory,
                                       IFileSystem fileSystem,
                                       IEnumerable<string> defaultConfigPaths,
                                       string[] args)
    {
        // Ordering is important here
        var reader = new InstalledByConfigurationBuilder
            (new DefaultSchemaConfigurationBuilder
                (new ArgsConfigurationBuilder
                    (loggerFactory, args, new JsonConfigurationBuilder
                        (fileSystem, defaultConfigPaths)
                    )
                )
            );

        return reader.Build();
    }
}
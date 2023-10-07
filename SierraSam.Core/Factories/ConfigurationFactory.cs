using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using SierraSam.Core.ConfigurationReaders;

namespace SierraSam.Core.Factories;

public static class ConfigurationFactory
{

    public static IConfiguration Create(ILoggerFactory loggerFactory,
                                       IFileSystem fileSystem,
                                       string[] args)
    {
        // Ordering is important here
        var reader = new InstalledByConfigurationReader
            (new DefaultSchemaConfigurationReader
                (new ArgsConfigurationReader
                    (loggerFactory, args, new JsonConfigurationReader
                        (fileSystem, ConfigPaths())
                    )
                )
            );

        return reader.Read();
    }

    private static IEnumerable<string> ConfigPaths(string fileName = "flyway.json")
    {
        var userFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return new []
        {
            Path.Combine(userFolderPath, fileName),
            Path.Combine(Environment.CurrentDirectory, fileName)
        };
    }
}
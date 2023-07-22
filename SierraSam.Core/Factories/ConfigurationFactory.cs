using System.Data.Odbc;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using SierraSam.Core.ConfigurationBuilders;

namespace SierraSam.Core.Factories;

public sealed class ConfigurationFactory
{
    private readonly ILogger<ConfigurationFactory> _logger;

    private readonly IFileSystem _fileSystem;

    private readonly IEnumerable<string> _defaultConfigPaths;

    public ConfigurationFactory
        (ILogger<ConfigurationFactory> logger,
         IFileSystem fileSystem,
         IEnumerable<string> defaultConfigPaths)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _fileSystem = fileSystem
                      ?? throw new ArgumentNullException(nameof(fileSystem));

        _defaultConfigPaths = defaultConfigPaths
            ?? throw new ArgumentNullException(nameof(defaultConfigPaths));
    }

    public Configuration Create(string[] args)
    {
        // Ordering is important here
        var reader = new InstalledByConfigurationBuilder
            (_logger, new DefaultSchemaConfigurationBuilder
                (_logger, new ArgsConfigurationBuilder
                    (_logger, args, new JsonConfigurationBuilder
                        (_logger, _fileSystem, _defaultConfigPaths)
                    )
                )
            );

        return reader.Build();
    }
}
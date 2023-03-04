
using Microsoft.Extensions.Logging;
using SierraSam.Capabilities;
using Version = SierraSam.Capabilities.Version;

namespace SierraSam;

public sealed class App
{
    public App
        (ILogger<App> logger,
         ICapabilityResolver capabilityResolver)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _capabilityResolver = capabilityResolver
            ?? throw new ArgumentNullException(nameof(capabilityResolver));
    }

    public void Start(string[] args)
    {
        _logger.LogInformation($"{nameof(App)} running");

        if (!args.Any())
        {
            _capabilityResolver.Resolve(typeof(Help)).Run(args);

            return;
        };

        switch (args[0])
        {
            case "-v" or "--version" or "version":
                _capabilityResolver.Resolve(typeof(Version)).Run(Array.Empty<string>());
                break;
            case "--auth" or "auth":
                _capabilityResolver.Resolve(typeof(Auth)).Run(args[1..]);
                break;
            case "--migrate" or "migrate":
                _capabilityResolver.Resolve(typeof(Migrate)).Run(args[1..]);
                break;
            case "--help" or "help":
                _capabilityResolver.Resolve(typeof(Help)).Run(args[1..]);
                break;
        }
    }

    private readonly ILogger _logger;

    private readonly ICapabilityResolver _capabilityResolver;
}
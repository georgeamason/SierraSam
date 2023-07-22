
using Microsoft.Extensions.Logging;
using SierraSam.Capabilities;

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
            case "--auth" or "auth":
                _capabilityResolver.Resolve(typeof(Auth)).Run(args[1..]);
                break;
            case "--baseline" or "baseline":
                _capabilityResolver.Resolve(typeof(Baseline)).Run(args[1..]);
                break;
            case "--clean" or "clean":
                _capabilityResolver.Resolve(typeof(Clean)).Run(args[1..]);
                break;
            case "--help" or "help":
                _capabilityResolver.Resolve(typeof(Help)).Run(args[1..]);
                break;
            case "--migrate" or "migrate":
                _capabilityResolver.Resolve(typeof(Migrate)).Run(args[1..]);
                break;
            case "-v" or "--version" or "version":
                _capabilityResolver.Resolve(typeof(Capabilities.Version)).Run(Array.Empty<string>());
                break;
        }
    }

    private readonly ILogger _logger;

    private readonly ICapabilityResolver _capabilityResolver;
}
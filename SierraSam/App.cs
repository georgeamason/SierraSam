
using System.Reflection;
using Microsoft.Extensions.Logging;
using SierraSam.Capabilities;

namespace SierraSam;

public sealed class App
{
    private readonly ILogger _logger;

    private readonly ICapabilityResolver _capabilityResolver;

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
        _logger.LogTrace($"{nameof(App)} running");

        if (!args.Any())
        {
            _capabilityResolver.Resolve(typeof(Help)).Run(args);

            return;
        };

        var version = Assembly.GetExecutingAssembly().GetName().Version
            ?? throw new ApplicationException("No assembly version specified");

        Console.Write(Environment.NewLine);
        Console.WriteLine($"SierraSam {version.Major}.{version.Minor}.{version.Build}.{version.Revision} by George Mason");
        Console.Write(Environment.NewLine);

        switch (args[0])
        {
            case "auth":
                _capabilityResolver.Resolve(typeof(Auth)).Run(args[1..]);
                break;
            case "clean":
                _capabilityResolver.Resolve(typeof(Clean)).Run(args[1..]);
                break;
            case "help":
                _capabilityResolver.Resolve(typeof(Help)).Run(args[1..]);
                break;
            case "init":
                _capabilityResolver.Resolve(typeof(Initialise)).Run(args[1..]);
                break;
            case "info":
                _capabilityResolver.Resolve(typeof(Information)).Run(args[1..]);
                break;
            case "migrate":
                _capabilityResolver.Resolve(typeof(Migrate)).Run(args[1..]);
                break;
            case "rollup":
                _capabilityResolver.Resolve(typeof(Rollup)).Run(args[1..]);
                break;
            case "validate":
                _capabilityResolver.Resolve(typeof(Validate)).Run(args[1..]);
                break;
            case "-v" or "--version" or "version":
                _capabilityResolver.Resolve(typeof(Capabilities.Version)).Run(Array.Empty<string>());
                break;
        }
    }
}
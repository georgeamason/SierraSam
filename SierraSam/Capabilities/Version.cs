using Microsoft.Extensions.Logging;

namespace SierraSam.Capabilities;

public sealed class Version : ICapability
{
    public Version(ILogger<Version> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Version)} running");

        Console.WriteLine($"Version: {GetType().Assembly.GetName().Version!}");
    }

    private readonly ILogger _logger;
}
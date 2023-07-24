using Microsoft.Extensions.Logging;

namespace SierraSam.Capabilities;

internal sealed class Clean : ICapability
{
    public Clean(ILogger<Clean> logger)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Clean)} is running");
    }

    private readonly ILogger<Clean> _logger;
}
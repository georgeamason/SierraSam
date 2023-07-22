using Microsoft.Extensions.Logging;

namespace SierraSam.Capabilities;

public sealed class Auth : ICapability
{
    public Auth(ILogger<Auth> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Run(string[] args)
    {
        _logger.LogInformation($"{nameof(Auth)} is running");
    }

    private readonly ILogger<Auth> _logger;
}
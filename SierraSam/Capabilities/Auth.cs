using Microsoft.Extensions.Logging;

namespace SierraSam.Capabilities;

public sealed class Auth : ICapability
{
    public Auth(ILogger<Auth> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Auth)} is running");

        return Task.CompletedTask;
    }

    private readonly ILogger<Auth> _logger;
}
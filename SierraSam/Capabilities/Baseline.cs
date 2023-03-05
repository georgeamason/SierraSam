using Microsoft.Extensions.Logging;

namespace SierraSam.Capabilities;

internal sealed class Baseline : ICapability
{
    public Baseline(ILogger<Baseline> logger)
    {
        _logger = logger 
            ?? throw new ArgumentNullException(nameof(logger));
    }
    public void Run(string[] args)
    {
        throw new NotImplementedException();
    }

    private readonly ILogger<Baseline> _logger;
}
using Microsoft.Extensions.Logging;

namespace SierraSam.Capabilities;

public sealed class CapabilityResolver : ICapabilityResolver
{
    public CapabilityResolver
        (ILogger<CapabilityResolver> logger,
         IEnumerable<ICapability> capabilities)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _capabilities = capabilities
            ?? throw new ArgumentNullException(nameof(capabilities));
    }

    public ICapability Resolve(Type T)
    {
        _logger.LogInformation($"Resolving capability {T}");

        var capability = _capabilities.FirstOrDefault(c => c.GetType() == T);

        if (capability is null)
            throw new NullReferenceException($"{T.Name} is not a listed capability");

        if (IsProtected(capability))
        {
            // Check for a permit..
        }

        return capability;
    }

    private static bool IsProtected(ICapability T)
    {
        switch (T)
        {
            case Help:
            case Version:
            case Auth:
                return false;
            default:
                return true;
        }
    }

    private readonly ILogger _logger;

    private readonly IEnumerable<ICapability> _capabilities;
}
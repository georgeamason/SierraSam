namespace SierraSam.Capabilities;

public sealed class CapabilityFactory : ICapabilityFactory
{
    private readonly IEnumerable<ICapability> _capabilities;

    public CapabilityFactory(IEnumerable<ICapability> capabilities)
    {
        _capabilities = capabilities;
    }

    public ICapability Resolve(Type T)
    {
        var capability = _capabilities.FirstOrDefault(c => c.GetType() == T);
        
        if (capability is null) 
            throw new InvalidOperationException($"{T} is not a listed capability");

        return capability;
    }
}
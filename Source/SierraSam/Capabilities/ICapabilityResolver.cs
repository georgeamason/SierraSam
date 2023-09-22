namespace SierraSam.Capabilities;

public interface ICapabilityResolver
{
    ICapability Resolve(Type type);
}
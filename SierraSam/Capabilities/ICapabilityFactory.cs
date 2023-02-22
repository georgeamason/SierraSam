namespace SierraSam.Capabilities;

public interface ICapabilityFactory
{
    ICapability Resolve(Type type);
}
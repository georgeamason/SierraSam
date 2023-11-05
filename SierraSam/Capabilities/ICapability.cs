namespace SierraSam.Capabilities;

public interface ICapability
{
    Task Run(string[] args);
}
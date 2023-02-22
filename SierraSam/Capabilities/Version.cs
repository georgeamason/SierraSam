using Microsoft.Extensions.Logging;

namespace SierraSam.Capabilities;

public sealed class Version : ICapability
{
    public Version(ILogger logger)
    {
        m_Logger = logger;
    }
    
    public void Run(string[] args)
    {
        m_Logger.LogInformation($"{nameof(Version)} running.");

        Console.WriteLine($"Version: {GetType().Assembly.GetName().Version!}");
    }

    private readonly ILogger m_Logger;
}

using Microsoft.Extensions.Logging;
using SierraSam.Capabilities;
using Version = SierraSam.Capabilities.Version;

namespace SierraSam;

public sealed class App : ICapability
{
    public App(ILogger logger, ICapabilityFactory capabilityFactory)
    {
        m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        m_CapabilityFactory = capabilityFactory ?? throw new ArgumentNullException(nameof(capabilityFactory));
    }

    public void Run(string[] args)
    {
        m_Logger.LogInformation("App running.");

        if (!args.Any())
        {
            m_CapabilityFactory.Resolve(typeof(Help)).Run(args);

            return;
        };

        switch (args[0])
        {
            case "-v" or "--version" or "version":
                m_CapabilityFactory.Resolve(typeof(Version)).Run(args);
                break;
            case "--auth" or "auth":
                break;
            case "--help" or "help":
                m_CapabilityFactory.Resolve(typeof(Help)).Run(args[1..]);
                break;
        }
    }

    private readonly ILogger m_Logger;

    private readonly ICapabilityFactory m_CapabilityFactory;
}
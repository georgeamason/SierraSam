using Microsoft.Extensions.Logging;

namespace SierraSam.Capabilities;

public sealed class Help : ICapability
{
    public Help(ILogger logger)
    {
        m_Logger = logger;
    }

    public void Run(string[] args)
    {
        m_Logger.LogInformation($"{nameof(Help)} running.");
        
        if (!args.Any())
        {
            Console.WriteLine("usage: ss [-v | --version] [--help] [--auth]");

            return;
        }

        switch (args[0])
        {
            case "auth":
                Console.WriteLine("This gives me some extra help");
                break;
        }
    }

    private readonly ILogger m_Logger;
}
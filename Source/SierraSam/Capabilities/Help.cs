using Microsoft.Extensions.Logging;

namespace SierraSam.Capabilities;

public sealed class Help : ICapability
{
    public Help(ILogger<Help> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Help)} running");
        
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

    private readonly ILogger _logger;
}
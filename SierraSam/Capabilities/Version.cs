using System.Reflection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace SierraSam.Capabilities;

public sealed class Version : ICapability
{
    private readonly ILogger _logger;
    private readonly IAnsiConsole _console;

    public Version(ILogger<Version> logger, IAnsiConsole console)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    public Task Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Version)} running");

        _console.Write(new FigletText("SierraSam"));

        var version = Assembly.GetExecutingAssembly().GetName().Version
                      ?? throw new ApplicationException("No assembly version specified");

        _console.WriteLine($"Version: {version.Major}.{version.Minor}.{version.Build}.{version.Revision}");

        return Task.CompletedTask;
    }
}
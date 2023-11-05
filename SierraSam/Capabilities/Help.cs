﻿using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace SierraSam.Capabilities;

public sealed class Help : ICapability
{
    private readonly ILogger _logger;
    private readonly IAnsiConsole _console;

    public Help(ILogger<Help> logger, IAnsiConsole console)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    public Task Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Help)} running");

        if (!args.Any())
        {
            _console.WriteLine("usage: ss [-v | --version] [--help] [--auth]");

            return Task.CompletedTask;
        }

        switch (args[0])
        {
            case "auth":
                _console.WriteLine("This gives me some extra help");
                break;
        }

        return Task.CompletedTask;
    }
}
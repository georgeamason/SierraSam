using Microsoft.Extensions.Logging;
using SierraSam.Core.MigrationValidators;
using Spectre.Console;

namespace SierraSam.Capabilities;

public sealed class Validate : ICapability
{
    private readonly ILogger<Validate> _logger;
    private readonly IMigrationValidator _validator;
    private readonly IAnsiConsole _console;
    private readonly TimeProvider _timeProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public Validate(
        ILogger<Validate> logger,
        IMigrationValidator validator,
        IAnsiConsole console,
        TimeProvider timeProvider
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Validate)} running");

        var startTimestamp = _timeProvider.GetTimestamp();
        var validated = _validator.Validate();
        var elapsedTime = _timeProvider.GetElapsedTime(startTimestamp);

        _console.MarkupLine(
            $"[green]Successfully validated {validated} migrations " +
            $@"(execution time {elapsedTime:mm\:ss\.fff}s)[/]"
        );
    }
}
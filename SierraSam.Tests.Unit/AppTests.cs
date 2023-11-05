using System.Collections;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SierraSam.Capabilities;
using Spectre.Console;
using Version = SierraSam.Capabilities.Version;

namespace SierraSam.Tests.Unit;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class AppTests
{
    private readonly ILogger<App> _logger = Substitute.For<ILogger<App>>();
    private readonly ICapabilityResolver _capabilityResolver = Substitute.For<ICapabilityResolver>();
    private readonly IAnsiConsole _console = Substitute.For<IAnsiConsole>();

    private static IEnumerable Get_args()
    {
        yield return new TestCaseData(Array.Empty<string>(), typeof(Help));
        yield return new TestCaseData(new[] { "help" }, typeof(Help));
        yield return new TestCaseData(new[] { "-v" }, typeof(Version));
        yield return new TestCaseData(new[] { "auth" }, typeof(Auth));
        yield return new TestCaseData(new[] { "migrate" }, typeof(Migrate));
    }
    
    [TestCaseSource(nameof(Get_args))]
    public async Task Args_call_correct_path(string[] args, Type type)
    {
        var app = new App(_logger, _capabilityResolver, _console);

        await app.Start(args);

        _capabilityResolver.Received(1).Resolve(type);

        _logger.Received().Log(
            LogLevel.Trace,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString() == "App running"),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }
}
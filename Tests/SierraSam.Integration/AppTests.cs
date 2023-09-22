using System.Collections;
using SierraSam.Capabilities;
using Version = SierraSam.Capabilities.Version;

namespace SierraSam.Tests.Integration;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class AppTests
{
    private readonly ILogger<App> _logger;

    private readonly ICapabilityResolver _capabilityResolver;

    public AppTests()
    {
        _logger = Substitute.For<ILogger<App>>();

        _capabilityResolver = Substitute.For<ICapabilityResolver>();
    }

    private static IEnumerable Get_args()
    {
        yield return new TestCaseData(Array.Empty<string>(), typeof(Help));
        yield return new TestCaseData(new[] { "help" }, typeof(Help));
        yield return new TestCaseData(new[] { "-v" }, typeof(Version));
        yield return new TestCaseData(new[] { "auth" }, typeof(Auth));
        yield return new TestCaseData(new[] { "migrate" }, typeof(Migrate));
    }
    
    [TestCaseSource(nameof(Get_args))]
    public void Args_call_correct_path(string[] args, Type type)
    {
        var app = new App(_logger, _capabilityResolver);

        app.Start(args);

        _capabilityResolver.Received(1).Resolve(type);

        _logger.Received(1).LogTrace($"App running");
    }
}
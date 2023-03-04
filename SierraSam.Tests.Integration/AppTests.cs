using System.Collections;
using SierraSam.Capabilities;
using Version = SierraSam.Capabilities.Version;

namespace SierraSam.Tests.Integration;

internal sealed class AppTests
{
    private ILogger<App> m_Logger;

    private ICapabilityResolver _mCapabilityResolver;

    [SetUp]
    public void SetUp()
    {
        m_Logger = Substitute.For<ILogger<App>>();

        _mCapabilityResolver = Substitute.For<ICapabilityResolver>();
    }

    private static IEnumerable Get_args()
    {
        yield return new TestCaseData(Array.Empty<string>(), typeof(Help));
        yield return new TestCaseData(new[] { "help" }, typeof(Help));
        yield return new TestCaseData(new[] { "--help" }, typeof(Help));
        yield return new TestCaseData(new[] { "-v" }, typeof(Version));
        yield return new TestCaseData(new[] { "auth" }, typeof(Auth));
        yield return new TestCaseData(new[] { "--auth" }, typeof(Auth));
        yield return new TestCaseData(new[] { "--migrate" }, typeof(Migrate));
        yield return new TestCaseData(new[] { "migrate" }, typeof(Migrate));
    }
    
    [TestCaseSource(nameof(Get_args))]
    public void Args_call_correct_path(string[] args, Type type)
    {
        var app = new App(m_Logger, _mCapabilityResolver);

        app.Start(args);

        _mCapabilityResolver.Received(1).Resolve(type);

        m_Logger.Received(1).LogInformation($"App running");
    }
}
using System.Collections;
using Microsoft.Extensions.Logging;

using NSubstitute;

using NUnit.Framework;

using SierraSam.Capabilities;
using Version = SierraSam.Capabilities.Version;

namespace SierraSam.Tests.Integration;

internal sealed class AppTests
{
    private ILogger m_Logger;

    private ICapabilityFactory m_CapabilityFactory;

    [SetUp]
    public void SetUp()
    {
        m_Logger = Substitute.For<ILogger>();

        m_CapabilityFactory = Substitute.For<ICapabilityFactory>();
    }

    private static IEnumerable Get_args()
    {
        yield return new TestCaseData(Array.Empty<string>(), typeof(Help));
        yield return new TestCaseData(new[] { "help" }, typeof(Help));
        yield return new TestCaseData(new[] { "--help" }, typeof(Help));
        yield return new TestCaseData(new[] { "-v" }, typeof(Version));
    }
    
    [TestCaseSource(nameof(Get_args))]
    public void Args_call_correct_path(string[] args, Type type)
    {
        var app = new App(m_Logger, m_CapabilityFactory);

        app.Run(args);

        m_CapabilityFactory.Received(1).Resolve(type);

        m_Logger.Received(1).LogInformation($"App running.");
    }
}
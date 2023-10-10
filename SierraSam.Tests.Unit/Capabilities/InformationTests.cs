using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SierraSam.Core;
using SierraSam.Capabilities;
using SierraSam.Core.Serializers;

namespace SierraSam.Tests.Unit.Capabilities;

internal sealed class InformationTests
{
    [Test]
    public void Run()
    {
        var logger = Substitute.For<ILogger<Information>>();

        var migrationMerger = Substitute.For<IMigrationMerger>();

        var printer = Substitute.For<ISerializer>();

        var sut = new Information(logger, migrationMerger, printer);

        Assert.DoesNotThrow(() => sut.Run(Array.Empty<string>()));
    }
}
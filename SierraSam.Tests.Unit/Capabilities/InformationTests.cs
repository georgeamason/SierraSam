using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SierraSam.Core;
using SierraSam.Capabilities;

namespace SierraSam.Tests.Unit.Capabilities;

internal sealed class InformationTests
{
    [Test]
    public void Run()
    {
        var logger = Substitute.For<ILogger<Information>>();

        var migrationMerger = Substitute.For<IMigrationMerger>();

        var sut = new Information(logger, migrationMerger);

        Assert.DoesNotThrow(() => sut.Run(Array.Empty<string>()));
    }
}
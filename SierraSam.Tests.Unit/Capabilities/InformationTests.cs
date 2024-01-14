using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SierraSam.Core;
using SierraSam.Capabilities;
using SierraSam.Core.Serializers;
using Spectre.Console;

namespace SierraSam.Tests.Unit.Capabilities;

internal sealed class InformationTests
{
    [Test]
    public void Run()
    {
        var logger = Substitute.For<ILogger<Information>>();
        var migrationMerger = Substitute.For<IMigrationAggregator>();
        var serializer = Substitute.For<ISerializer>();
        var console = Substitute.For<IAnsiConsole>();

        var sut = new Information(logger, migrationMerger, serializer, console);

        Assert.DoesNotThrow(() => sut.Run(Array.Empty<string>()));
    }
}
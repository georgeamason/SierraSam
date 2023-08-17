using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using SierraSam.Core;
using SierraSam.Capabilities;
using SierraSam.Core.MigrationSeekers;

namespace SierraSam.Tests.Unit.Capabilities;

internal sealed class InformationTests
{
    [Test]
    public void Run()
    {
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<Information>>();
        var database = Substitute.For<IDatabase>();
        var configuration = new Configuration();
        var migrationSeeker = Substitute.For<IMigrationSeeker>();

        var sut = new Information(logger, database, configuration, migrationSeeker);

        Assert.DoesNotThrow(() => sut.Run(Array.Empty<string>()));
    }
}
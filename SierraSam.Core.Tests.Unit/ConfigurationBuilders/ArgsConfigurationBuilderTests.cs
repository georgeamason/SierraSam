using System.Collections;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SierraSam.Core.ConfigurationReaders;

namespace SierraSam.Core.Tests.Unit.ConfigurationBuilders;

internal sealed class ArgsConfigurationBuilderTests
{
    private static IEnumerable Get_config_overrides()
    {
        yield return new TestCaseData(
            new [] { "verb", "--url=fakeConnectionString" },
            new Configuration(url: "fakeConnectionString"))
            .SetName("url is set correctly");

        yield return new TestCaseData(
            new[] { "verb", "--connectionTimeout=3" },
            new Configuration(connectionTimeout: 3))
            .SetName("connectionTimeout is set correctly");

        yield return new TestCaseData(new[] { "verb", "--connectionRetries=4" }, new Configuration(connectionRetries: 4))
            .SetName("connectionRetries is set correctly");

        yield return new TestCaseData(
            new[] { "verb", "--defaultSchema=xyz" },
            new Configuration(defaultSchema: "xyz"))
            .SetName("defaultSchema is set correctly");

        yield return new TestCaseData(
            new[] { "verb", "--initSql=ssf" },
            new Configuration(initialiseSql: "ssf"))
            .SetName("initSql is set correctly");

        yield return new TestCaseData(
                new[] { "verb", "--table=a-table" },
            new Configuration(schemaTable: "a-table"))
            .SetName("table is set correctly");

        yield return new TestCaseData(
                new[] { "verb", "--repeatableMigrationPrefix=Q" },
                new Configuration(repeatableMigrationPrefix: "Q"))
            .SetName("repeatable migration prefix is set correctly");

        yield return new TestCaseData(
                new[] { "verb", "--undoMigrationPrefix=Z" },
                new Configuration(undoMigrationPrefix: "Z"))
            .SetName("undo migration prefix is set correctly");

        yield return new TestCaseData(
                new[] { "verb", "--ignoredMigrations=*:*" },
                new Configuration(ignoredMigrations: new[] {"*:*"}))
            .SetName("ignored migrations is set correctly");

        yield return new TestCaseData(
                new[] { "verb", "--output=none" },
                new Configuration(output: "none"))
            .SetName("output is set correctly");
    }

    [TestCaseSource(nameof(Get_config_overrides))]
    public void Config_overrides_are_read_correctly(string[] args, IConfiguration expected)
    {
        var configurationBuilder = Substitute.For<IConfigurationReader>();

        configurationBuilder
            .Read()
            .Returns(new Configuration());

        var loggerFactory = Substitute.For<ILoggerFactory>();

        var argsConfigurationBuilder = new ArgsConfigurationReader(
            loggerFactory,
            args,
            configurationBuilder);

        argsConfigurationBuilder.Read().Should().BeEquivalentTo(expected);
    }
}
﻿using System.Collections;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SierraSam.Core.ConfigurationBuilders;

namespace SierraSam.Core.Tests.Unit.ConfigurationBuilders;

internal sealed class ArgsConfigurationBuilderTests
{
    private static IEnumerable Get_config_overrides()
    {
        yield return new TestCaseData
                (new[] { "verb", "--url=fakeConnectionString" }.AsEnumerable())
            .Returns(new Configuration(url: "fakeConnectionString"))
            .SetName("url is set correctly");

        yield return new TestCaseData
                (new[] { "verb", "--connectionTimeout=3" }.AsEnumerable())
            .Returns(new Configuration(connectionTimeout: 3))
            .SetName("connectionTimeout is set correctly");

        yield return new TestCaseData
                (new[] { "verb", "--connectionRetries=4" }.AsEnumerable())
            .Returns(new Configuration(connectionRetries: 4))
            .SetName("connectionRetries is set correctly");

        yield return new TestCaseData
                (new[] { "verb", "--defaultSchema=xyz" }.AsEnumerable())
            .Returns(new Configuration(defaultSchema: "xyz"))
            .SetName("defaultSchema is set correctly");

        yield return new TestCaseData
                (new[] { "verb", "--initSql=ssf" }.AsEnumerable())
            .Returns(new Configuration(initialiseSql: "ssf"))
            .SetName("initSql is set correctly");

        yield return new TestCaseData
                (new[] { "verb", "--table=a-table" }.AsEnumerable())
            .Returns(new Configuration(schemaTable: "a-table"))
            .SetName("table is set correctly");

        yield return new TestCaseData
                (new[] { "verb", "--repeatableMigrationPrefix=Q" }.AsEnumerable())
            .Returns(new Configuration(repeatableMigrationPrefix: "Q"))
            .SetName("repeatable migration prefix is set correctly");

        yield return new TestCaseData
                (new[] { "verb", "--undoMigrationPrefix=Z" }.AsEnumerable())
            .Returns(new Configuration(undoMigrationPrefix: "Z"))
            .SetName("undo migration prefix is set correctly");

        yield return new TestCaseData
                (new[] { "verb", "--ignoredMigrations=*:*" }.AsEnumerable())
            .Returns(new Configuration(ignoredMigrations: new[] {"*:*"}))
            .SetName("ignored migrations is set correctly");
    }

    [TestCaseSource(nameof(Get_config_overrides))]
    public Configuration Config_overrides_are_read_correctly(string[] args)
    {
        var configurationBuilder = Substitute.For<IConfigurationBuilder>();

        configurationBuilder
            .Build()
            .Returns(new Configuration());

        var loggerFactory = Substitute.For<ILoggerFactory>();

        var argsConfigurationBuilder = new ArgsConfigurationBuilder
            (loggerFactory, args, configurationBuilder);

        return argsConfigurationBuilder.Build();
    }
}
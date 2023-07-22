using System.Collections;
using System.IO.Abstractions;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SierraSam.Core.ConfigurationBuilders;
using SierraSam.Core.Factories;

namespace SierraSam.Core.Tests.Unit.ConfigurationBuilders;

internal sealed class JsonConfigurationBuilderTests
{
    private static IEnumerable Example_Configs()
    {
        yield return new TestCaseData
                ("{ \"url\": \"Driver={ODBC Driver 17 for SQL Server};Server=myServerAddress;Database=myDataBase;\" }")
            .Returns(new Configuration
                (url: "Driver={ODBC Driver 17 for SQL Server};Server=myServerAddress;Database=myDataBase;"))
            .SetName("Url is set correctly");

        yield return new TestCaseData("{ \"connectionTimeout\": 5 }")
            .Returns(new Configuration(connectionTimeout: 5))
            .SetName("Connection timeout is set correctly");

        yield return new TestCaseData("{ \"connectionRetries\": 4 }")
            .Returns(new Configuration(connectionRetries: 4))
            .SetName("Connection retries is set correctly");

        yield return new TestCaseData("{ \"defaultSchema\": \"dbo\" }")
            .Returns(new Configuration(defaultSchema: "dbo"))
            .SetName("Default schema is set correctly");

        yield return new TestCaseData("{ \"initialiseSql\": \"ssf\" }")
            .Returns(new Configuration(initialiseSql: "ssf"))
            .SetName("Initialise sql is set correctly");

        yield return new TestCaseData("{ \"schemaTable\": \"tableName\" }")
            .Returns(new Configuration(schemaTable: "tableName"))
            .SetName("Schema table is set correctly");

        yield return new TestCaseData(string.Empty)
            .Returns(new Configuration())
            .SetName("empty config");
    }

    [TestCaseSource(nameof(Example_Configs))]
    public Configuration Config_file_is_read_correctly(string config)
    {
        var logger = Substitute.For<ILogger<ConfigurationFactory>>();
        var fileSystem = Substitute.For<IFileSystem>();

        fileSystem.File.Exists(string.Empty).ReturnsForAnyArgs(true);
        fileSystem.File.ReadAllText(string.Empty).ReturnsForAnyArgs(config);

        var jsonBuilder = new JsonConfigurationBuilder
            (logger, fileSystem, new []{ string.Empty });

        return jsonBuilder.Build();
    }

    [Test]
    public void Config_file_throws_for_bad_config()
    {
        var logger = Substitute.For<ILogger<ConfigurationFactory>>();
        var fileSystem = Substitute.For<IFileSystem>();

        fileSystem
            .File
            .Exists(string.Empty)
            .ReturnsForAnyArgs(true);

        fileSystem
            .File
            .ReadAllText(string.Empty)
            .ReturnsForAnyArgs("{ \"this\": \"is\" } not json");

        var jsonBuilder = new JsonConfigurationBuilder
            (logger, fileSystem, new []{ string.Empty });

        var func = () => jsonBuilder.Build();

        func.Should().Throw<JsonException>();
    }

    [Test]
    public void Config_file_not_found_returns_default()
    {
        var logger = Substitute.For<ILogger<ConfigurationFactory>>();
        var fileSystem = Substitute.For<IFileSystem>();

        fileSystem
            .File
            .Exists(string.Empty)
            .ReturnsForAnyArgs(false);

        var jsonBuilder = new JsonConfigurationBuilder
            (logger, fileSystem, new[] { string.Empty });

        jsonBuilder.Build().Should().BeEquivalentTo(new Configuration());
    }
}
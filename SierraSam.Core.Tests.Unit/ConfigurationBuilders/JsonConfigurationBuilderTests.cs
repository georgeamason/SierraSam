using System.Collections;
using System.IO.Abstractions;
using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using SierraSam.Core.ConfigurationReaders;

namespace SierraSam.Core.Tests.Unit.ConfigurationBuilders;

internal sealed class JsonConfigurationBuilderTests
{
    private static IEnumerable Example_Configs()
    {
        yield return new TestCaseData(
                "{ \"url\": \"Driver={ODBC Driver 17 for SQL Server};Server=myServerAddress;Database=myDataBase;\" }",
                new Configuration(url: "Driver={ODBC Driver 17 for SQL Server};Server=myServerAddress;Database=myDataBase;"))
            .SetName("Url is set correctly");

        yield return new TestCaseData(
                "{ \"connectionTimeout\": 5 }",
                new Configuration(connectionTimeout: 5))
            .SetName("Connection timeout is set correctly");

        yield return new TestCaseData(
                "{ \"connectionRetries\": 4 }",
                new Configuration(connectionRetries: 4))
            .SetName("Connection retries is set correctly");

        yield return new TestCaseData(
                "{ \"defaultSchema\": \"dbo\" }",
                new Configuration(defaultSchema: "dbo"))
            .SetName("Default schema is set correctly");

        yield return new TestCaseData(
                "{ \"initialiseSql\": \"ssf\" }",
                new Configuration(initialiseSql: "ssf"))
            .SetName("Initialise sql is set correctly");

        yield return new TestCaseData(
                "{ \"schemaTable\": \"tableName\" }",
                new Configuration(schemaTable: "tableName"))
            .SetName("Schema table is set correctly");

        yield return new TestCaseData(
                string.Empty,
                new Configuration())
            .SetName("empty config");
    }

    [TestCaseSource(nameof(Example_Configs))]
    public void Config_file_is_read_correctly(string config, IConfiguration expected)
    {
        var fileSystem = Substitute.For<IFileSystem>();

        fileSystem.File.Exists(string.Empty).ReturnsForAnyArgs(true);
        fileSystem.File.ReadAllText(string.Empty).ReturnsForAnyArgs(config);

        var jsonBuilder = new JsonConfigurationReader
            (fileSystem, new []{ string.Empty });

        jsonBuilder.Read().Should().BeEquivalentTo(expected);
    }

    [Test]
    public void Config_file_throws_for_bad_config()
    {
        var fileSystem = Substitute.For<IFileSystem>();

        fileSystem
            .File
            .Exists(string.Empty)
            .ReturnsForAnyArgs(true);

        fileSystem
            .File
            .ReadAllText(string.Empty)
            .ReturnsForAnyArgs("{ \"this\": \"is\" } not json");

        var jsonBuilder = new JsonConfigurationReader
            (fileSystem, new []{ string.Empty });

        var func = () => jsonBuilder.Read();

        func.Should().Throw<JsonException>();
    }

    [Test]
    public void Config_file_not_found_returns_default()
    {
        var fileSystem = Substitute.For<IFileSystem>();

        fileSystem
            .File
            .Exists(string.Empty)
            .ReturnsForAnyArgs(false);

        var jsonBuilder = new JsonConfigurationReader
            (fileSystem, new[] { string.Empty });

        jsonBuilder.Read().Should().BeEquivalentTo(new Configuration());
    }
}
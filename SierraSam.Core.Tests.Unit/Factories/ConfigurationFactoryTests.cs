using System.Collections;
using System.IO.Abstractions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SierraSam.Core.Factories;

namespace SierraSam.Core.Tests.Unit.Factories;

internal sealed class ConfigurationFactoryTests
{
    private static IEnumerable Get_config()
    {
        yield return new TestCaseData
                ("{ \"url\": \"Driver={ODBC Driver 17 for SQL Server};Server=myServerAddress;Database=myDataBase;\" }")
            .Returns(new Configuration { Url = "Driver={ODBC Driver 17 for SQL Server};Server=myServerAddress;Database=myDataBase;" })
            .SetName("Url is set correctly");
    
        yield return new TestCaseData("{ \"connectionTimeout\": 5 }")
            .Returns(new Configuration { ConnectionTimeout = 5 })
            .SetName("Connection timeout is set correctly");
        
        yield return new TestCaseData("{ \"connectionRetries\": 4 }")
            .Returns(new Configuration { ConnectionRetries = 4 })
            .SetName("Connection retries is set correctly");

        yield return new TestCaseData("{ \"defaultSchema\": \"dbo\" }")
            .Returns(new Configuration { DefaultSchema = "dbo" })
            .SetName("Default schema is set correctly");

        yield return new TestCaseData("{ \"initialiseSql\": \"ssf\" }")
            .Returns(new Configuration { InitialiseSql = "ssf" })
            .SetName("Initialise sql is set correctly");

        yield return new TestCaseData("{ \"schemaTable\": \"tableName\" }")
            .Returns(new Configuration { SchemaTable = "tableName" })
            .SetName("Schema table is set correctly");
        
        yield return new TestCaseData(string.Empty)
            .Returns(new Configuration())
            .SetName("empty config");
    }

    [TestCaseSource(nameof(Get_config))]
    public Configuration Config_file_is_read_correctly(string config)
    {
        var logger = Substitute.For<ILogger<ConfigurationFactory>>();
        var fileSystem = Substitute.For<IFileSystem>();

        fileSystem.File.Exists(string.Empty).ReturnsForAnyArgs(true);
        fileSystem.File.ReadAllText(string.Empty).ReturnsForAnyArgs(config);

        var factory = new ConfigurationFactory
            (logger, fileSystem, new[] { string.Empty });

        return factory.Create(Array.Empty<string>());
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

        var factory = new ConfigurationFactory
            (logger, fileSystem, new[] { string.Empty });

        Assert.That
            (() => factory.Create(Array.Empty<string>()),
             Throws.InstanceOf<JsonException>());
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

        var factory = new ConfigurationFactory
            (logger, fileSystem, new[] { string.Empty });

        Assert.That
        (() => factory.Create(Array.Empty<string>()), Is.EqualTo(new Configuration()));
    }

    private static IEnumerable Get_config_overrides()
    {
        yield return new TestCaseData
                (new[] { "verb", "--url=fakeConnectionString" }.AsEnumerable())
            .Returns(new Configuration { Url = "fakeConnectionString" })
            .SetName("url is set correctly");

        yield return new TestCaseData
                (new[] { "verb", "--connectionTimeout=3" }.AsEnumerable())
            .Returns(new Configuration { ConnectionTimeout = 3 })
            .SetName("connectionTimeout is set correctly");

        yield return new TestCaseData
                (new[] { "verb", "--connectionRetries=4" }.AsEnumerable())
            .Returns(new Configuration { ConnectionRetries = 4 })
            .SetName("connectionRetries is set correctly");

        yield return new TestCaseData
                (new[] { "verb", "--defaultSchema=xyz" }.AsEnumerable())
            .Returns(new Configuration { DefaultSchema = "xyz" })
            .SetName("defaultSchema is set correctly");

        yield return new TestCaseData
                (new[] { "verb", "--initSql=ssf" }.AsEnumerable())
            .Returns(new Configuration { InitialiseSql = "ssf" })
            .SetName("initSql is set correctly");

        yield return new TestCaseData
                (new[] { "verb", "--table=a-table" }.AsEnumerable())
            .Returns(new Configuration { SchemaTable = "a-table" })
            .SetName("table is set correctly");
    }

    [TestCaseSource(nameof(Get_config_overrides))]
    public Configuration Config_overrides_are_read_correctly(string[] args)
    {
        var logger = Substitute.For<ILogger<ConfigurationFactory>>();
        var fileSystem = Substitute.For<IFileSystem>();

        var factory = new ConfigurationFactory
            (logger, fileSystem, Array.Empty<string>());

        return factory.Create(args);
    }
}
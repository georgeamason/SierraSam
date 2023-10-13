using FluentAssertions;
using SierraSam.Core.Enums;
using SierraSam.Core.Serializers;
using Spectre.Console.Testing;

namespace SierraSam.Core.Tests.Unit.Serializers;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class YamlSerializerTests
{
    private readonly YamlSerializer _serializer = new();
    private readonly TestConsole _console = new ();

    [Test]
    public void Serialize_returns_expected_response()
    {
        var installedOn = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var content = new TerseMigration(
            MigrationType.Versioned,
            "1",
            "someDescription",
            "someType",
            "someChecksum",
            installedOn,
            MigrationState.Pending);

        var result = _serializer.Serialize(content);

        _console.Write(result);

        const string expected =
            """
            migrationType: Versioned
            version: 1
            description: someDescription
            type: someType
            checksum: someChecksum
            installedOn: 2023-01-01T00:00:00.0000000Z
            state: Pending
            
            """;

        _console.Output.Should().Be(expected.NormalizeLineEndings());
    }

    [Test]
    public void Serialize_ignores_null_values()
    {
        var content = new TerseMigration(
            MigrationType.Versioned,
            "1",
            "someDescription",
            "someType",
            "someChecksum",
            null,
            MigrationState.Pending);

        var result = _serializer.Serialize(content);

        _console.Write(result);

        const string expected =
            """
            migrationType: Versioned
            version: 1
            description: someDescription
            type: someType
            checksum: someChecksum
            state: Pending
            
            """;

        _console.Output.Should().Be(expected.NormalizeLineEndings());
    }
}
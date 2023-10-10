using FluentAssertions;
using SierraSam.Core.Enums;
using Spectre.Console.Testing;

namespace SierraSam.Core.Tests.Unit.Serializers;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class JsonSerializerTests
{
    private readonly Core.Serializers.JsonSerializer _serializer = new();
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
            {
              "MigrationType": "Versioned",
              "Version": "1",
              "Description": "someDescription",
              "Type": "someType",
              "Checksum": "someChecksum",
              "InstalledOn": "2023-01-01T00:00:00Z",
              "State": "Pending"
            }
            """;

        _console.Output.Should().Be(expected.NormalizeLineEndings());
    }
}
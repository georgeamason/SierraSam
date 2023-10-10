using FluentAssertions;
using SierraSam.Core.Enums;
using Spectre.Console;

namespace SierraSam.Core.Tests.Unit.Serializers;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class EmptySerializerTests
{
    private readonly Core.Serializers.EmptySerializer _serializer = new();

    [Test]
    public void Serialize_returns_expected_response()
    {
        var content = new TerseMigration(
            MigrationType.Versioned,
            "1",
            "someDescription",
            "someType",
            "someChecksum",
            DateTime.UtcNow,
            MigrationState.Pending);

        var result = _serializer.Serialize(content);

        result.Should().BeEquivalentTo(Text.Empty);
    }
}
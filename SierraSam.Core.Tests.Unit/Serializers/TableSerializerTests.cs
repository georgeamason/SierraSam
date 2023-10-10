using FluentAssertions;
using SierraSam.Core.Serializers;
using Spectre.Console.Testing;

namespace SierraSam.Core.Tests.Unit.Serializers;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
internal sealed class TableSerializerTests
{
    private readonly TableSerializer _serializer = new();
    private readonly TestConsole _console = new ();

    private record ExampleForTest(string Forename, string Surname);

    [Test]
    public void Serialize_returns_expected_response()
    {
        var content = new ExampleForTest[]
        {
            new ("Joe", "Blogs"),
            new ("Karen", "Smith")
        };

        var result = _serializer.Serialize(content);

        _console.Write(result);

        const string expected =
            """
                                  
            | Forename | Surname |
            | :------- | :------ |
            | Joe      | Blogs   |
            | Karen    | Smith   |
                                  
            
            """;

        _console.Output.Should().Be(expected.NormalizeLineEndings());
    }

    [Test]
    public void Serialize_returns_expected_response_for_non_array()
    {
        var content = new ExampleForTest("Joe", "Blogs");

        var result = _serializer.Serialize(content);

        _console.Write(result);

        const string expected =
            """
                                  
            | Forename | Surname |
            | :------- | :------ |
            | Joe      | Blogs   |
                                  
            
            """;

        _console.Output.Should().Be(expected.NormalizeLineEndings());
    }
}
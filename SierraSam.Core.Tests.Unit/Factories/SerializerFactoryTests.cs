using FluentAssertions;
using NSubstitute;
using SierraSam.Core.Factories;
using SierraSam.Core.Serializers;

namespace SierraSam.Core.Tests.Unit.Factories;

internal sealed class SerializerFactoryTests
{
    [TestCase("json", typeof(JsonSerializer))]
    [TestCase("yaml", typeof(YamlSerializer))]
    [TestCase("none", typeof(EmptySerializer))]
    public void Factory_creates_correct_implementation_from_configuration(
        string output,
        Type implementation)
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration.Output.Returns(output);

        var result = SerializerFactory.Create(configuration);

        result.Should().BeOfType(implementation);
    }
}
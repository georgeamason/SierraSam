using Spectre.Console;
using Spectre.Console.Rendering;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SierraSam.Core.Serializers;

internal sealed class YamlSerializer : ISerializer
{
    public IRenderable Serialize<T>(T content)
    {
        var builder = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        return new Text(builder.Serialize(content));
    }
}
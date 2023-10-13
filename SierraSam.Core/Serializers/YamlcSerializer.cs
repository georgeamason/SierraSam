using System.Text.RegularExpressions;
using Spectre.Console;
using Spectre.Console.Rendering;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SierraSam.Core.Serializers;

internal sealed partial class YamlcSerializer : ISerializer
{
    public IRenderable Serialize<T>(T content)
    {
        var builder = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        var yaml = YamlKeyRegex()
            .Replace(
                builder.Serialize(content),
                match => $"[rgb(114,159,207)]{match.Value}[/]"
            );

        return new Markup(yaml);
    }

    [GeneratedRegex("[A-z]+(?=:\\s)")]
    private static partial Regex YamlKeyRegex();
}
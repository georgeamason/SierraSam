using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SierraSam.Core.Serializers;

internal sealed class JsonSerializer : ISerializer
{
    public IRenderable Serialize<T>(T content)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            content,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            }
        );

        return new Text(json);
    }
}
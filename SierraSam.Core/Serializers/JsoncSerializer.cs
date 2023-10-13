using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console.Json;
using Spectre.Console.Rendering;
using Color = Spectre.Console.Color;

namespace SierraSam.Core.Serializers;

internal sealed class JsoncSerializer : ISerializer
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
                    new JsonStringEnumConverter(),
                }
            }
        );

        return new JsonText(json)
            .MemberColor(new Color(114, 159, 207))
            .StringColor(new Color(196, 160, 0))
            .BooleanColor(new Color(52, 101, 164))
            .NullColor(Color.Red3)
            .ColonColor(Color.White)
            .BracketColor(Color.White)
            .BracesColor(Color.White);
    }
}
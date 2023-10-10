using Spectre.Console;
using Spectre.Console.Rendering;

namespace SierraSam.Core.Serializers;

internal sealed class EmptySerializer : ISerializer
{
    public IRenderable Serialize<T>(T content) => new Text(string.Empty);
}
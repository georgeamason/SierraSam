using Spectre.Console.Rendering;

namespace SierraSam.Core.Serializers;

public interface ISerializer
{
    IRenderable Serialize<T>(T content);
}
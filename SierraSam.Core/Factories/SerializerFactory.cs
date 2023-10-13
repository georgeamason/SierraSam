using SierraSam.Core.Serializers;

namespace SierraSam.Core.Factories;

public static class SerializerFactory
{
    public static ISerializer Create(IConfiguration configuration)
    {
        return configuration.Output switch
        {
            "json" => new JsonSerializer(),
            "jsonc" => new JsoncSerializer(),
            "yaml" => new YamlSerializer(),
            "yamlc" => new YamlcSerializer(),
            "table" => new TableSerializer(),
            "none" => new EmptySerializer(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
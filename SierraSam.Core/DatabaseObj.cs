namespace SierraSam.Core;

public sealed class DatabaseObj(string schema, string name, string? type, string? parent = null)
{
    public string Schema { get; } = schema ?? throw new ArgumentNullException(nameof(schema));

    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    public string? Type { get; } = type;

    public string? Parent { get; } = parent;
}
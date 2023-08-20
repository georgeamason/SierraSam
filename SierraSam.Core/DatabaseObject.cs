namespace SierraSam.Core;

public sealed class DatabaseObject
{
    public DatabaseObject(string schema, string name, string type, string? parent = null)
    {
        Schema = schema;
        Name = name;
        Type = type;
        Parent = parent;
    }

    public string Schema { get; }

    public string Name { get; }

    public string Type { get; }

    public string? Parent { get; }
}
using System.Collections;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SierraSam.Core.Serializers;

internal sealed class TableSerializer : ISerializer
{
    public IRenderable Serialize<T>(T content)
    {
        ArgumentNullException.ThrowIfNull(content, nameof(content));

        var type = typeof(T) switch
        {
            { IsGenericType: true } => typeof(T).GetGenericArguments().First(),
            { IsArray: true } or { IsPointer: true } and { IsByRef: true } => typeof(T).GetElementType()!,
            _ => typeof(T)
        };

        var properties = type.GetProperties();

        var table = new Table
        {
            Border = TableBorder.Markdown
        };

        foreach (var prop in properties)
        {
            table.AddColumn(prop.Name, column => column.Alignment = Justify.Left);
        }

        foreach (var obj in content as IEnumerable ?? new [] { content })
        {
            var values = new List<string>();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(obj);

                values.Add(value?.ToString() ?? string.Empty);
            }

            table.AddRow(values.ToArray());
        }

        return table;
    }
}
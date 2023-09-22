using System.Data.Odbc;

namespace SierraSam.Core.Extensions;

public static class OdbcConnectionStringBuilderExtensions
{
    public static string GetValue(this OdbcConnectionStringBuilder builder, string key)
    {
        var keyValue = builder.ConnectionString
            .Split(';')
            .Single(kvp => kvp.TrimStart().StartsWith(key));

        return keyValue
            .Split('=')
            .Last();
    }
}
using System.Data;

namespace SierraSam.Core.Extensions;

public static class DataRowExtensions
{
    public static bool GetBoolean(this DataRow row, string name)
    {
        var value = row[name];

        if (value is bool b)
            return b;

        var bit = Convert.ToInt32(value);
        
        return Convert.ToBoolean(bit);
    }

    public static string GetString(this DataRow row, string name)
    {
        return Convert.ToString(row[name])!;
    }

    public static DateTime GetDateTime(this DataRow row, string name)
    {
        return Convert.ToDateTime(row[name]);
    } 
}
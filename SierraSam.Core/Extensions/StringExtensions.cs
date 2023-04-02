using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SierraSam.Core.Extensions;

public static class StringExtensions
{
    public static bool IsJson(this string json, out Exception? exception)
    {
        exception = null;

        try
        {
            JsonDocument.Parse(json, new JsonDocumentOptions()
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            return true;
        }
        catch (JsonException ex)
        {
            exception = ex;
            return false;
        }
    }
    
    public static string Checksum(this string contents)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(contents));

        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
    }
}
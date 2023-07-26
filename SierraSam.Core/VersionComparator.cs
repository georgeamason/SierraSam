using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SierraSam.Core;

public static class VersionComparator
{
    [SuppressMessage("GeneratedRegex", "SYSLIB1045:Convert to \'GeneratedRegexAttribute\'.")]
    public static bool Compare(string version1, string version2)
    {
        ArgumentNullException.ThrowIfNull(version1, nameof(version1));
        ArgumentNullException.ThrowIfNull(version2, nameof(version2));

        static IEnumerable<string> GetVersionComponents(string version)
        {
            return Regex.Matches(version, "\\d").Select(m => m.Value);
        }

        var v1 = string.Join(string.Empty, GetVersionComponents(version1));
        var v2 = string.Join(string.Empty, GetVersionComponents(version2));;

        return long.Parse(v1) > long.Parse(v2);
    }
}
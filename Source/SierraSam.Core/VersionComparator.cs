using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SierraSam.Core;

public partial class VersionComparator
{
    private readonly string _version;

    public VersionComparator(string version)
    {
        _version = version ?? throw new ArgumentNullException(nameof(version));
    }

    public bool IsGreaterThan(string version)
    {
        ArgumentNullException.ThrowIfNull(version, nameof(version));

        var v1 = string.Concat(GetDigits(_version));
        var v2 = string.Concat(GetDigits(version));

        return long.Parse(v1) > long.Parse(v2);
    }

    public bool IsLessThanOrEqualTo(string version)
    {
        ArgumentNullException.ThrowIfNull(version, nameof(version));

        var v1 = string.Concat(GetDigits(_version));
        var v2 = string.Concat(GetDigits(version));

        return long.Parse(v1) <= long.Parse(v2);
    }

    private static IEnumerable<string> GetDigits(string version)
    {
        return DigitRegex().Matches(version).Select(m => m.Value);
    }

    [GeneratedRegex("\\d")]
    private static partial Regex DigitRegex();
}
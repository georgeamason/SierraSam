using System.Text.RegularExpressions;

namespace SierraSam.Core.Constants;

internal static class MigrationRegex
{
    public static readonly Regex VersionRegex = new(@"((\d+)((\.{1}\d+)*)(\.{0}))?");

    public static readonly Regex DescriptionRegex = new(@"(\w|\s)+");
}
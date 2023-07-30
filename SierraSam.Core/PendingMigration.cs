
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace SierraSam.Core;

[SuppressMessage("GeneratedRegex", "SYSLIB1045:Convert to \'GeneratedRegexAttribute\'.")]
public sealed class PendingMigration
{
    public static PendingMigration Parse(Configuration configuration, IFileInfo fileInfo)
    {
        var version = GetVersion(configuration, fileInfo.Name);

        var description = GetDescription(configuration, fileInfo.Name);

        return new PendingMigration
            (string.IsNullOrEmpty(version) ? null : version,
             string.IsNullOrEmpty(description) ? null : description,
             fileInfo.FullName,
             fileInfo.Name);
    }

    public string? Version { get; }

    public string? Description { get; }

    public string FilePath { get; }

    public string FileName { get; }

    private PendingMigration(string? version,
                             string? description,
                             string filePath,
                             string fileName)
    {
        Version = version;

        Description = description;

        FilePath = filePath
            ?? throw new ArgumentNullException(nameof(filePath));

        FileName = fileName
            ?? throw new ArgumentNullException(nameof(fileName));
    }

    private static string? GetVersion(Configuration configuration, string fileName) =>
        Regex.Match(fileName,
             $"(?<={configuration.MigrationPrefix})" +
            @$"((\d+)((\.{{1}}\d+)*)(\.{{0}}))?" +
             $"(?={configuration.MigrationSeparator})").Value;

    private static string GetDescription(Configuration configuration, string fileName) =>
        Regex.Match(fileName,
             $"(?<={configuration.MigrationSeparator})" +
            @$"(\w|\s)+" +
            @$"(?={string.Join('|', configuration.MigrationSuffixes.Select(suffix => @$"\{suffix}").ToArray())})").Value;
}
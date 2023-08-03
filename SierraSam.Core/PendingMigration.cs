
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
             string.IsNullOrEmpty(description) ? null! : description,
             fileInfo.Name.StartsWith(configuration.RepeatableMigrationPrefix)
                 ? MigrationType.Repeatable
                 : MigrationType.Versioned,
             fileInfo.FullName,
             fileInfo.Name);
    }

    public string? Version { get; }

    public string Description { get; }

    public MigrationType MigrationType { get; }

    public string FilePath { get; }

    public string FileName { get; }

    internal PendingMigration(string? version,
                             string description,
                             MigrationType migrationType,
                             string filePath,
                             string fileName)
    {
        if (migrationType is MigrationType.Versioned && string.IsNullOrEmpty(version))
        {
            throw new ArgumentNullException(nameof(version),
                "Version cannot be null or empty for versioned migrations.");
        }

        Version = version;

        Description = description
            ?? throw new ArgumentNullException(nameof(description));

        MigrationType = migrationType;

        FilePath = filePath
            ?? throw new ArgumentNullException(nameof(filePath));

        FileName = fileName
            ?? throw new ArgumentNullException(nameof(fileName));
    }

    private static string? GetVersion(Configuration configuration, string fileName)
    {
        var prefixes = string.Join('|', configuration.MigrationPrefix, configuration.UndoMigrationPrefix);

        // TODO: Pull out the regex pattern into a shared constant.
        // It's used by both the FileSystemMigrationSeeker and this class.
        var pattern = $"(?<={prefixes})" +
                      @$"((\d+)((\.{{1}}\d+)*)(\.{{0}}))?" +
                      $"(?={configuration.MigrationSeparator})";

        return Regex.Match(fileName, pattern).Value;
    }

    private static string GetDescription(Configuration configuration, string fileName)
    {
        var suffixes = string.Join('|', configuration.MigrationSuffixes
            .Select(suffix => @$"\{suffix}")
            .ToArray());

        var pattern = $"(?<={configuration.MigrationSeparator})" +
                      @$"(\w|\s)+" +
                      @$"(?={suffixes})";

        return Regex.Match(fileName, pattern).Value;
    }
}
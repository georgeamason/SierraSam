using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using SierraSam.Core.Constants;
using SierraSam.Core.Enums;
using SierraSam.Core.Extensions;

namespace SierraSam.Core;

[SuppressMessage("GeneratedRegex", "SYSLIB1045:Convert to \'GeneratedRegexAttribute\'.")]
public sealed class PendingMigration
{
    internal PendingMigration(
        string? version,
        string description,
        MigrationType migrationType,
        string sql,
        string fileName)
    {
        if (string.IsNullOrEmpty(version) && migrationType is MigrationType.Versioned)
        {
            throw new ArgumentNullException(nameof(version),
                "Version cannot be null or empty for versioned migrations.");
        }

        if (description == string.Empty)
        {
            throw new ArgumentNullException(nameof(description),
                "Description cannot be empty.");
        }

        Version = string.IsNullOrEmpty(version) ? null : version;

        Description = description ?? throw new ArgumentNullException(nameof(description));

        MigrationType = migrationType;

        Sql = sql ?? throw new ArgumentNullException(nameof(sql));

        Checksum = sql.Checksum();

        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
    }

    public string? Version { get; }

    public string Description { get; }

    public MigrationType MigrationType { get; }

    public string Sql { get; }

    public string Checksum { get; }

    public string FileName { get; }

    public static PendingMigration Parse(IConfiguration configuration, IFileInfo fileInfo)
    {
        var migrationType = fileInfo.Name.StartsWith(configuration.RepeatableMigrationPrefix)
            ? MigrationType.Repeatable
            : MigrationType.Versioned;

        return new PendingMigration(
            ParseVersion(configuration, fileInfo.Name),
            ParseDescription(configuration, fileInfo.Name),
            migrationType,
            fileInfo.FileSystem.File.ReadAllText(fileInfo.FullName),
            fileInfo.Name);
    }

    private static string? ParseVersion(IConfiguration configuration, string fileName)
    {
        var prefixes = string.Join('|', new[]
            {
                configuration.MigrationPrefix,
                configuration.UndoMigrationPrefix
            }
        );

        var pattern = $"(?<={prefixes})" +
                      @$"{MigrationRegex.VersionRegex}" +
                      $"(?={configuration.MigrationSeparator})";

        var version = Regex.Match(fileName, pattern).Value;

        return string.IsNullOrEmpty(version) ? null : version;
    }

    private static string ParseDescription(IConfiguration configuration, string fileName)
    {
        var suffixes = string.Join('|', configuration.MigrationSuffixes.Select(suffix => @$"\{suffix}"));

        var pattern = $"(?<={configuration.MigrationSeparator})" +
                      @$"{MigrationRegex.DescriptionRegex}" +
                      @$"(?={suffixes})";

        return Regex.Match(fileName, pattern).Value;
    }
}
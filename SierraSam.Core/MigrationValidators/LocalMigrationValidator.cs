using System.Diagnostics;
using System.IO.Abstractions;
using SierraSam.Core.Extensions;

namespace SierraSam.Core.MigrationValidators;

/// <summary>
/// Validation fails if local migrations haven't been applied
/// to the database.
/// </summary>
internal sealed class LocalMigrationValidator : IMigrationValidator
{
    private readonly IFileSystem _fileSystem;

    private readonly IReadOnlyCollection<(string Type, string Status)> _ignoredMigrations;

    private readonly IMigrationValidator _validator;

    public LocalMigrationValidator
        (IFileSystem fileSystem,
         IReadOnlyCollection<(string Type, string Status)> ignoredMigrations,
         IMigrationValidator validator)
    {
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));

        _ignoredMigrations = ignoredMigrations
            ?? throw new ArgumentNullException(nameof(ignoredMigrations));

        _validator = validator
            ?? throw new ArgumentNullException(nameof(validator));
    }

    public TimeSpan Validate
        (IReadOnlyCollection<AppliedMigration> appliedMigrations,
         IReadOnlyCollection<PendingMigration> discoveredMigrations)
    {
        if (appliedMigrations == null) throw new ArgumentNullException(nameof(appliedMigrations));
        if (discoveredMigrations == null) throw new ArgumentNullException(nameof(discoveredMigrations));

        var executionTime = _validator.Validate
            (appliedMigrations, discoveredMigrations);

        var toIgnore = _ignoredMigrations
            .Where(pattern => pattern.Status.ToLower() == "pending" || pattern.Status == "*")
            .ToArray();

        if (toIgnore.Any(pattern => pattern.Type == "*"))
            return executionTime;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var filteredDiscoveredMigrations = discoveredMigrations
            .Where(m =>
            {
                var types = toIgnore
                    .Select(p => p.Type.ToLower())
                    .ToArray();

                return types switch
                {
                    ["repeatable"] => m.MigrationType is not MigrationType.Repeatable,
                    ["versioned"] => m.MigrationType is not MigrationType.Versioned,
                    ["repeatable", "versioned"] or ["versioned", "repeatable"] => false,
                    _ => true
                };
            });

        foreach (var discoveredMigration in filteredDiscoveredMigrations)
        {
            var appliedMigration = appliedMigrations
                .SingleOrDefault(m =>
                {
                    var migrationSql = _fileSystem.File.ReadAllText
                        (discoveredMigration.FilePath);

                    return m.Version == discoveredMigration.Version &&
                           m.Script == discoveredMigration.FileName &&
                           m.Type == "SQL" &&
                           m.Checksum == migrationSql.Checksum();
                });

            if (appliedMigration is null)
            {
                throw new Exception(
                    $"Unable to find remote migration {discoveredMigration.FileName}");
            }
        }

        stopwatch.Stop();

        return executionTime.Add(stopwatch.Elapsed);
    }
}
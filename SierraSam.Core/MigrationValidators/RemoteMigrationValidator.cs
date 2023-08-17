using System.Diagnostics;
using SierraSam.Core.Enums;

namespace SierraSam.Core.MigrationValidators;

/// <summary>
/// Validation fails if applied migrations haven't been discovered
/// locally by the migration seeker.
/// </summary>
internal sealed class RemoteMigrationValidator : IMigrationValidator
{
    private readonly IReadOnlyCollection<(string Type, string Status)> _ignoredMigrations;

    private readonly IMigrationValidator _validator;

    public RemoteMigrationValidator
        (IReadOnlyCollection<(string Type, string Status)> ignoredMigrations,
         IMigrationValidator validator)
    {
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
            .Where(pattern => pattern.Status.ToLower() == "missing" || pattern.Status == "*")
            .ToArray();

        if (toIgnore.Any(pattern => pattern.Type == "*"))
            return executionTime;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var filteredAppliedMigrations = appliedMigrations
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

        foreach (var appliedMigration in filteredAppliedMigrations)
        {
            var discoveredMigration = discoveredMigrations
                .SingleOrDefault(m => m.Version == appliedMigration.Version &&
                                      m.FileName == appliedMigration.Script &&
                                      "SQL" == appliedMigration.Type &&
                                      m.Checksum == appliedMigration.Checksum);

            if (discoveredMigration is null)
            {
                throw new Exception
                    ($"Unable to find local migration {appliedMigration.Script}");
            }
        }

        stopwatch.Stop();

        return executionTime.Add(stopwatch.Elapsed);
    }
}
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

    private readonly IMigrationValidator _validator;

    public LocalMigrationValidator
        (IFileSystem fileSystem,
         IMigrationValidator validator)
    {
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));

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

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // TODO: This should be skipped by default!
        // Seems a bit crazy to fail validation if you have pending migrations!
        foreach (var discoveredMigration in discoveredMigrations)
        {
            var migrationSql = _fileSystem.File.ReadAllText
                (discoveredMigration.FilePath);

            var appliedMigration = appliedMigrations
                .SingleOrDefault(m => m.Version == discoveredMigration.Version &&
                                      m.Script == discoveredMigration.FileName &&
                                      m.Type == "SQL" &&
                                      m.Checksum == migrationSql.Checksum());

            if (appliedMigration is null)
            {
                throw new Exception(
                    $"Unable to find remote migration {discoveredMigration.FileName} [{migrationSql.Checksum()}]");
            }
        }

        stopwatch.Stop();

        return executionTime.Add(stopwatch.Elapsed);
    }
}
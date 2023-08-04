using System.Diagnostics;
using System.IO.Abstractions;
using SierraSam.Core.Extensions;

namespace SierraSam.Core.MigrationValidators;

/// <summary>
/// Validation fails if applied migrations haven't been discovered
/// locally by the migration seeker.
/// </summary>
internal sealed class RemoteMigrationValidator : IMigrationValidator
{
    private readonly IFileSystem _fileSystem;

    private readonly Configuration _configuration;
    private readonly IMigrationValidator _validator;

    public RemoteMigrationValidator
        (IFileSystem fileSystem,
         Configuration configuration,
         IMigrationValidator validator)
    {
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

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

        // TODO: You should be able to skip this step if you want
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var appliedMigration in appliedMigrations)
        {
            var discoveredMigration = discoveredMigrations
                .SingleOrDefault(m =>
                {
                    var migrationSql = _fileSystem.File.ReadAllText(m.FilePath);

                    return m.Version == appliedMigration.Version &&
                           m.FileName == appliedMigration.Script &&
                           "SQL" == appliedMigration.Type &&
                           migrationSql.Checksum() == appliedMigration.Checksum;
                });

            if (discoveredMigration is null)
            {
                throw new Exception
                    ($"Unable to find local migration {appliedMigration.Script} [{appliedMigration.Checksum}]");
            }
        }

        stopwatch.Stop();

        return executionTime.Add(stopwatch.Elapsed);
    }
}
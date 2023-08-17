using System.IO.Abstractions;
using SierraSam.Core.MigrationValidators;

namespace SierraSam.Core.Factories;

public static class MigrationValidatorFactory
{
    public static IMigrationValidator Create(Configuration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        var ignoredMigrations = configuration.IgnoredMigrations
            .Select(pattern =>
            {
                var split = pattern.Split
                    (':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                return split.Length is not 2 ? (string.Empty, string.Empty) : (split[0], split[1]);
            })
            .ToArray<(string Type, string Status)>()
            .AsReadOnly();

        return new LocalMigrationValidator(ignoredMigrations,
                new RemoteMigrationValidator(ignoredMigrations,
                    new DistinctVersionMigrationValidator()));
    }
}
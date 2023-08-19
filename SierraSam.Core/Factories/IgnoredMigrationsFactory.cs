using System.Collections.Immutable;
using SierraSam.Core.Enums;
using static System.StringSplitOptions;

namespace SierraSam.Core.Factories;

public sealed class IgnoredMigrationsFactory : IIgnoredMigrationsFactory
{
    private readonly IConfiguration _configuration;

    public IgnoredMigrationsFactory(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public IReadOnlyCollection<(MigrationType Type, MigrationState State)> Create()
    {
        return _configuration.IgnoredMigrations
            .Select(pattern =>
            {
                var split = pattern.Split(':', 2, TrimEntries | RemoveEmptyEntries);

                if (split.Length != 2) return (MigrationType.None, MigrationState.None);

                if (Enum.TryParse<MigrationType>(split[0], out var type))
                {
                    throw new ArgumentException($"Invalid migration type: {split[0]}");
                }

                if (Enum.TryParse<MigrationState>(split[1], out var state))
                {
                    throw new ArgumentException($"Invalid migration state: {split[1]}");
                }

                return (type, state);
            })
            .ToImmutableArray();
    }
}
using SierraSam.Core.Enums;

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
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .Distinct()
            .Select(pattern => pattern[..].Replace("*", "any"))
            .Select(pattern =>
            {
                var split = pattern.Split(':', 2);

                if (split.Length is not 2)
                {
                    throw new ArgumentException($"Invalid ignore migration pattern `{pattern}`");
                }

                if (!Enum.TryParse<MigrationType>(split[0], ignoreCase: true, out var type))
                {
                    throw new ArgumentException($"Invalid migration type: {split[0]}");
                }

                if (!Enum.TryParse<MigrationState>(split[1], ignoreCase: true, out var state))
                {
                    throw new ArgumentException($"Invalid migration state: {split[1]}");
                }

                return (type, state);
            })
            .ToArray();
    }
}
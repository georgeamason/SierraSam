using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.MigrationSeekers;
using Spectre.Console;

namespace SierraSam.Capabilities;

public sealed class Repair(
    ILogger<Repair> logger,
    IMigrationSeeker migrationSeeker,
    IDatabase database,
    IAnsiConsole console,
    IConfiguration configuration,
    IMigrationRepairer repairer
) : ICapability
{
    private readonly ILogger<Repair> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Repair)} running");

        var discoveredMigrations = migrationSeeker.GetPendingMigrations().ToArray();
        var appliedMigrations = database.GetAppliedMigrations().ToArray();

        var repairs = new Dictionary<AppliedMigration, PendingMigration>();
        foreach (var discoveredMigration in discoveredMigrations)
        {
            var migrationToRepair = appliedMigrations.SingleOrDefault(
                appliedMigration =>
                {
                    // ReSharper disable once ConvertToLambdaExpression
                    return appliedMigration.Version == discoveredMigration.Version &&
                           (appliedMigration.Description != discoveredMigration.Description ||
                            appliedMigration.Checksum != discoveredMigration.Checksum);
                });

            if (migrationToRepair is null) continue;

            repairs.Add(migrationToRepair, discoveredMigration);

            console.MarkupLine(
                $"[red]{migrationToRepair.Description} ({migrationToRepair.Checksum})[/] => " +
                $"[green]{discoveredMigration.Description} ({discoveredMigration.Checksum})[/]"
            );
        }

        if (repairs.Count == 0)
        {
            console.MarkupLine(
                $"[green]\"{configuration.DefaultSchema}\".\"{configuration.SchemaTable}\" is synchronized[/]"
            );

            return;
        }

        repairer.Repair(repairs);

        console.MarkupLine(
            $"[green]Successfully synchronized \"{configuration.DefaultSchema}\".\"{configuration.SchemaTable}\"[/]"
        );
    }
}
using System.Data.Odbc;
using System.Text;
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.Enums;
using SierraSam.Core.MigrationSeekers;
using Spectre.Console;

namespace SierraSam.Capabilities;

internal sealed class Initialise : ICapability
{
    private readonly ILogger<Initialise> _logger;
    private readonly IDatabase _database;
    private readonly IConfiguration _configuration;
    private readonly IMigrationSeeker _migrationSeeker;
    private readonly IAnsiConsole _console;
    private readonly TimeProvider _timeProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public Initialise(
        ILogger<Initialise> logger,
        IDatabase database,
        IConfiguration configuration,
        IMigrationSeeker migrationSeeker,
        IAnsiConsole console,
        TimeProvider timeProvider
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _migrationSeeker = migrationSeeker ?? throw new ArgumentNullException(nameof(migrationSeeker));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Initialise)} running");

        if (_database.HasMigrationTable())
        {
            _console.MarkupLine(
                $"[yellow]Schema history table " +
                $"\"{_configuration.DefaultSchema}\".\"{_configuration.SchemaTable}\" " +
                $"already exists[/]"
            );

            return;
        }

        var transaction = _database.Connection.BeginTransaction();

        try
        {
            _database.CreateSchemaHistory(transaction: transaction);

            var sb = new StringBuilder();

            sb.Append($"Schema history table " +
                      $"\"{_configuration.DefaultSchema}\".\"{_configuration.SchemaTable}\"" +
                      $"created");

            if (!string.IsNullOrEmpty(_configuration.InitialiseVersion))
            {
                var filteredMigrations = _migrationSeeker
                    .GetPendingMigrations()
                    .Where(m => m.MigrationType is MigrationType.Versioned)
                    .Where(m =>
                        new VersionComparator(m.Version!).IsLessThanOrEqualTo(_configuration.InitialiseVersion));

                var i = 1;
                foreach (var filteredMigration in filteredMigrations)
                {
                    var migration = new AppliedMigration(
                        i++,
                        filteredMigration.Version,
                        filteredMigration.Description,
                        "SQL",
                        filteredMigration.FileName,
                        filteredMigration.Checksum,
                        _configuration.InstalledBy,
                        _timeProvider.GetUtcNow().UtcDateTime,
                        TimeSpan.Zero.TotalMilliseconds,
                        true);

                    _database.InsertSchemaHistory(migration, transaction);
                }

                sb.Clear();
                sb.Append($"Schema history table " +
                          $"\"{_configuration.DefaultSchema}\".\"{_configuration.SchemaTable}\"" +
                          $"initialised to version {_configuration.InitialiseVersion}");
            }

            transaction.Commit();

            _console.MarkupLine($"[green]{sb}[/]");
        }
        catch (OdbcException)
        {
            transaction.Rollback();

            throw;
        }
    }
}
using Microsoft.Extensions.Logging;
using SierraSam.Core;

namespace SierraSam.Capabilities;

internal sealed class Initialise : ICapability
{
    private readonly ILogger<Initialise> _logger;
    private readonly IDatabase _database;
    private readonly IConfiguration _configuration;

    public Initialise(ILogger<Initialise> logger, IDatabase database, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Initialise)} running");

        if (_database.HasMigrationTable)
        {
            ColorConsole.WarningLine($"Schema history table " +
                                     $"\"{_configuration.DefaultSchema}\".\"{_configuration.SchemaTable}\" " +
                                     $"already exists");

            return;
        }

        _database.CreateSchemaHistory();

        ColorConsole.SuccessLine($"Schema history table " +
                                 $"\"{_configuration.DefaultSchema}\".\"{_configuration.SchemaTable}\" " +
                                 $"created");
    }
}
using Microsoft.Extensions.Logging;
using SierraSam.Core;
using SierraSam.Core.Enums;

namespace SierraSam.Capabilities;

internal sealed class Rollup : ICapability
{
    private readonly ILogger<Rollup> _logger;
    private readonly IDatabase _database;
    private readonly IConfiguration _configuration;

    public Rollup(
        ILogger<Rollup> logger,
        IDatabase database,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    public Task Run(string[] args)
    {
        _logger.LogTrace($"{nameof(Rollup)} running");

        // TODO: Join all versioned migrations up into 1 single migration up to specified version
        return Task.CompletedTask;
    }
}
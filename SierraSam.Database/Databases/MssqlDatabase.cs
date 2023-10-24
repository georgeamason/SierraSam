using System.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

public sealed class MssqlDatabase : DefaultDatabase
{
    private readonly ILogger<MssqlDatabase> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDbExecutor _dbExecutor;

    public MssqlDatabase(
        ILogger<MssqlDatabase> logger,
        IDbConnection connection,
        IDbExecutor executor,
        IConfiguration configuration,
        IMemoryCache cache)
        : base(logger, connection, executor, configuration, cache)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));

        _dbExecutor = executor
            ?? throw new ArgumentNullException(nameof(executor));

        _configuration.DefaultSchema ??= this.DefaultSchema;
    }

    public override string Provider => "MSSQL";

    public override string ServerVersion =>
        _dbExecutor.ExecuteScalar<string>("SELECT SERVERPROPERTY('productversion')")!;

    public override string DefaultSchema =>
        _dbExecutor.ExecuteScalar<string>("SELECT SCHEMA_NAME()")!;
}
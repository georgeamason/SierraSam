using System.Data;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

public sealed class MssqlDatabase : DefaultDatabase
{
    private readonly IConfiguration _configuration;
    private readonly IDbExecutor _dbExecutor;

    public MssqlDatabase(
        IDbConnection connection,
        IDbExecutor executor,
        IConfiguration configuration)
        : base(connection, executor, configuration)
    {
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
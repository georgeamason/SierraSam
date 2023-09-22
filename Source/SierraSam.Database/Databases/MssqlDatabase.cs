using System.Data.Odbc;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

public class MssqlDatabase : DefaultDatabase
{
    private readonly IConfiguration _configuration;

    public MssqlDatabase(OdbcConnection odbcConnection, IConfiguration configuration)
        : base(odbcConnection, configuration)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public override string Name => "Microsoft SQL Server";
}
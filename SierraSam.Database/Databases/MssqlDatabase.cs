using System.Data.Odbc;
using SierraSam.Core;

namespace SierraSam.Database.Databases;

public class MssqlDatabase : Database
{
    private readonly Configuration _configuration;

    public MssqlDatabase(OdbcConnection odbcConnection, Configuration configuration)
        : base(odbcConnection, configuration)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public override string Name => "Microsoft SQL Server";
}
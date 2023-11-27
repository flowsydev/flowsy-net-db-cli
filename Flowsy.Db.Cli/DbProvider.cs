namespace Flowsy.Db.Cli;

public static class DbProvider
{
    public const string PostgreSql = "Npgsql";
    public const string MySql = "MySql.Data.MySqlClient";
    public const string Oracle = "Oracle.ManagedDataAccess.Client";
    public const string MsSql = "System.Data.SqlClient";
    public const string IbmDb2 = "IBM.Data.DB2";
    public const string Firebird = "FirebirdSql.Data.FirebirdClient";

    public static readonly string[] Names =
    {
        PostgreSql,
        MySql,
        Oracle,
        MsSql,
        IbmDb2,
        Firebird
    };
    
    public static bool IsSupported(string providerInvariantName)
        => Names.Contains(providerInvariantName);
    
    public static int GetDefaultPort(string providerInvariantName)
        => providerInvariantName switch
        {
            PostgreSql => 5432,
            MySql => 3306,
            Oracle => 1521,
            MsSql => 1433,
            IbmDb2 => 50000,
            Firebird => 3050,
            _ => 0
        };
    
    public static string? GetDefaultDatabase(string providerInvariantName)
        => providerInvariantName switch
        {
            PostgreSql => "postgres",
            MySql => "mysql",
            Oracle => "ORCL",
            MsSql => "master",
            _ => null
        };
    
    public static string? GetDefaultUser(string providerInvariantName)
        => providerInvariantName switch
        {
            PostgreSql => "postgres",
            MySql => "root",
            Oracle => "SYSTEM",
            MsSql => "sa",
            Firebird => "SYSDBA",
            _ => null
        };
    
    public static string BuildConnectionString(
        string host,
        int port,
        string? database,
        string user,
        string password,
        string? additionalOptions = null
    )
    {
        var strings = new List<string>();
        
        if (!string.IsNullOrEmpty(host))
            strings.Add($"Server={host}");
        
        if (port > 0)
            strings.Add($"Port={port}");
        
        if (!string.IsNullOrEmpty(database))
            strings.Add($"Database={database}");
        
        if (!string.IsNullOrEmpty(user))
            strings.Add($"User Id={user}");
        
        if (!string.IsNullOrEmpty(password))
            strings.Add($"Password={password}");
        
        if (!string.IsNullOrEmpty(additionalOptions))
            strings.Add(additionalOptions);

        return string.Join(";", strings);
    }
}
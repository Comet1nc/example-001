using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool;

public class DatabaseBuilder
{
    private readonly IEnumerable<string> _scriptsPath;
    private readonly string _dbPath;
    
    public DatabaseBuilder(string databaseDirectory, string scriptsDirectory)
    {
        _dbPath = Path.Combine(databaseDirectory, "database.fdb");
        _scriptsPath = SqlScriptsUtil.GetScriptsPathFromDirectory(scriptsDirectory);
    }
    
    public void BuildDatabase()
    {
        string connectionString = CreateDatabase();
        var runner = new FirebirdScriptRunner(connectionString, _scriptsPath);
        runner.ExecuteScripts();
    }

    private string CreateDatabase()
    {
        var csb = new FbConnectionStringBuilder
        {
            DataSource = "localhost",
            Database = _dbPath,
            UserID = "SYSDBA",
            Password = "masterkey",
            ServerType = FbServerType.Default,
            Charset = "UTF8",
            Pooling = true
        };
        
        FbConnection.CreateDatabase(csb.ToString());
        Console.WriteLine($"Utworzono nową bazę danych: {_dbPath}");

        return csb.ToString();
    }
}

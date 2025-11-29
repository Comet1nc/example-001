using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool;

public class DatabaseBuilder
{
    private readonly IEnumerable<string> _scriptsPath;
    private readonly string _dbPath;
    
    public DatabaseBuilder(string databaseDirectory, string scriptsDirectory)
    {
        _dbPath = Path.Combine(databaseDirectory, "database.fdb");
        _scriptsPath = GetScriptsPathFromDirectory(scriptsDirectory);
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
            Password = "082005",
            ServerType = FbServerType.Default, // Wymaga zainstalowanego serwera Firebird
            Charset = "UTF8",
            Pooling = true
        };
        
        FbConnection.CreateDatabase(csb.ToString());
        Console.WriteLine($"Utworzono nową bazę danych: {_dbPath}");

        return csb.ToString();
    }

    private IEnumerable<string> GetScriptsPathFromDirectory(string directory)
    {
        var files = Directory.GetFiles(directory, "*.sql")
            .OrderBy(f => f)
            .ToList();

        if (files.Count == 0)
        {
            throw new FileNotFoundException("Brak skryptów .sql do wykonania.");
        }

        return files;
    }
}

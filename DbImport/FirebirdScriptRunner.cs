using System.Reflection;
using System.Text.RegularExpressions;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;

namespace DbMetaTool;

public class FirebirdScriptRunner
{
    private readonly string _connectionString;
    private readonly IEnumerable<string> _scriptsPath;
    private readonly Regex _ddlRegex;

    public FirebirdScriptRunner(string connectionString, IEnumerable<string> scriptsPath)
    {
        _connectionString = connectionString;
        _scriptsPath = scriptsPath;
        _ddlRegex = new Regex(
            @"^\s*(?:CREATE|ALTER|RECREATE|DROP)(?:\s+OR\s+ALTER)?\s+(?:DOMAIN|TABLE|PROCEDURE)\b", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline
        );
    }
    
    public void ExecuteScripts()    
    {
        using var connection = new FbConnection(_connectionString);
        connection.Open();
        var batch = new FbBatchExecution(connection);
        foreach (string scriptPath in _scriptsPath)
        {
            string scriptContent = File.ReadAllText(scriptPath);
            var fbScript = new FbScript(scriptContent);
            fbScript.Parse();
            if (ShouldExecuteScript(fbScript))
            {
                batch.AppendSqlStatements(fbScript);
            }
            else
            {
                Console.WriteLine($"Pominięto skrypt. Wspierane są tylko domeny, tabele, procedury:" +
                                  $" {Path.GetFileName(scriptPath)}");
            }
        }
        batch.Execute();
        Console.WriteLine($"Wszystkie skrypty zostały wykonane pomyślnie.");
    }
    
    private bool ShouldExecuteScript(FbScript script)
    {
        foreach (var statement in script.Results)
        {
            PropertyInfo cleanTextProp = typeof(FbStatement).GetProperty("CleanText", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            string cleanSql = (string)cleanTextProp.GetValue(statement);
            if (string.IsNullOrWhiteSpace(cleanSql) ||  !_ddlRegex.IsMatch(cleanSql))
            {
                return false;
            }
        }
        return true;
    }
}
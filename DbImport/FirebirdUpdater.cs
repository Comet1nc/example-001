using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;

namespace DbMetaTool;

public class FirebirdUpdater
{
    private readonly string _connectionString;
    private readonly IEnumerable<string> _scriptsPath;
    private readonly Regex _ddlRegex;
    private static readonly PropertyInfo? CleanTextProp = typeof(FbStatement)
        .GetProperty("CleanText", BindingFlags.NonPublic | BindingFlags.Instance);
    
    public FirebirdUpdater(string connectionString, IEnumerable<string> scriptsPath)
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
        var sortedSql = GetSortedSql();
        var sql = MergeSqlList(sortedSql);
        var mergedScript = new FbScript(sql);
        mergedScript.Parse();

        using var connection = new FbConnection(_connectionString);
        connection.Open();
        var batch = new FbBatchExecution(connection);
        batch.AppendSqlStatements(mergedScript);
        batch.Execute();
    }

    private IEnumerable<string> GetSortedSql()
    {
        return _scriptsPath
            .Where(File.Exists)
            .SelectMany(path => 
            {
                var script = new FbScript(File.ReadAllText(path));
                script.Parse();
                return script.Results.Cast<FbStatement>();
            })
            .Select(stmt => (string)CleanTextProp.GetValue(stmt))
            .Where(sql => !string.IsNullOrWhiteSpace(sql) && _ddlRegex.IsMatch(sql))
            .Select(sql => new
            {
                Sql = sql,
                Priority = Regex.IsMatch(sql, @"^\s*CREATE\s+DOMAIN", RegexOptions.IgnoreCase) ? 1 :
                    Regex.IsMatch(sql, @"^\s*(RE)?CREATE\s+TABLE", RegexOptions.IgnoreCase) ? 2 : 3
            })
            .OrderBy(x => x.Priority)
            .Select(x => x.Sql);
    }

    private string MergeSqlList(IEnumerable<string> sortedSql)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SET TERM ^ ;");
        foreach (var sql in sortedSql)
        {
            sb.Append(sql);
            sb.AppendLine("^");
        }
        sb.AppendLine("SET TERM ; ^");

        return sb.ToString();
    }
}
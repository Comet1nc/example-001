using System.Data;
using System.Text;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool;

public class DatabaseSchemaExporter
{
    private readonly string _connectionString;
    private readonly string _outputDirectory;
    private readonly StringBuilder _scriptBuilder;

    public DatabaseSchemaExporter(string connectionString, string outputDirectory)
    {
        _connectionString = connectionString;
        _outputDirectory = outputDirectory;
        _scriptBuilder = new StringBuilder();
    }

    public void Export()
    {
        var script = GenerateFullSchemaScript();
        Save(script);
    }

    private void Save(string script)
    {
        var fileName = $"script_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
        var fullPath = Path.Combine(_outputDirectory, fileName);
        File.WriteAllText(fullPath, script, Encoding.UTF8);
    }

    public string GenerateFullSchemaScript()
    {
        _scriptBuilder.AppendLine("/* Firebird Database Schema Dump */");
        _scriptBuilder.AppendLine($"/* Generated: {DateTime.Now} */");
        _scriptBuilder.AppendLine("SET SQL DIALECT 3;");
        _scriptBuilder.AppendLine();
        using (var conn = new FbConnection(_connectionString))
        {
            conn.Open();
            var domainsScripts = new DomainsSchemaExporter(conn).Export();
            var tablesScripts = new TablesSchemaExporter(conn).Export();
            var proceduresScripts = new ProceduresSchemaExporter(conn).Export();

            _scriptBuilder.Append(domainsScripts);
            _scriptBuilder.Append(tablesScripts);
            _scriptBuilder.Append(proceduresScripts);
        }

        return _scriptBuilder.ToString();
    }

    internal static string GetDataType(IDataRecord row)
    {
        var type = Convert.ToInt32(row["RDB$FIELD_TYPE"]);
        var subType = row["RDB$FIELD_SUB_TYPE"] == DBNull.Value ? 0 : Convert.ToInt32(row["RDB$FIELD_SUB_TYPE"]);
        var length = Convert.ToInt32(row["RDB$FIELD_LENGTH"]);
        var scale = row["RDB$FIELD_SCALE"] == DBNull.Value ? 0 : Convert.ToInt32(row["RDB$FIELD_SCALE"]);
        var charLen = row["RDB$CHARACTER_LENGTH"] == DBNull.Value ? 0 : Convert.ToInt32(row["RDB$CHARACTER_LENGTH"]);

        switch (type)
        {
            case 7: // SMALLINT
                return subType == 1 || subType == 2 ? $"NUMERIC(4, {-scale})" : "SMALLINT";
            case 8: // INTEGER
                return subType == 1 || subType == 2 ? $"NUMERIC(9, {-scale})" : "INTEGER";
            case 10: return "FLOAT";
            case 12: return "DATE";
            case 13: return "TIME";
            case 14: return $"CHAR({charLen})";
            case 16: // BIGINT
                return subType == 1 || subType == 2 ? $"NUMERIC(18, {-scale})" : "BIGINT";
            case 27: return "DOUBLE PRECISION";
            case 35: return "TIMESTAMP";
            case 37: return $"VARCHAR({charLen})";
            case 261: // BLOB
                return subType == 1 ? "BLOB SUB_TYPE TEXT" : "BLOB SUB_TYPE BINARY";
            default: return "UNKNOWN_TYPE";
        }
    }
}
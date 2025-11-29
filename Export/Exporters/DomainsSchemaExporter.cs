using System.Text;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool;

public class DomainsSchemaExporter : DbEntitySchemaExporter
{
    public DomainsSchemaExporter(FbConnection fbConn) : base(fbConn)
    {
    }

    public override StringBuilder Export()
    {
        ScriptBuilder.AppendLine("/* --- DOMAINS --- */");
        var query = @"
                SELECT RDB$FIELD_NAME, RDB$FIELD_TYPE, RDB$FIELD_LENGTH, RDB$FIELD_SCALE, 
                       RDB$FIELD_SUB_TYPE, RDB$CHARACTER_LENGTH, RDB$DEFAULT_SOURCE, RDB$VALIDATION_SOURCE
                FROM RDB$FIELDS 
                WHERE RDB$SYSTEM_FLAG = 0 AND RDB$FIELD_NAME NOT LIKE 'RDB$%'";

        using var cmd = new FbCommand(query, FbConn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var name = reader["RDB$FIELD_NAME"].ToString().Trim();
            if (name.StartsWith("RDB$")) continue;

            var typeDef = DatabaseSchemaExporter.GetDataType(reader);
            var defaultSrc = reader["RDB$DEFAULT_SOURCE"] as string;
            var checkSrc = reader["RDB$VALIDATION_SOURCE"] as string;

            ScriptBuilder.Append($"CREATE DOMAIN {name} AS {typeDef}");
            if (!string.IsNullOrWhiteSpace(defaultSrc)) ScriptBuilder.Append($" {defaultSrc.Trim()}");
            if (!string.IsNullOrWhiteSpace(checkSrc)) ScriptBuilder.Append($" {checkSrc.Trim()}");
            ScriptBuilder.AppendLine(";");
        }

        ScriptBuilder.AppendLine();

        return ScriptBuilder;
    }
}
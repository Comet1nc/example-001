using System.Text;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool;

public class TablesSchemaExporter : DbEntitySchemaExporter
{
    public TablesSchemaExporter(FbConnection fbConn) : base(fbConn)
    {
    }

    public override StringBuilder Export()
    {
        ScriptBuilder.AppendLine("/* --- TABLES --- */");
        var tables = new List<string>();
        var tableQuery =
            "SELECT RDB$RELATION_NAME FROM RDB$RELATIONS WHERE RDB$SYSTEM_FLAG = 0 AND RDB$VIEW_BLR IS NULL ORDER BY RDB$RELATION_NAME";

        using (var cmd = new FbCommand(tableQuery, FbConn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read()) tables.Add(reader.GetString(0).Trim());
        }

        foreach (var table in tables)
        {
            ScriptBuilder.AppendLine($"CREATE TABLE {table} (");

            var colQuery = @"
                    SELECT RF.RDB$FIELD_NAME, F.RDB$FIELD_TYPE, F.RDB$FIELD_LENGTH, F.RDB$FIELD_SCALE, 
                           F.RDB$FIELD_SUB_TYPE, F.RDB$CHARACTER_LENGTH, RF.RDB$NULL_FLAG, RF.RDB$DEFAULT_SOURCE, RF.RDB$FIELD_SOURCE
                    FROM RDB$RELATION_FIELDS RF
                    JOIN RDB$FIELDS F ON RF.RDB$FIELD_SOURCE = F.RDB$FIELD_NAME
                    WHERE RF.RDB$RELATION_NAME = @TableName
                    ORDER BY RF.RDB$FIELD_POSITION";

            using var cmd = new FbCommand(colQuery, FbConn);
            cmd.Parameters.AddWithValue("@TableName", table);
            using var reader = cmd.ExecuteReader();

            var columns = new List<string>();
            while (reader.Read())
            {
                var colName = reader["RDB$FIELD_NAME"].ToString()?.Trim();
                var domainName = reader["RDB$FIELD_SOURCE"].ToString()?.Trim();
                var notNull = reader["RDB$NULL_FLAG"] != DBNull.Value && Convert.ToInt32(reader["RDB$NULL_FLAG"]) == 1;

                var typeDef = domainName;
                if (domainName != null && domainName.StartsWith("RDB$"))
                    typeDef = DatabaseSchemaExporter.GetDataType(reader);

                var line = $"    {colName} {typeDef}";
                if (notNull) line += " NOT NULL";

                columns.Add(line);
            }

            ScriptBuilder.AppendLine(string.Join(",\n", columns));
            ScriptBuilder.AppendLine(");");
            ScriptBuilder.AppendLine();
        }

        return ScriptBuilder;
    }
}
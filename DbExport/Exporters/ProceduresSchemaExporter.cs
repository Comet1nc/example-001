using System.Text;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool;

public class ProceduresSchemaExporter : DbEntitySchemaExporter
{
    public ProceduresSchemaExporter(FbConnection fbConn) : base(fbConn)
    {
    }

    public override StringBuilder Export()
    {
        ScriptBuilder.AppendLine("/* --- PROCEDURES --- */");
        var procQuery = "SELECT RDB$PROCEDURE_NAME, RDB$PROCEDURE_SOURCE FROM RDB$PROCEDURES WHERE RDB$SYSTEM_FLAG = 0";

        using var cmd = new FbCommand(procQuery, FbConn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var procName = reader["RDB$PROCEDURE_NAME"].ToString().Trim();
            var source = reader["RDB$PROCEDURE_SOURCE"].ToString();

            var paramsQuery = @"
                    SELECT RDB$PARAMETER_NAME, RDB$PARAMETER_TYPE, RDB$FIELD_SOURCE 
                    FROM RDB$PROCEDURE_PARAMETERS 
                    WHERE RDB$PROCEDURE_NAME = @ProcName 
                    ORDER BY RDB$PARAMETER_TYPE, RDB$PARAMETER_NUMBER";

            var inputs = new List<string>();
            var outputs = new List<string>();

            using (var pCmd = new FbCommand(paramsQuery, FbConn))
            {
                pCmd.Parameters.AddWithValue("@ProcName", procName);
                using var pReader = pCmd.ExecuteReader();
                while (pReader.Read())
                {
                    var pName = pReader["RDB$PARAMETER_NAME"].ToString().Trim();
                    var pType = Convert.ToInt32(pReader["RDB$PARAMETER_TYPE"]);
                    var pDomain = pReader["RDB$FIELD_SOURCE"].ToString().Trim();
                    if (pType == 0) inputs.Add($"{pName} {pDomain}");
                    else outputs.Add($"{pName} {pDomain}");
                }
            }

            ScriptBuilder.Append("SET TERM ^ ;\n");
            ScriptBuilder.Append($"CREATE PROCEDURE {procName}");

            if (inputs.Count > 0) ScriptBuilder.Append($" (\n    {string.Join(",\n    ", inputs)}\n)");

            if (outputs.Count > 0) ScriptBuilder.Append($"\nRETURNS (\n    {string.Join(",\n    ", outputs)}\n)");

            ScriptBuilder.Append("\nAS\n");
            ScriptBuilder.Append(source);
            ScriptBuilder.AppendLine("^");
            ScriptBuilder.AppendLine("SET TERM ; ^");
            ScriptBuilder.AppendLine();
        }

        return ScriptBuilder;
    }
}
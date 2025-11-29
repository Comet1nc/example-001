using System.Text;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool;

public abstract class DbEntitySchemaExporter
{
    protected readonly FbConnection FbConn;
    protected readonly StringBuilder ScriptBuilder;

    protected DbEntitySchemaExporter(FbConnection fbConn)
    {
        FbConn = fbConn;
        ScriptBuilder = new StringBuilder();
    }

    public abstract StringBuilder Export();
}
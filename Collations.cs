using System;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Collections.Specialized;

class ScriptDMO {
    static void Main(string[] args) {
        ServerConnection sc;
        Server server;

        sc = new ServerConnection("host", "username", "password");
        server = new Server(sc);
        server.ConnectionContext.Connect();

        Database db = server.Databases[server.ConnectionContext.CurrentDatabase];
        //Database db = server.Databases["database"];

        Scripter scr = new Scripter(server);
        scr.Options.DriAllConstraints = true;
        scr.Options.DriIncludeSystemNames = true;
        scr.Options.IncludeIfNotExists = true;

        //Altera COLLATION
        string collation = db.Collation;
        foreach (Table t in db.Tables) {
            string scrtbl = null;
            foreach (Column i in t.Columns) {
                if (i.Collation == db.Collation) {
                    if (scrtbl == null) {
                        StringBuilder sb = new StringBuilder();
                        foreach (var j in scr.Script(new UrnCollection() { t.Urn })) {
                            sb.AppendLine(j);
                        }
                        scrtbl = sb.ToString();
                    }
                    string script = scrtbl.Split('\r').First(f => f.Contains("[" + i.Name + "]"));
                    script = script.TrimStart(new char[] { '\r', '\n', '\t' }).TrimEnd(',');
                    script = script.Substring(script.IndexOf(' '));
                    script = script.Replace(i.Collation, "database_default");
                    Console.WriteLine("ALTER TABLE ["+t.Schema+"].["+t.Name+"] ALTER COLUMN ["+i.Name+"] "+script+";");
                    Console.WriteLine("GO");
                }
            }
        }
    }
}

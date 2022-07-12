using System;
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

        // Cria DROPs
        scr.Options.ScriptDrops = true;
        foreach (Table t in db.Tables) {
            foreach (ForeignKey i in t.ForeignKeys) {
                Salva(scr.Script(new UrnCollection() { i.Urn }));
            }
        }
        foreach (Table t in db.Tables) {
            foreach (Check i in t.Checks) {
                Salva(scr.Script(new UrnCollection() { i.Urn }));
            }
            foreach (Index i in t.Indexes) {
                Salva(scr.Script(new UrnCollection() { i.Urn }));
            }
        }
    }

    static void Salva(StringCollection c) {
        StringBuilder sb = new StringBuilder();
        foreach (var i in c) {
            sb.AppendLine(i);
        }
        sb.AppendLine("GO");
        Console.Write(sb.ToString());
    }
}

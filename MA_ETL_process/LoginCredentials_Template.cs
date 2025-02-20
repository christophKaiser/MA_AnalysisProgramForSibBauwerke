using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MA_ETL_process
{
    // ToDo: add description how to change this template to be used inside the application's scope

    internal static class LoginCredentials_Template
    {
        public static string SqlConnectionString =
            "Data Source=<addressToServer>;Initial Catalog=<nameOfDatabase>;" +
            "Integrated Security=True;Encrypt=False;Trust Server Certificate=True";

        public static string Neo4jUri = "bolt://localhost:<9999>";
        public static string Neo4jUser = "<user>";
        public static string Neo4jPassword = "<password>";
        public static string Neo4jDatabase = "<database>";
    }
}

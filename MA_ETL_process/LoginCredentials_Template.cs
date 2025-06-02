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

        // Path where the DBMS is stored which can be obtained in neo4j Desktop on the DBMS > Settings > Open folder > DBMS,
        // check / adjust the drive too - "C:" is just for illustration here
        public static string Neo4jBatFilePath = @"C:\\<DBMS-path>\\bin\\neo4j.bat";

        public static string Neo4jUri = "bolt://localhost:<9999>";
        public static string Neo4jUser = "<user>";
        public static string Neo4jPassword = "<password>";
        public static string Neo4jDatabase = "<database>";

        public static string CsvSchadentypPath = @"C:\\<path>\\<filename>.csv";
        // csv of the SCHADEN (aka Schadentyp) must be in the format
        // nr;text;drk_text;;nr_withZeros;Level;Text-Baum
    }
}

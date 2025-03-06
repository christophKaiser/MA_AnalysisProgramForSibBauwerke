using System.Text;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MA_ETL_process
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SqlClient? sqlClient = null;
        Neo4jDriver? neo4jDriver = null;

        public MainWindow()
        {
            InitializeComponent();
            Utilities.SetTxtConsoleReference = mainWindow.txtConsole;
            Utilities.ConsoleLog("Click a button");
        }

        private void btn_SqlConnection_Click(object sender, RoutedEventArgs e)
        {
            // create and open a new connection to the SQL Server
            sqlClient = new SqlClient();
        }

        private void btn_SqlTest_Click(object sender, RoutedEventArgs e)
        {
            if (sqlClient == null)
            {
                Utilities.ConsoleLog("no SQL connection");
                return;
            }

            Utilities.ConsoleLog("\nBauwerk:");
            sqlClient.SelectRows(
                @"SELECT [BWNR], [BWNAME], [ORT], [ANZ_TEILBW], [LAENGE_BR]
                FROM [SIB_BAUWERKE_19_20230427].[dbo].[GES_BW]
                WHERE [SIB_BAUWERKE_19_20230427].[dbo].[GES_BW].[BWNR]=5527701");

            Utilities.ConsoleLog("\nTeilbauwerke:");
            sqlClient.SelectRows(
                @"SELECT [BWNR], [TEIL_BWNR], [TW_NAME], [KONSTRUKT], [ID_NR]
                FROM [SIB_BAUWERKE_19_20230427].[dbo].[TEIL_BW]
                WHERE [SIB_BAUWERKE_19_20230427].[dbo].[TEIL_BW].[BWNR]=5527701");

            Utilities.ConsoleLog("\nDurchgeführte Prüfungen:");
            sqlClient.SelectRows(
                @"SELECT [BWNR], [TEIL_BWNR], [ID_NR], [PRUF_DATUM], [PRUF_ZYKL], [ZUSTANDSN], [PRUF_NR], [IDENT]
                FROM [SIB_BAUWERKE_19_20230427].[dbo].[PRUF_DGF]
                WHERE [SIB_BAUWERKE_19_20230427].[dbo].[PRUF_DGF].[BWNR]=5527701");

            Utilities.ConsoleLog("'Test SQL Connection' finished");
        }

        private void btn_Neo4jConnection_Click(object sender, RoutedEventArgs e)
        {
            // start neo4j server
            //System.Diagnostics.Process.Start(@"D:\\Christoph\\.Neo4jDesktop\\relate-data\\dbmss\\dbms-bd74cc49-cd57-461f-896f-a5a17a27ca1c\\bin\\neo4j.bat", "start").WaitForExit();

            // creating a new driver to Neo4j, the server (DBMS) must be started inside Neo4j Desktop beforehand manually
            neo4jDriver = new Neo4jDriver();

            // stop neo4j server
            //System.Diagnostics.Process.Start(@"D:\\Christoph\\.Neo4jDesktop\\relate-data\\dbmss\\dbms-bd74cc49-cd57-461f-896f-a5a17a27ca1c\\bin\\neo4j.bat", "stop").WaitForExit();
        }

        private void btn_Neo4jTest_Click(object sender, RoutedEventArgs e)
        {
            if (neo4jDriver == null)
            {
                Utilities.ConsoleLog("no Neo4j connection");
                return;
            }

            // call test function which creates a node in the Neo4j database
            neo4jDriver.PrintGreeting("hello world", 42, "42");
        }

        private void btn_CreateConstraints_Click(object sender, RoutedEventArgs e)
        {
            if (neo4jDriver == null)
            {
                Utilities.ConsoleLog("no Neo4j connection");
                return;
            }

            List<string> sibBw_labels = [];
            sibBw_labels.Add(new SibBW_GES_BW().label);
            sibBw_labels.Add(new SibBW_TEIL_BW().label);
            SibBw sibBw_dummy = new SibBw();

            foreach (string label in sibBw_labels)
            {
                string cypherString = sibBw_dummy.GetCypherConstraintKey(label);
                Utilities.ConsoleLog(cypherString);
                neo4jDriver.ExecuteCypherQuery(cypherString);
                // neo4j requires the "CREATE CONSTRAINTS" to be single statements in _session.Run(..)
            }

            Utilities.ConsoleLog("created constriants");
        }

        private void btn_FirstTriple_Click(object sender, RoutedEventArgs e)
        {
            if (sqlClient == null)
            {
                Utilities.ConsoleLog("no SQL connection");
                return;
            }
            if (neo4jDriver == null)
            {
                Utilities.ConsoleLog("no Neo4j connection");
                return;
            }

            Utilities.ConsoleLog("\nBauwerk:");
            List<SibBW_GES_BW> BWs = sqlClient.SelectRows<SibBW_GES_BW>(
                @"SELECT [BWNR], [BWNAME], [ORT], [ANZ_TEILBW], [LAENGE_BR]
                FROM [SIB_BAUWERKE_19_20230427].[dbo].[GES_BW]
                WHERE [SIB_BAUWERKE_19_20230427].[dbo].[GES_BW].[BWNR]=5527701");

            Utilities.ConsoleLog("\nTeilbauwerke");
            foreach(SibBW_GES_BW bw in BWs)
            {
                bw.teilbauwerke = sqlClient.SelectRows<SibBW_TEIL_BW>(
                    $@"SELECT [BWNR], [TEIL_BWNR], [TW_NAME], [KONSTRUKT], [ID_NR]
                    FROM [SIB_BAUWERKE_19_20230427].[dbo].[TEIL_BW]
                    WHERE [SIB_BAUWERKE_19_20230427].[dbo].[TEIL_BW].[BWNR]={bw.stringValues["BWNR"]}");
            }

            //Utilities.ConsoleLog("\n\nCyher-string Bauwerk:");
            //Utilities.ConsoleLog(BWs[0].GetCypherCreate());

            //Utilities.ConsoleLog("\nCypher-string Teilbauwerke:");
            //foreach (SibBW_TEIL_BW teilBw in BWs[0].teilbauwerke)
            //{
            //    Utilities.ConsoleLog(teilBw.GetCypherCreate());
            //}

            string cypherString = BWs[0].GetCypherCreateMerge_BW_TeilBWs();
            Utilities.ConsoleLog("\n\nKombinierter Cypher-string für erste Tripel:");
            Utilities.ConsoleLog(cypherString);
            neo4jDriver.ExecuteCypherQuery(cypherString);
            Utilities.ConsoleLog("First triple created!");
        }

        private void btn_CreateAllBridges_Click(object sender, RoutedEventArgs e)
        {
            if (sqlClient == null)
            {
                Utilities.ConsoleLog("no SQL connection");
                return;
            }
            if (neo4jDriver == null)
            {
                Utilities.ConsoleLog("no Neo4j connection");
                return;
            }

            List<string> bridgeNumbers = sqlClient.SelectRowsOneColumn("BRUECKE", "BWNR");
            // bridgeNumbers.Count(): 20349

            // remove dublicates in the list (because one entry for each Teilbauwerk)
            bridgeNumbers = bridgeNumbers.Distinct().ToList();
            // bridgeNumbers.Count(): 17504

            Utilities.ConsoleLog("\nBauwerke:");
            List<SibBW_GES_BW> BWs = sqlClient.SelectRows<SibBW_GES_BW>(
                // ... SELECT TOP (100) [BWNR], ...
                $@"SELECT TOP (100) [BWNR], [BWNAME], [ORT], [ANZ_TEILBW], [LAENGE_BR]
                FROM [SIB_BAUWERKE_19_20230427].[dbo].[GES_BW]
                WHERE [SIB_BAUWERKE_19_20230427].[dbo].[GES_BW].[BWNR]
                IN ('{String.Join("', '", bridgeNumbers)}')");

            Utilities.ConsoleLog("\nTeilbauwerke");
            foreach (SibBW_GES_BW bw in BWs)
            {
                bw.teilbauwerke = sqlClient.SelectRows<SibBW_TEIL_BW>(
                    $@"SELECT [BWNR], [TEIL_BWNR], [TW_NAME], [KONSTRUKT], [ID_NR]
                    FROM [SIB_BAUWERKE_19_20230427].[dbo].[TEIL_BW]
                    WHERE [SIB_BAUWERKE_19_20230427].[dbo].[TEIL_BW].[BWNR]={bw.stringValues["BWNR"]}");
            }

            btn_CreateConstraints_Click(sender, e);

            foreach (SibBW_GES_BW BW in BWs)
            {
                neo4jDriver.ExecuteCypherQuery(BW.GetCypherCreateMerge_BW_TeilBWs());
            }
            Utilities.ConsoleLog("created neo4j nodes and relationships");

            Utilities.ConsoleLog("'Create all bridges' finished");
        }

        private void btn_Neo4jDeleteNodes_Click(object sender, RoutedEventArgs e)
        {
            if (neo4jDriver == null)
            {
                Utilities.ConsoleLog("no Neo4j connection");
                return;
            }

            neo4jDriver.DeleteAllNodesInDatabase();
        }

        private void btn_Neo4jDeleteConstraints_Click(object sender, RoutedEventArgs e)
        {
            if (neo4jDriver == null)
            {
                Utilities.ConsoleLog("no Neo4j connection");
                return;
            }

            neo4jDriver.DeleteAllConstraintsInDatabase();
        }
    }
}

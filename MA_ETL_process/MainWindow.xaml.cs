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
using System.Diagnostics;
using System.Windows.Threading;

namespace MA_ETL_process
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SqlClient? sqlClient = null;
        Neo4jDriver? neo4jDriver = null;
        int queryMaxLength = 100000;
        string query = "";

        public MainWindow()
        {
            InitializeComponent();
            Utilities.SetTxtConsoleReference = mainWindow.txtConsole;
            Utilities.ConsoleLog("Click a button");
        }

        private void buttonsSwitchClickableTo(bool stage)
        {
            foreach (Button button in buttonsList(mainWindow))
            {
                button.IsEnabled = stage;
            }
        }

        private void btn_SqlConnection_Click(object sender, RoutedEventArgs e)
        {
            // create and open a new connection to the SQL Server
            sqlClient = new SqlClient();
        }

        private void btn_Neo4jConnection_Click(object sender, RoutedEventArgs e)
        {
            // start neo4j server
            //System.Diagnostics.Process.Start(LoginCredentials.Neo4jBatFilePath, "start").WaitForExit();

            // creating a new driver to Neo4j, the server (DBMS) must be started inside Neo4j Desktop beforehand manually
            neo4jDriver = new Neo4jDriver();

            // stop neo4j server
            //System.Diagnostics.Process.Start(LoginCredentials.Neo4jBatFilePath, "stop").WaitForExit();
        }

        private async void btn_CreateAllBridges_Click(object sender, RoutedEventArgs e)
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

            buttonsSwitchClickableTo(false);

            List<string> bridgeNumbers = getBridgeNumbers();
            createConstraints();

            // start new thread beside the UI-thread (which the button would use)
            Task task = Task.Run(() =>
            {
                Stopwatch sw = Stopwatch.StartNew();

                foreach (string bridgeNumber in bridgeNumbers)
                {
                    extractAndLoadBridge(bridgeNumber);
                }

                sw.Stop();
                Utilities.ConsoleLog($"created neo4j nodes in time '{sw.Elapsed}', no relationships created");

                Utilities.ConsoleLog("'Create all bridges' finished");
            });

            await task;
            buttonsSwitchClickableTo(true);
        }

        private List<string> getBridgeNumbers()
        {
            if(sqlClient == null)
            {
                Utilities.ConsoleLog("no SQL connection");
                return [];
            }

            List<string> bridgeNumbers = sqlClient.SelectRowsOneColumn("BRUECKE", "BWNR");
            // bridgeNumbers.Count(): 20349

            // remove dublicates in the list (because one entry for each Teilbauwerk)
            bridgeNumbers = bridgeNumbers.Distinct().ToList();
            // bridgeNumbers.Count(): 17504

            // test-purpose: starting at <index>, take <count> bridges with GetRange(<index>, <count>)
            return bridgeNumbers.GetRange(0, 5);
        }

        private void createConstraints()
        {
            if (neo4jDriver == null)
            {
                Utilities.ConsoleLog("no Neo4j connection");
                return;
            }

            List<string> sibBw_labels = [];
            sibBw_labels.Add(static_SibBW_GES_BW.label);
            sibBw_labels.Add(static_SibBW_TEIL_BW.label);
            sibBw_labels.Add(static_SibBW_PRUFALT.label);
            sibBw_labels.Add(static_SibBW_SCHADALT.label);

            foreach (string label in sibBw_labels)
            {
                string cypherString = static_SibBW.GetCypherConstraintKey(label);
                Utilities.ConsoleLog(cypherString);
                neo4jDriver.ExecuteCypherQuery(cypherString);
                // neo4j requires the "CREATE CONSTRAINTS" to be single statements in _session.Run(..)
            }

            Utilities.ConsoleLog("created constriants");
        }

        private void extractAndLoadBridge(string bridgeNumber)
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

            // Extract Bauwerk from database
            // GES_BWs will only have one entry due to the outer foreach, but still uses the same functions as the other tables
            List<SibBW_GES_BW> GES_BWs = sqlClient.SelectRows<SibBW_GES_BW>(static_SibBW_GES_BW.sqlQuery(bridgeNumber));
            // Extract Teilbauwerke from database
            List<SibBW_TEIL_BW> TEIL_BWs = sqlClient.SelectRows<SibBW_TEIL_BW>(static_SibBW_TEIL_BW.sqlQuery(bridgeNumber));
            // Extract PrüfungenAlt from database
            List<SibBW_PRUFALT> PRUFALTs = sqlClient.SelectRows<SibBW_PRUFALT>(static_SibBW_PRUFALT.sqlQuery(bridgeNumber));
            // Extract SchädenAlt from database
            List<SibBW_SCHADALT> SCHADALTs = sqlClient.SelectRows<SibBW_SCHADALT>(static_SibBW_SCHADALT.sqlQuery(bridgeNumber));

            query = "";
            assembleCypherCreateQueries(GES_BWs);
            assembleCypherCreateQueries(TEIL_BWs);
            assembleCypherCreateQueries(PRUFALTs);
            assembleCypherCreateQueries(SCHADALTs);

            // finally, send query at the end of this bridge
            SendCypherQuery(ref query);

            Utilities.ConsoleLog($"created bridge no {bridgeNumber} " +
                $"with {TEIL_BWs.Count} TEIL_BWs, {PRUFALTs.Count} PRUFALTs, {SCHADALTs.Count} SCHADALTs");
        }

        private void assembleCypherCreateQueries<T>(List<T> entryList) where T : SibBw
        {
            foreach (T entry in entryList)
            {
                query += entry.GetCypherCreate() + "\n";
                if (query.Length > queryMaxLength)
                {
                    SendCypherQuery(ref query);
                }
            }
        }

        private void SendCypherQuery(ref string query)
        {
            if (query != "" && neo4jDriver != null)
            {
                neo4jDriver.ExecuteCypherQuery(query);
                query = "";
            }
        }

        private async void btn_CreateRelationshipsAllBridges_Click(object sender, RoutedEventArgs e)
        {
            if (neo4jDriver == null)
            {
                Utilities.ConsoleLog("no Neo4j connection");
                return;
            }

            buttonsSwitchClickableTo(false);

            // start new thread beside the UI-thread (which the button would use)
            Task task = Task.Run(() =>
            {
                Stopwatch sw = Stopwatch.StartNew();

                var x = neo4jDriver.ExecuteCypherQuery(
                    "MATCH (bw:GES_BW)\r\n" +
                    "MATCH (teilBw:TEIL_BW) WHERE bw.BWNR = teilBw.BWNR\r\n" +
                    "MERGE (bw)-[r:bw_teilBw]->(teilBw)\r\n" +
                    "RETURN count(r)").ToList();

                Utilities.ConsoleLog($"relationships created, there are {x[0]["count(r)"]} relationships fitting the pattern");

                var y = neo4jDriver.ExecuteCypherQuery(
                    "MATCH (teilBw:TEIL_BW)\r\n" +
                    "MATCH (prufAlt:PRUFALT) WHERE teilBw.ID_NR = prufAlt.ID_NR\r\n" +
                    "MERGE (teilBw)-[r:teilBw_prufAlt]->(prufAlt)\r\n" +
                    "RETURN count(r)").ToList();

                Utilities.ConsoleLog($"relationships created, there are {y[0]["count(r)"]} relationships fitting the pattern");

                var z = neo4jDriver.ExecuteCypherQuery(
                    "MATCH (prufAlt:PRUFALT)\r\n" +
                    "MATCH (schadAlt:SCHADALT) WHERE prufAlt.identifier = schadAlt.identifierPruf\r\n" +
                    "MERGE (prufAlt)-[r:prufAlt_schadAlt]->(schadAlt)\r\n" +
                    "RETURN count(r)").ToList();

                Utilities.ConsoleLog($"relationships created, there are {z[0]["count(r)"]} relationships fitting the pattern");

                sw.Stop();
            
                Utilities.ConsoleLog($"all relationships created in time {sw.Elapsed}");
            });

            await task;
            buttonsSwitchClickableTo(true);
        }

        private async void btn_CreateTimeseries_Click(object sender, RoutedEventArgs e)
        {
            if (neo4jDriver == null)
            {
                Utilities.ConsoleLog("no Neo4j connection");
                return;
            }

            buttonsSwitchClickableTo(false);

            // start new thread beside the UI-thread (which the button would use)
            Task task = Task.Run(() =>
            {
                neo4jDriver.ExecuteCypherQuery(
                    "MATCH (p:PRUFALT)   // get all PRUFALT\r\n" +
                    "CALL(p) {   // execute foreach PRUFALT\r\n" +
                    "  // get all other PRUFALT's into ps which are part of same TEIL_BW\r\n" +
                    "  MATCH (p)<-[:teilBw_prufAlt]-(:TEIL_BW)-[:teilBw_prufAlt]->(ps:PRUFALT)\r\n" +
                    "    // filter ps to have all PRUFALT ps being older than p\r\n" +
                    "    WITH ps WHERE ps.PRUFDAT2 < p.PRUFDAT2\r\n" +
                    "    // oder the list descending = youngest ps first;\r\n" +
                    "    // only take the youngest by LIMIT 1 into ps\r\n" +
                    "    ORDER BY ps.PRUFDAT2 DESC LIMIT 1\r\n" +
                    "  MERGE (p)-[:hat_vorherige_prufAlt]->(ps)\r\n" +
                    "  //RETURN collect([p, ps]) as psCollection\r\n" +
                    "}\r\n" +
                    "//RETURN psCollection\r\n");

                var x = neo4jDriver.ExecuteCypherQuery(
                    "MATCH r=(:PRUFALT)-[:hat_vorherige_prufAlt]->(:PRUFALT)\r\n" +
                    "RETURN DISTINCT count(r)").ToList();

                Utilities.ConsoleLog($"created {x[0]["count(r)"]} relationships ':hat_vorherige_prufAlt'");
            });

            await task;
            buttonsSwitchClickableTo(true);
        }

        private void btn_Neo4jDeleteNodes_Click(object sender, RoutedEventArgs e)
        {
            if (neo4jDriver == null)
            {
                Utilities.ConsoleLog("no Neo4j connection");
                return;
            }

            neo4jDriver.DeleteAllConstraintsInDatabase();
            neo4jDriver.DeleteAllNodesInDatabase();
        }

        private static IEnumerable<Button> buttonsList(DependencyObject element)
        {
            if (element != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(element, i);

                    if (child != null)
                    {
                        if (child is Button)
                        {
                            yield return (Button)child;
                        }

                        foreach (Button childOfChild in buttonsList(child))
                        {
                            yield return childOfChild;
                        }
                    }
                }
            }
        }
    }
}

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
using System.IO;

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
        Dictionary<string, string> asbIng_key_name = [];

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

            createConstraints();
            List<string> bridgeNumbers = getBridgeNumbers();

            // start new thread beside the UI-thread (which the button would use)
            Task task = Task.Run(() =>
            {
                Utilities.ConsoleLog("creating bridges ...");
                Stopwatch sw = Stopwatch.StartNew();

                foreach (string bridgeNumber in bridgeNumbers)
                {
                    extractAndLoadBridge(bridgeNumber);
                }

                sw.Stop();
                Utilities.ConsoleLog($"created neo4j nodes in time '{sw.Elapsed}', no relationships created");

                Utilities.ConsoleLog("creating relationships ...");
                createRelationshipsAllBridges();

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

            Utilities.ConsoleLog("created constraints");
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
            List<SibBW_GES_BW> GES_BWs = sqlClient.SelectRows<SibBW_GES_BW>(static_SibBW_GES_BW.GetSqlQuery(bridgeNumber));
            // Extract Teilbauwerke from database
            List<SibBW_TEIL_BW> TEIL_BWs = sqlClient.SelectRows<SibBW_TEIL_BW>(static_SibBW_TEIL_BW.GetSqlQuery(bridgeNumber));
            // Extract PrüfungenAlt from database
            List<SibBW_PRUFALT> PRUFALTs = sqlClient.SelectRows<SibBW_PRUFALT>(static_SibBW_PRUFALT.GetSqlQuery(bridgeNumber));
            // Extract SchädenAlt from database
            List<SibBW_SCHADALT> SCHADALTs = sqlClient.SelectRows<SibBW_SCHADALT>(static_SibBW_SCHADALT.GetSqlQuery(bridgeNumber));

            // potential data preparation goes here aka. 'staging area'
            // (nothing in the current stage of developement)

            // prepare CREATE statements; send them if too many in temporal storage
            query = "";
            assembleCypherCreateQueries(GES_BWs);
            assembleCypherCreateQueries(TEIL_BWs);
            assembleCypherCreateQueries(PRUFALTs);
            assembleCypherCreateQueries(SCHADALTs);

            // finally, send query at the end of this bridge
            SendCypherQuery();

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
                    SendCypherQuery();
                }
            }
        }

        private void SendCypherQuery()
        {
            if (query != "" && neo4jDriver != null)
            {
                neo4jDriver.ExecuteCypherQuery(query);
                query = "";
            }
        }

        private void createRelationshipsAllBridges()
        {
            if (neo4jDriver == null)
            {
                Utilities.ConsoleLog("no Neo4j connection");
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();

            List<Neo4j.Driver.IRecord> records = neo4jDriver.ExecuteCypherQuery(static_SibBW_GES_BW.GetCypherMergeToTeilBW()).ToList();
            Utilities.ConsoleLog($"{records[0]["count(r)"]} relationships created " +
                $"from {static_SibBW_GES_BW.label} to {static_SibBW_TEIL_BW.label}");

            records = neo4jDriver.ExecuteCypherQuery(static_SibBW_TEIL_BW.GetCypherMergeToPRUFALT()).ToList();
            Utilities.ConsoleLog($"{records[0]["count(r)"]} relationships created " +
                $"from {static_SibBW_TEIL_BW.label} to {static_SibBW_PRUFALT.label}");

            records = neo4jDriver.ExecuteCypherQuery(static_SibBW_PRUFALT.GetCypherMergeToSCHADALT()).ToList();
            Utilities.ConsoleLog($"{records[0]["count(r)"]} relationships created " +
                $"from {static_SibBW_PRUFALT.label} to {static_SibBW_SCHADALT.label}");

            sw.Stop();
            
            Utilities.ConsoleLog($"all relationships created in time {sw.Elapsed}");
        }

        private async void btn_CreatePropertyNodes_Click(object sender, RoutedEventArgs e)
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
                Neo4j.Driver.IResultSummary summary = neo4jDriver.ExecuteCypherQuery(
                    "MATCH (s:SCHADALT)\r\n" +
                    "UNWIND s.SCHADEN AS schadentyp\r\n" +
                    "MERGE (st:SCHADENTYP {typId:schadentyp})\r\n" +
                    "MERGE (s)-[:istSchadenstyp]->(st)\r\n").Consume();

                Utilities.ConsoleLog($"created {summary.Counters.NodesCreated} nodes of the label ':SCHADENTYP'\n" +
                    $"created {summary.Counters.RelationshipsCreated} relationships of the type ':istSchadenstyp'");

                // write the textual property of the schadenstyp from the ASB-ING-Key-list
                writePropertiesToSCHADENTYP();
            });

            await task;
            buttonsSwitchClickableTo(true);
        }

        private void btn_TestCsv_Click(object sender, RoutedEventArgs e)
        {
            writePropertiesToSCHADENTYP();
        }

        private void writePropertiesToSCHADENTYP()
        {
            if (neo4jDriver == null)
            {
                Utilities.ConsoleLog("no Neo4j connection");
                return;
            }

            Utilities.ConsoleLog("started to enrich ':SCHADENTYP' nodes with more information ...");

            // load schadentyp names from csv into class property
            asbIng_key_name = loadAsbIng_KeysAndNameFromCsv();

            // get all SCHADENTYP nodes
            List<Neo4j.Driver.IRecord> records = neo4jDriver.ExecuteCypherQuery(
                "MATCH(st:SCHADENTYP)\r\n" +
                "RETURN st.typId").ToList();

            int counterPropertiesSet = 0;
            foreach(Neo4j.Driver.IRecord record in records)
            {
                // get typId from node and its name from the key-name-mapping of ASB-ING
                string typId = record["st.typId"].ToString() ?? "";
                string name = asbIng_key_name[typId];

                // set property to node in graph
                Neo4j.Driver.IResultSummary summary = neo4jDriver.ExecuteCypherQuery(
                    $"MATCH(st:SCHADENTYP {{typId: {typId}}})\r\n" +
                    $"SET st.name = '{name}'").Consume();

                counterPropertiesSet += summary.Counters.PropertiesSet;
            }

            Utilities.ConsoleLog($"wrote {counterPropertiesSet} properties ':SCHADENTYP.name'");
        }

        Dictionary<string, string> loadAsbIng_KeysAndNameFromCsv()
        {
            using (var reader = new StreamReader(LoginCredentials.CsvSchadentypPath))
            {
                // initialize result variable
                Dictionary<string, string> asbIng_key_name = new Dictionary<string, string>();

                // skip first line (header) of csv
                reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    string? line = reader.ReadLine();
                    if (line == null) { continue; }
                    string[] values = line.Split(';');

                    // schadentypID (nr), and its full name (drk_text)
                    asbIng_key_name.Add(values[0], values[2]);
                }
                return asbIng_key_name;
            }
        }

        private async void btn_CreateGraphProjection_Click(object sender, RoutedEventArgs e)
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
                Utilities.ConsoleLog("creating projection ...");
                List<Neo4j.Driver.IRecord> records = neo4jDriver.ExecuteCypherQuery(
                    "MATCH (source:PRUFALT)-[]-(s:SCHADALT)-[]-(target:SCHADENTYP)\r\n" +
                    "WITH source, target\r\n" +
                    "WITH gds.graph.project(\r\n" +
                    "    'prufSchadentyp',\r\n" +
                    "    source,\r\n" +
                    "    target,\r\n" +
                    "    '*'\r\n" +
                    ") AS g\r\n" +
                    "RETURN g.graphName AS graph, g.nodeCount AS nodes, g.relationshipCount AS rels").ToList();

                Utilities.ConsoleLog($"Created graph projection '{records[0]["graph"]}' " +
                    $"with {records[0]["nodes"]} nodes and {records[0]["rels"]} relationships");
            });

            await task;
            buttonsSwitchClickableTo(true);
        }

        private async void btn_NodeSimilarityDamageType_Click(object sender, RoutedEventArgs e)
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
                Utilities.ConsoleLog("Executing algorithem of node similarity ...");
                List<Neo4j.Driver.IRecord> records = neo4jDriver.ExecuteCypherQuery(
                    "CALL gds.nodeSimilarity.write(\r\n" +
                    "  'prufSchadentyp',\r\n" +
                    "  {\r\n" +
                    "    similarityCutoff: 0.5,\r\n" +
                    "    writeRelationshipType: 'similar',\r\n" +
                    "    writeProperty: 'similarity_schadensmuster'\r\n" +
                    "  }\r\n" +
                    ")\r\n" +
                    "YIELD nodesCompared, relationshipsWritten, similarityDistribution").ToList();

                Utilities.ConsoleLog($"Node Similarity compared {records[0]["nodesCompared"]} nodes " +
                    $"and wrote {records[0]["relationshipsWritten"]} relationships ':similar'");
            });

            await task;
            buttonsSwitchClickableTo(true);
        }
        
        private async void btn_SimplifyBidirectionalRelationships_Click(object sender, RoutedEventArgs e)
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
                Neo4j.Driver.IResultSummary summary = neo4jDriver.ExecuteCypherQuery(
                    "MATCH (p1:PRUFALT)-[r:similar]->(p2:PRUFALT)\r\n" +
                    "MATCH (p2)-[:similar]->(p1)\r\n" +
                    "WHERE elementid(p1) > elementid(p2)\r\n" +
                    "DELETE r").Consume();

                Utilities.ConsoleLog($"deleted {summary.Counters.RelationshipsDeleted} relationships of the type ':similar'");
            });

            await task;
            buttonsSwitchClickableTo(true);
        }

        private async void btn_MarkRelationshipsInTBW_Click(object sender, RoutedEventArgs e)
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
                Neo4j.Driver.IResultSummary summary = neo4jDriver.ExecuteCypherQuery(
                    "MATCH (p1)-[s:similar]->(p2) \r\n" +
                    "WITH s, CASE\r\n" +
                    "    WHEN p1.ID_NR = p2.ID_NR THEN false \r\n" +
                    "    ELSE true\r\n" +
                    "END AS aTBw\r\n" +
                    "SET s.anderesTeilbauwerk = aTBw").Consume();

                Utilities.ConsoleLog($"set {summary.Counters.PropertiesSet} properties 'anderesTeilbauwerk: true | false' " +
                    $"on relationships of the type ':similar'");
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
                Neo4j.Driver.IResultSummary summary = neo4jDriver.ExecuteCypherQuery(
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
                    "//RETURN psCollection\r\n").Consume();

                Utilities.ConsoleLog($"created {summary.Counters.RelationshipsCreated} relationships ':hat_vorherige_prufAlt'");
            });

            await task;
            buttonsSwitchClickableTo(true);
        }

        private async void btn_ShowResult_Click(object sender, RoutedEventArgs e)
        {
            buttonsSwitchClickableTo(false);

            // start new thread beside the UI-thread (which the button would use)
            Task task = Task.Run(() =>
            {
                // identifier and similarity of example:
                string p1 = "ID_NR5727614_0_2002_H";
                string p2 = "ID_NR5926682_0_2003_H";
                string p2_Nachfolger = "ID_NR5926682_0_2020_H";
                double similarity_schadensmuster = 0.8;

                Utilities.ConsoleLog("\n" +
                    "Show results for an example query:");

                GetNewDamages(p1, p2, p2_Nachfolger, similarity_schadensmuster);
            });

            await task;
            buttonsSwitchClickableTo(true);
        }

        private void GetNewDamages(string p1, string p2, string p2_Nachfolger, double similarity_schadensmuster)
        {
            if (neo4jDriver == null)
            {
                Utilities.ConsoleLog("no Neo4j connection");
                return;
            }

            Utilities.ConsoleLog("\n" +
                $"The considered inspection {p1} has a similar damage pattern with inspection {p2},\n" +
                $"the similarity is {similarity_schadensmuster}.\n" +
                $"Inspection {p2} has the subsequent inspection {p2_Nachfolger}.\n");

            List<Neo4j.Driver.IRecord> records = neo4jDriver.ExecuteCypherQuery(
                "CALL{\r\n" +
                $"    MATCH (p2:PRUFALT {{identifier:'{p2}'}})-[:prufAlt_schadAlt]->(:SCHADALT)-[:istSchadenstyp]->(p2_Schaeden:SCHADENTYP)\r\n" +
                "    RETURN COLLECT(p2_Schaeden.typId) AS p2_Schaeden\r\n" +
                "}\r\n" +
                "CALL(p2_Schaeden){\r\n" +
                $"    MATCH (:PRUFALT {{identifier:'{p2_Nachfolger}'}})-[:prufAlt_schadAlt]->(schaden:SCHADALT)-[:istSchadenstyp]->(p2_Nachfolger_Schaeden:SCHADENTYP)\r\n" +
                "        WHERE NOT p2_Nachfolger_Schaeden.typId IN p2_Schaeden\r\n" +
                "    RETURN p2_Nachfolger_Schaeden, schaden\r\n" +
                "}\r\n" +
                "RETURN p2_Nachfolger_Schaeden.typId, p2_Nachfolger_Schaeden.name, schaden.BAUTEIL, schaden.KONTEIL").ToList();

            Utilities.ConsoleLog($"The following damages occured between {p2} and {p2_Nachfolger}:");
            Utilities.ConsoleLog("  [damage type ID], [damage name], [building component], [construction element]");
            foreach (Neo4j.Driver.IRecord record in records)
            {
                Utilities.ConsoleLog($"  {record["p2_Nachfolger_Schaeden.typId"]}, \"{record["p2_Nachfolger_Schaeden.name"]}\", " +
                    $"{record["schaden.BAUTEIL"]}, {record["schaden.KONTEIL"]}");
            }
            Utilities.ConsoleLog("\n");
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

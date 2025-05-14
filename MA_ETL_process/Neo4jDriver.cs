using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace MA_ETL_process
{
    internal class Neo4jDriver : IDisposable
    {
        private readonly IDriver _driver;
        private Neo4j.Driver.ISession _session;

        public Neo4jDriver()
        {
            _driver = GraphDatabase.Driver(
                LoginCredentials.Neo4jUri, 
                AuthTokens.Basic(LoginCredentials.Neo4jUser, LoginCredentials.Neo4jPassword));
            _session = _driver.Session(o => o.WithDatabase(LoginCredentials.Neo4jDatabase));
            Utilities.ConsoleLog("Connection to Neo4j Server successfully");
            // ToDo: was it realy successful?
        }

        public IResult ExecuteCypherQuery(string query)
        {
            return _session.Run(query);
        }

        public void DeleteAllNodesInDatabase()
        {
            ExecuteCypherQuery("MATCH (n) DETACH DELETE n");
            Utilities.ConsoleLog("all entries from current Neo4j database deleted");
        }

        public void DeleteAllConstraintsInDatabase()
        {
            List<IRecord> records = ExecuteCypherQuery("SHOW CONSTRAINTS YIELD name").ToList();
            foreach (IRecord record in records)
            {
                ExecuteCypherQuery($"DROP CONSTRAINT {record["name"]}");
            }
            Utilities.ConsoleLog("all constraints from current Neo4j database deleted");
        }

        public void Dispose()
        {
            _session.Dispose();
            _driver.Dispose();
        }
    }
}

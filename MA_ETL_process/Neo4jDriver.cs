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

        public void PrintGreeting(string message, int iInt, string iString)
        {
            // called function: Neo4j.Driver.IQueryRunner.Run(..)
            var result = _session.Run(
                "CREATE (a:Greeting) " +
                //"SET a.message = $message " +
                "SET a = {message: $message, iInt: $iInt, iString: $iString} \n" +
                "CREATE (b:Greeting) SET b = {message: $message} \n" +
                "RETURN a.message + ', from node ' + id(a), \n" +
                "  b.message + ', from node ' + id(b)",
                //new { messsage });
                new { message, iInt, iString }).Single();
            // test result: iInt is intager in neo4j, iString is string in neo4j

            // console output of several Return / result entities
            foreach (var item in result)
            {
                Utilities.ConsoleLog(item.Value.ToString());
            }
        }

        private void ExecuteCypherQuery(string query)
        {
            var result = _session.Run(query);
        }

        public void DeleteAllInDatabase()
        {
            ExecuteCypherQuery("MATCH (n) DETACH DELETE n");
            Utilities.ConsoleLog("all entries from current Neo4j database deleted");
        }

        public void Dispose()
        {
            _session.Dispose();
            _driver.Dispose();
        }
    }
}

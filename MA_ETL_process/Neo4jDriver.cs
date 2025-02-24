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
            var greeting = _session.ExecuteWrite(
                tx =>
                {
                    // called function: Neo4j.Driver.IQueryRunner.Run(..)
                    var result = tx.Run(
                        "CREATE (a:Greeting) " +
                        //"SET a.message = $message " +
                        "SET a = {message: $message, iInt: $iInt, iString: $iString} " +
                        "RETURN a.message + ', from node ' + id(a)",
                        //new { messsage });
                        new { message, iInt, iString }); // parameters of the query
                                                            // test result: iInt is intager in neo4j, iString is string in neo4j

                    return result.Single()[0].As<string>();
                });

            Utilities.ConsoleLog(greeting);
        }

        private void ExecuteCypherQuery(string query)
        {
            var result = _session.Run(query);
            //session.ExecuteWrite(
            //    tx =>
            //    {
            //        // called function: Neo4j.Driver.IQueryRunner.Run(..)
            //        var result = tx.Run(
            //            new Query(query));
            //        return;
            //        //return result.Single()[0].As<string>();
            //    });

            //Utilities.ConsoleLog(greeting);
        }

        public void DeleteAllInDatabase()
        {
            ExecuteCypherQuery("MATCH (n) DETACH DELETE n");
        }

        public void Dispose()
        {
            _session.Dispose();
            _driver.Dispose();
        }
    }
}

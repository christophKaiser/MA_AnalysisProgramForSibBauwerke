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
        private readonly IDriver? _driver;

        public Neo4jDriver()
        {
            _driver = GraphDatabase.Driver(
                LoginCredentials.Neo4jUri, 
                AuthTokens.Basic(LoginCredentials.Neo4jUser, LoginCredentials.Neo4jPassword));
            Utilities.ConsoleLog("Connection to Neo4j Server successfully");
            // ToDo: was it realy successful?
        }

        public void PrintGreeting(string message, int iInt, string iString)
        {
            var session = _driver.Session();
            var greeting = session.ExecuteWrite(
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

        public void Dispose()
        {
            _driver?.Dispose();
        }
    }
}

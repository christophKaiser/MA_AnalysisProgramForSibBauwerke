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

        public void Dispose()
        {
            _driver?.Dispose();
        }
    }
}

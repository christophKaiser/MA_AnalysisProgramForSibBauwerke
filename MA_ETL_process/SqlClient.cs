using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlC = Microsoft.Data.SqlClient;

namespace MA_ETL_process
{
    internal class SqlClient
    {
        private readonly SqlC.SqlConnection? _connection;

        public SqlClient()
        {
            _connection = new SqlC.SqlConnection(LoginCredentials.SqlConnectionString);
            _connection.Open();
            Utilities.ConsoleLog("Connection to SQL Server successfully");
            // ToDo: was it realy successful?
        }
    }
}

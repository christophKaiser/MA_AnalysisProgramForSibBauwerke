using System;
using System.Collections.Generic;
using System.Data;
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

            // example to test the dispatcher
            Thread.Sleep(5000);
            Utilities.ConsoleLog("awaiked");
        }

        public List<T> SelectRows<T>(string commandText) where T : SibBw, new()
        {
            List<T> sibBws = new List<T>();

            using (var command = new SqlC.SqlCommand())
            {
                command.Connection = _connection;
                command.CommandType = CommandType.Text;
                command.CommandText = commandText;

                SqlC.SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    T sibBw = new T();

                    string line = "";
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        //line += reader.GetName(i) + ": " + reader.GetValue(i) + "   ";  // maybe a bit slower
                        line += reader.GetName(i) + ": " + reader[i] + "   ";  // maybe a bit faster

                        sibBw.stringValues.Add(reader.GetName(i), reader[i].ToString());

                        // string reader[i].GetDataTypeName()  // fast
                        // Type reader[i].GetType() // slower than GetDataTypeName()
                        //                             ... at least on Debug-Console with text output in console
                    }
                    //Utilities.ConsoleLog(line);
                    sibBws.Add(sibBw);
                }

                reader.Close();
            }
            return sibBws;
            // get type(string) of list objects: sibBws[0].GetType().Name
        }

        public List<string> SelectRowsOneColumn(string table, string column)
        {
            List<string> result = new List<string>();

            using (var command = new SqlC.SqlCommand())
            {
                command.Connection = _connection;
                command.CommandType = CommandType.Text;
                command.CommandText =
                    $"SELECT {column} FROM [SIB_BAUWERKE_19_20230427].[dbo].[{table}]";

                SqlC.SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    result.Add(reader.GetValue(0).ToString());
                }

                reader.Close();
            }

            return result;
        }
    }
}

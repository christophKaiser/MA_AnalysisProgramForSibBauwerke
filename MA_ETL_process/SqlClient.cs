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
        }

        public void SelectTestRows()
        {
            string commandText = @"SELECT TOP (10) * FROM [SIB_BAUWERKE_19_20230427].[dbo].[GES_BW]";
            SelectRows(commandText);
        }

        public void SelectRows(string commandText)
        {
            using (var command = new SqlC.SqlCommand())
            {
                command.Connection = _connection;
                command.CommandType = CommandType.Text;
                command.CommandText = commandText;

                SqlC.SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string line = "";
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        //line += reader.GetName(i) + ": " + reader.GetValue(i) + "   ";  // maybe a bit slower
                        line += reader.GetName(i) + ": " + reader[i] + "   ";  // maybe a bit faster

                        // string reader[i].GetDataTypeName()  // fast
                        // Type reader[i].GetType() // slower than GetDataTypeName()
                        //                             ... at least on Debug-Console with text output in console
                    }
                    Utilities.ConsoleLog(line);
                }

                reader.Close();
            }
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
                    Utilities.ConsoleLog(line);
                    sibBws.Add(sibBw);
                }

                reader.Close();
            }
            return sibBws;
            // get type(string) of list objects: sibBws[0].GetType().Name
        }
    }
}

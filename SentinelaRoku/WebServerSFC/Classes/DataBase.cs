using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Data;

namespace WebServerSFC.Class
{
    public class DataBase : IDisposable
    {
        public DataTable DT = new DataTable();
        public int execSQLCommandMySQL(string Command)
        {
            int ID_HEADER = 0;
            MySqlConnection connection = null;
            try
            {
                DT.TableName = null;
                DT.PrimaryKey = null;
                DT.Columns.Clear();
                connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["stringConexao"].ToString());
                connection.Open();
                var CMD = new MySqlCommand();
                MySqlDataReader reader = null;
                CMD.Connection = connection;
                CMD.CommandText = Command;
                if (Command.Contains("INSERT") || Command.Contains("insert"))
                {
                    CMD.CommandText += "select last_insert_id()";
                    ID_HEADER = Convert.ToInt32(CMD.ExecuteScalar());
                }
                else
                {
                    reader = CMD.ExecuteReader();
                    if (reader.HasRows)
                    {
                        DT.Clear();
                        DT.Load(reader);
                    }
                }
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }
            return ID_HEADER;
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}

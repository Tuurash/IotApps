using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace AttendenceApp.Services
{
    class DBConnect
    {
        private MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;
        private int port;

        public DBConnect(string server_ip, string ps)
        {
            server = server_ip; 
            database = "brotecshrm";
            uid = "root";
            password = ps;

            port = 3306;
            Initialize();
        }

        //Initialize values
        private void Initialize()
        {
            string connectionString = "SERVER=" + server + ";" + "PORT=" + port + ";" + "DATABASE=" + database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
            connection = new MySqlConnection(connectionString);
        }
        //open connection to database
        private bool OpenConnection()
        {
            var ConnectionState = connection.State;

            try
            {
                if (ConnectionState.ToString() == "Closed")
                {
                    connection.Open();
                    return true;
                }
                else if (ConnectionState.ToString() == "Open")
                    return true;
                else
                {
                    try { connection.Open(); return true; }
                    catch (Exception ex) 
                    { 
                        //EmailServices.SendMail("\nException Detalis: " + ex);
                        return false; 
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Exception NO: " + ex.Number);
                //EmailServices.SendMail("\n Exception Number: " + ex.Number.ToString() + "\n Exception Details: " + ex.Message);
                return false;
            }
        }

        private bool CloseConnection()
        {
            var ConnectionState = connection.State;

            try
            {
                if (ConnectionState.ToString() == "Closed")
                    return true;
                else if (ConnectionState.ToString() == "Open") { connection.Close(); return true; }
                else
                {
                    try { connection.Close(); return true; }
                    catch (Exception ex) 
                    { 
                        //EmailServices.SendMail("\nException Detalis: " + ex);
                        return false; 
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
                //EmailServices.SendMail("\n Exception Number: " + ex.Number.ToString() + "\n Exception Details: " + ex.Message);
                return false;
            }
        }
        public bool TestConnection()
        {
            if (OpenConnection() == true)
            {
                if (CloseConnection() == true)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        #region CRUD methods

        public List<string>[] Select(string query, int colNo = 0)
        {
            //Create a list to store the result
            List<string>[] result = new List<string>[colNo];
            int i;
            if (colNo > 0)
            {
                for (i = 0; i < colNo; i++)
                {
                    result[i] = new List<string>();
                }
            }
            else
            {
                result[colNo] = new List<string>();
            }

            if (query != string.Empty)
            {
                //Open connection
                if (this.OpenConnection() == true)
                {

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    int count = 0;
                    while (dataReader.Read())
                    {
                        for (i = 0; i < colNo; i++, count++)
                        {
                            if (dataReader.GetValue(count) != DBNull.Value)
                            {
                                result[i].Add(dataReader.GetString(count));
                                // dbLog.Log("RESULT: " + dataReader.GetString(count) + " ;", LogFile.LogLevel.Debug);
                            }
                            else
                            {
                                // dbLog.Log("RESULT: DB Null ;", LogFile.LogLevel.Debug);
                                break;
                            }
                        }
                    }
                }
                this.CloseConnection();
            }
            return result;
        }

        public string Select(string query)
        {
            //Create a list to store the result
            string result = "";
            if (query != string.Empty)
            {

                //Open connection
                if (this.OpenConnection() == true)
                {
                    // dbLog.Log("QUERY: " + query + ";", LogFile.LogLevel.Debug);
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        if (!dataReader.IsDBNull(0))
                        {
                            result = Convert.ToString(dataReader.GetValue(0));
                            // dbLog.Log("RESULT: " + dataReader.GetString(0) + " ;", LogFile.LogLevel.Debug);
                        }
                        else
                        {
                            // dbLog.Log("RESULT: DB Null ;", LogFile.LogLevel.Debug);
                            result = null;
                        }
                    }

                    //result = dataReader[;
                }
                this.CloseConnection();
            }
            return result;
        }

        //Insert statement
        public bool Insert(string query)
        {
            //open connection
            if (this.OpenConnection() == true)
            {
                // dbLog.Log("QUERY: " + query + ";", LogFile.LogLevel.Debug);
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, connection);

                //Execute command
                cmd.ExecuteNonQuery();

                //close connection
                this.CloseConnection();
                // dbLog.Log("RESULT: true ;", LogFile.LogLevel.Debug);
                return true;
            }
            // dbLog.Log("RESULT: false ;", LogFile.LogLevel.Debug);
            return false;
        }

        //Update statement
        public bool Update(string query)
        {
            //Open connection
            if (this.OpenConnection() == true)
            {

                MySqlCommand cmd = new MySqlCommand();
                cmd.CommandText = query;
                cmd.Connection = connection;

                //Execute query
                cmd.ExecuteNonQuery();
                //close connection
                this.CloseConnection();
                return true;
            }
            return false;
        }

        #endregion
    }
}

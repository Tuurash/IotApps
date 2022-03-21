using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace AttendanceApplicationF1.Services
{
    class DBConnect
    {
        private MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;
        //newly added
        private int port;

        private int dbDebug;

        //private LogFile  dbLog;
        private string dbLogFilePath;
        //Constructor
        public DBConnect(string server_ip, string ps, int debug, string logfilepath)
        {
            server = server_ip; //server_ip // "192.168.30.28" //"192.168.30.152"
            database = "brotecshrm";
            uid = "root";
            password = ps;//"brotecs";//ps //"brotecs1230"
            port = 3306;
            dbDebug = debug;
            dbLogFilePath = logfilepath;
            Initialize();
        }

        //Initialize values
        private void Initialize()
        {
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "PORT=" + port + ";" + "DATABASE=" + database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
            connection = new MySqlConnection(connectionString);
        }



        //open connection to database
        private bool OpenConnection()
        {
            if (dbDebug > 2) Console.Write("Opening Database...");

            var ConnectionState = connection.State;


            try
            {
                if (ConnectionState.ToString() == "Closed")
                {
                    connection.Open();

                    if (dbDebug > 2) Console.WriteLine("Successfull");
                    //// dbLog.Log("Opening Database...Successfull ;", LogFile.LogLevel.Info);
                    return true;
                }
                else if (ConnectionState.ToString() == "Open")
                {
                    return true;
                }
                else { return false; }
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                Console.WriteLine("Exception NO: " + ex.Number);
                switch (ex.Number)
                {
                    case 0:
                        if (dbDebug == 2) Console.WriteLine("Cannot connect to server.  Contact administrator");
                        // dbLog.Log("Cannot connect to server.  Contact administrator ;", LogFile.LogLevel.Error);
                        break;

                    case 1045:
                        if (dbDebug == 2) Console.WriteLine("Invalid username/password, please try again");
                        // dbLog.Log("Invalid username/password, please try again ;", LogFile.LogLevel.Error);
                        break;

                    default:
                        if (dbDebug == 2) Console.WriteLine("Other: " + ex.Message);
                        // dbLog.Log(ex.Message + ";", LogFile.LogLevel.Error);
                        break;
                }
                return false;
            }

        }

        //Close connection
        private bool CloseConnection()
        {
            if (dbDebug > 2) Console.Write("Closing Database...");
            try
            {
                connection.Close();
                if (dbDebug > 2) Console.WriteLine("Successfull");
                //// dbLog.Log("Closing Database...Successfull ;", LogFile.LogLevel.Info);
                return true;
            }
            catch (MySqlException ex)
            {
                if (dbDebug > 2) Console.WriteLine(ex.Message);
                // dbLog.Log(ex.Message + ";", LogFile.LogLevel.Error);
                return false;
            }
        }

        public bool TestConnection()
        {
            if (OpenConnection() == true)
            {
                //Console.WriteLine( "OpenConnection TRUE" );
                if (dbDebug > 2) Console.WriteLine("Test Database connection opened");
                //// dbLog.Log("Test Database connection opened ;", LogFile.LogLevel.Info);
                if (CloseConnection() == true)
                {
                    if (dbDebug > 2) Console.WriteLine("Test Database connection closed");
                    //// dbLog.Log("Test Database connection closed ;", LogFile.LogLevel.Info);
                    return true;
                }
                else
                {
                    if (dbDebug > 2) Console.WriteLine("Error Closing Test Database connection");
                    // dbLog.Log("Error Closing Test Database connection ;", LogFile.LogLevel.Error);
                    return false;
                }
            }
            else
            {
                if (dbDebug > 2) Console.WriteLine("Error Opening Test Database connection");
                // dbLog.Log("Error Opening Test Database connection ;", LogFile.LogLevel.Error);
                return false;
            }
        }

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
                    // dbLog.Log("QUERY: " + query + " ;", LogFile.LogLevel.Debug);
                    //Create Command
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
                // dbLog.Log("QUERY: " + query + ";", LogFile.LogLevel.Debug);
                //create mysql command
                MySqlCommand cmd = new MySqlCommand();
                //Assign the query using CommandText
                cmd.CommandText = query;
                //Assign the connection using Connection
                cmd.Connection = connection;

                //Execute query
                cmd.ExecuteNonQuery();

                //close connection
                this.CloseConnection();
                // dbLog.Log("RESULT: true ;", LogFile.LogLevel.Debug);
                return true;
            }
            // dbLog.Log("RESULT: false ;", LogFile.LogLevel.Debug);
            return false;
        }
    }
}

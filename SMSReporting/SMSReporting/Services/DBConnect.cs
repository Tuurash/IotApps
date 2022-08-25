using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BrotecsLateSMSReporting
{
    class DBConnect
    {
        private MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;

        private static Mutex mut = new Mutex();

        //private LogFile  dbLog;
        //Constructor
        public DBConnect(string server_ip, string ps, string usrID, string dbName)
        {
            server = server_ip; // "192.168.30.28"
            //database = "brotecshrm";
            database = dbName;      // Amjad; 5th Dec, 2013
            uid = usrID;
            password = ps;//"brotecs";//
            Initialize();
        }

        //Initialize values
        private void Initialize()
        {
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" + database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
            connection = new MySqlConnection(connectionString);
        }

        //open connection to database
        private bool OpenConnection()
        {
            var ConnectionState = connection.State;
            mut.WaitOne();
            try
            {
                if (ConnectionState.ToString() == "Closed")
                {
                    Task.Delay(1000).Wait();
                    connection.Open();
                    return true;
                }
                else if (ConnectionState.ToString() == "Open")
                    return true;
                else
                {
                    try { connection.Open(); return true; }
                    catch (Exception) { return false; }
                }
            }
            catch (Exception exc) { Console.WriteLine("Exception At: " + exc.Message); return false; }
        }

        private bool CloseConnection()
        {

            mut.ReleaseMutex();
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                Trace.WriteLine(string.Format("[{0}]", ex.Message));
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





        public List<List<string>> Select(string query, List<string> atributesOfTable)
        {
            //Create a list to store the result
            List<List<string>> result = new List<List<string>>();

            int i, len = atributesOfTable.Count;

            if (query != string.Empty)
            {
                //Open connection
                if (this.OpenConnection() == true)
                {
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        List<string> currentList = new List<string>();
                        string str;
                        //string query = "SELECT empID,empTel,informedTS,smsPayload FROM brotecshrm.blt_lateSMS_detail WHERE DATE(informedTS)='dateTime'";
                        for (i = 0; i < len; i++)
                        {
                            str = dataReader.GetString(dataReader.GetOrdinal(atributesOfTable[i]));
                            currentList.Add(str);
                        }
                        result.Add(currentList);
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
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        if (!dataReader.IsDBNull(0))
                        {
                            result = Convert.ToString(dataReader.GetValue(0));
                        }
                        else
                        {
                            result = null;
                        }
                    }
                    //dataReader.Close();     // Amjad; 26th Oct 2013
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
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, connection);

                //Execute command
                cmd.ExecuteNonQuery();

                //close connection
                this.CloseConnection();
                return true;
            }
            return false;
        }

        //Update statement
        public bool Update(string query)
        {
            //Open connection
            if (this.OpenConnection() == true)
            {
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
                return true;
            }
            return false;
        }
    }
}

using BrotecsLateSMSReporting.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BrotecsLateSMSReporting
{
    class SMSInfo
    {
        private int employeeid;
        private string sender;
        private string lateDuration;
        private string netWorkTimeStemp;
        private string smsContent;
        private int smsType;

        public int EmployeeID
        {
            get { return employeeid; }
            set { employeeid = value; }
        }

        public string Sender
        {
            get { return sender; }
            set { sender = value; }
        }

        public string LateDuration
        {
            get { return lateDuration; }
            set { lateDuration = value; }
        }

        public string NetWorkTimeStemp
        {
            get { return netWorkTimeStemp; }
            set { netWorkTimeStemp = value; }
        }

        public string SMSContent
        {
            get { return smsContent; }
            set { smsContent = value; }
        }
        /*
         * Edit: Topu
         * Date: July 22, 2013
         * considering smstype field in blt_lateSMS_detail table
         * smstype=0 for late
         * smstype=1 for leave
         * excuse: 2 for late excuse
         * invalid: -1
        */
        public int SMSType
        {
            get { return smsType; }
            set { smsType = value; }
        }
    }

    class DBHandler
    {
        #region Global variables
        private DBConnect dbConnect;
        private DBConnect dbConnectSMS;

        // configuration files
        private IniFile DBiniFile;
        private static string configurationFilename = "./dbConfig.ini";

        // hrm_db variables
        private string hrm_server_ip = "192.168.30.252", hrm_ps = "", hrm_uid = "root";     // Topu; 12/12/13 :) 
        private string hrm_db_name = "brotecshrm";

        // local_db variables
        private string local_server_ip = "192.168.30.252", local_ps = "", local_uid = "root";     // Topu; 12/12/13 :) 
        private string local_db_name = "brotecs_sms_db";

        #endregion


        #region Constructor
        //public DBHandler(string server_ip, string ps, string uid)
        public DBHandler()
        {
            try
            {
                readDB_Configuration();
                Trace.WriteLine("DBHandler: reading configuration file is complete");
                dbConnect = new DBConnect(hrm_server_ip, hrm_ps, hrm_uid, hrm_db_name);
                Trace.WriteLine("DBHandler: dbConnect construction is done");
                dbConnectSMS = new DBConnect("192.168.30.252", "", "root", "brotecs_sms_db");
                Trace.WriteLine("DBHandler: dbConnectSMS construction is done");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("error! {0}", ex.Message);
            }
        }
        #endregion


        // Amjad; 11/12/13 :)
        #region dbConfiguration Read/Write
        private void readDB_Configuration()
        {
            DBiniFile = new IniFile();// this was causing the worning
            string temp = "";

            DBiniFile.Load(configurationFilename);

            // hrm_db setting
            if (DBiniFile.GetKeyValue("hrm_db", "serverIP") != string.Empty)
            {
                hrm_server_ip = DBiniFile.GetKeyValue("hrm_db", "serverIP").Trim();
            }



            if (DBiniFile.GetKeyValue("hrm_db", "serverKey") != string.Empty)
            {
                temp = DBiniFile.GetKeyValue("hrm_db", "serverKey").Trim();
                if (temp.Contains("null") || temp == string.Empty || temp == null || temp == "")
                {
                    hrm_ps = "";
                }
                else
                {
                    hrm_ps = temp;
                }
            }
            else
            {
                hrm_ps = "";
            }

            if (DBiniFile.GetKeyValue("hrm_db", "usrID") != string.Empty)
            {
                hrm_uid = DBiniFile.GetKeyValue("hrm_db", "usrID").Trim();
            }

            // local_db settings
            if (DBiniFile.GetKeyValue("local_db", "serverIP") != string.Empty)
            {
                local_server_ip = DBiniFile.GetKeyValue("local_db", "serverIP").Trim();
            }

            if (DBiniFile.GetKeyValue("local_db", "serverKey") != string.Empty)
            {
                temp = DBiniFile.GetKeyValue("local_db", "serverKey").Trim();
                if (temp.Contains("null") || temp == string.Empty || temp == null || temp == "")
                {
                    local_ps = "";
                }
                else
                {
                    local_ps = temp;
                }
            }
            else
            {
                local_ps = "";
            }

            if (DBiniFile.GetKeyValue("local_db", "usrID") != string.Empty)
            {
                local_uid = DBiniFile.GetKeyValue("local_db", "usrID").Trim();
            }
        }
        #endregion


        public bool PostReport(SMSInfo Info)
        //public void PostReport(object data)
        {
            //SMSInfo Info = (SMSInfo)(data);
            string query;
            int empID = Info.EmployeeID;
            string empTel = "'" + Info.Sender + "'";
            string informedTS = "'" + getHRMSystemDateTime() + "'";
            string probableEntry = "'" + Info.LateDuration + "'";
            string networkTS = DateTime.Parse(Info.NetWorkTimeStemp).ToString("yyyy-MM-dd HH:mm:ss");  //"yyyy-MM-dd HH:mm:ss"

            string smsPayload = "'" + Info.SMSContent + "'";
            /*
            * considering smstype field in blt_lateSMS_detail table
            * smstype=0 for late
            * smstype=1 for leave
            */

            bool DetailInserted = false;
            bool ExcuseInserted = false;

            int smsType = Info.SMSType;
            query = @"INSERT INTO brotecshrm.blt_lateSMS_detail (empID,empTel,informedTS,probableEntry,smsPayload,networkTS,smstype) 
                    VALUES(" + empID + "," + empTel + "," + informedTS + "," + probableEntry + "," + smsPayload + ",'" + networkTS + "'," + smsType + ")";

            try
            {
                DetailInserted = dbConnect.Insert(query);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("error! {0}", ex.Message);
            }
            if (smsType == 2)
            {
                string applied = "'0'";
                query = "INSERT INTO brotecshrm.btl_late_excuse (blt_latesms_detail_id,employee_id,apply_time,status) VALUES(" + empID + "," + informedTS + "," + applied + ")";
                Trace.WriteLine(string.Format("query: {0}", query));
                try
                {
                    ExcuseInserted = dbConnect.Insert(query);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("error! {0}", ex.Message);
                }
            }

            return DetailInserted;
        }

        public int GetEmployeeID(string CellNumber)
        {
            int employeeID = -1;
            string result = dbConnect.Select(@"SELECT employee_id FROM hs_hr_employee WHERE emp_mobile LIKE '%" + CellNumber + "%'");
            if (!String.IsNullOrEmpty(result))
                employeeID = Convert.ToInt32(result);
            return employeeID;
        }

        public string getHRMSystemDateTime()
        {
            string query = "SELECT DATE_FORMAT(NOW(),'%Y-%m-%d %H:%i:%s')";
            string result = dbConnect.Select(query);
            if (result != string.Empty && result != null && result != "")
            {
                return result;
            }
            return string.Empty;
        }

        public string getHRMSystemTime(string sentTime)
        {
            string query = "SELECT DATE_FORMAT('" + sentTime + "', '%H:%i:%s')";
            string result = dbConnect.Select(query);
            if (result != string.Empty && result != null && result != "")
            {
                return result;
            }
            return string.Empty;
        }

        public string getHRMSystemDate()
        {
            string query = "SELECT DATE_FORMAT(NOW(),'%Y-%m-%d')";
            Trace.WriteLine(string.Format("query: {0}", query));
            string result = dbConnect.Select(query);
            if (!String.IsNullOrEmpty(result))
                return result;

            return string.Empty;
        }

        public string getNetworkTime(string sentTime)
        {
            string query = "SELECT DATE_FORMAT('" + sentTime + "', '%Y-%m-%d %H:%i:%s')";
            string result = dbConnect.Select(query);
            if (result != string.Empty && result != null && result != "")
            {
                return result;
            }
            return string.Empty;
        }

        public string getProbableEntry(string lateTime)
        {
            string query = "SELECT TIME_FORMAT(ADDTIME('09:30:00','" + lateTime + "'),'%H:%i:%s')";
            string result = dbConnect.Select(query);
            if (!String.IsNullOrEmpty(result))
            {
                return result;
            }
            return string.Empty;
        }

        public bool PostLeaveReport(string date)
        {
            return true;
        }

        public string getEmployeeFullName(int employeeID)
        {
            string fullName = "";
            string query = "SELECT emp_firstname FROM hs_hr_employee WHERE emp_number=" + employeeID;
            string result = dbConnect.Select(query);
            if (result != string.Empty && result != null && result != "")
            {
                fullName += result + " ";
            }
            query = "SELECT emp_middle_name FROM hs_hr_employee WHERE emp_number=" + employeeID;
            result = dbConnect.Select(query);
            result = dbConnect.Select(query);
            if (result != string.Empty && result != null && result != "")
            {
                fullName += result + " ";
            }
            query = "SELECT emp_lastname FROM hs_hr_employee WHERE emp_number=" + employeeID;
            result = dbConnect.Select(query);
            result = dbConnect.Select(query);
            if (result != string.Empty && result != null && result != "")
            {
                fullName += result + " ";
            }
            return fullName;
        }

        public string getEmployeeWorkEmailAddress(int employeeId)
        {
            //SELECT emp_work_email FROM hs_hr_employee WHERE employee_id=
            string employeeWorkEmailAddress = "";
            string query = "SELECT emp_work_email FROM hs_hr_employee WHERE employee_id=" + employeeId;
            string result = dbConnect.Select(query);
            if (result != string.Empty && result != null && result != "")
            {
                employeeWorkEmailAddress = result;
            }
            return employeeWorkEmailAddress;
        }

        //Amjad
        public bool performUPDATE_on_localDB(string query)
        {
            return (dbConnectSMS.Update(query));
        }
        public List<List<string>> getSMSReceivedOnDate(string dateTime)
        {
            List<List<string>> result = new List<List<string>>();

            List<string> Atributes = new List<string>();
            Atributes.Add("empID");
            Atributes.Add("empTel");
            Atributes.Add("informedTS");
            Atributes.Add("smsPayload");

            //informedTS is when recieved //networkTS is when sent
            string query = "SELECT empID,empTel,informedTS,smsPayload FROM brotecshrm.blt_lateSMS_detail WHERE DATE(networkTS)='" + dateTime + "'";

            result = dbConnect.Select(query, Atributes);

            return result;
        }


        //Amjad
        public List<string> getAllActiveEmployeePhoneNumbers()      // Except Nahid vaia, Niger Apu, and Murada(Admin) vai
        {
            List<List<string>> allIDs = new List<List<string>>();
            List<string> nums = new List<string>();

            string query = "SELECT id FROM ohrm_user WHERE status=1";

            List<string> Atributes = new List<string>();
            Atributes.Add("id");

            allIDs = dbConnect.Select(query, Atributes);

            int i, len = allIDs.Count;
            for (i = 0; i < len; i++)
            {
                if (allIDs[i][0] != "1" && allIDs[i][0] != "8" && allIDs[i][0] != "20")    // discurding Nahid vai, Murad vai & Niger apu
                {
                    query = "SELECT hs_hr_employee.emp_mobile FROM hs_hr_employee WHERE employee_id=" + allIDs[i][0];
                    nums.Add(dbConnect.Select(query));
                }
            }

            return nums;
        }

        //Amjad; 8th Dec, 2013

        public long getIdOfUnreadMsg(string query)
        {
            //string query = "SELECT MIN(id) FROM send_sms WHERE status='0'";
            string id = dbConnectSMS.Select(query);

            if (id != string.Empty && id != null && id != "")
            {
                return (Convert.ToInt64(id));
            }

            return -1;
        }


        //Amjad; 7th Jan, 2014

        public List<string> getReceivedUnreadSMS(long id)
        {
            List<string> res = new List<string>();
            string str;

            try
            {
                str = dbConnectSMS.Select("SELECT emp_id FROM received_sms WHERE id=" + id.ToString() + " AND sms_state='0'");
                res.Add(str);
                str = dbConnectSMS.Select("SELECT mobile_num FROM received_sms WHERE id=" + id.ToString() + " AND sms_state='0'");
                res.Add(str);
                str = dbConnectSMS.Select("SELECT recv_time FROM received_sms WHERE id=" + id.ToString() + " AND sms_state='0'");
                res.Add(str);
                str = dbConnectSMS.Select("SELECT sms_content FROM received_sms WHERE id=" + id.ToString() + " AND sms_state='0'");
                res.Add(str);
                //str = dbConnectSMS.Select("SELECT sms_type FROM received_sms WHERE id=" + id.ToString() + " AND sms_state='0'");
                //res.Add(str);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("error! {0}", ex.Message);
            }

            return res;
        }


        // Amjad; 12th Dec, 2013
        public bool updateSms_sendStatus(long ID)
        {
            string id = ID.ToString();

            string query = "UPDATE send_sms SET status='1' WHERE id=" + id;

            Trace.WriteLine(string.Format("query: {0}", query));
            try
            {
                if (dbConnectSMS.Update(query) != true)
                {
                    Trace.WriteLine("Error accessing DB");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("error! {0}", ex.Message);
                return false;
            }

            query = "UPDATE send_sms SET sent_time =" + getHRMSystemDateTime() + "WHERE id=" + id;
            Trace.WriteLine(string.Format("query: {0}", query));
            try
            {
                if (dbConnectSMS.Update(query) != true)
                {
                    Trace.WriteLine("Error accessing DB");
                    return false;
                }

            }
            catch (Exception ex)
            {
                Trace.WriteLine("error! {0}", ex.Message);
                return false;
            }

            return true;
        }


        // Amjad; 12th Dec, 2013
        public List<string> getNumsAndMsgFromSMS_send(long ID)
        {
            List<string> res = new List<string>();
            string id = ID.ToString();

            string query = "SELECT emp_phone_num FROM send_sms WHERE id=" + id;
            string tem = dbConnectSMS.Select(query);
            res.Add(tem);   // phone number

            query = "SELECT sms_content FROM send_sms WHERE id=" + id;
            tem = dbConnectSMS.Select(query);
            res.Add(tem);   // notice message

            return res;
        }

        //Amjad
        //public void postINsmsDB_received(int type, int emp_id, string nMsg)
        //{
        //    string Query = "INSERT INTO received_sms (emp_id, recv_time, sms_content, sms_state, sms_type) VALUES (" + emp_id.ToString() + "," + getHRMSystemDateTime() + "," + nMsg + "0," + type.ToString() + ")";
        //    Trace.WriteLine(string.Format("query: {0}", Query));
        //    try
        //    {
        //        if (dbConnectSMS.Insert(Query) != true)
        //        {
        //            Trace.WriteLine("Error accessing DB");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.WriteLine("error! {0}", ex.Message);
        //    }
        //}

        /*
         * Turash
         * Generalize Query
         */
        public bool ExecuteInsertionLocal(string query) => dbConnectSMS.Insert(query);
        public bool ExecuteNonQueryLocal(string query) => dbConnectSMS.Update(query);
        public string performSELECT_on_localDB(string query) => (dbConnectSMS.Select(query));


        public string getEmpFullName(int employeeID)
        {
            string fullName = "";

            string query = @"SELECT CONCAT(emp_firstname,' ', emp_middle_name,' ', emp_lastname) AS fullName  FROM hs_hr_employee WHERE emp_number=" + employeeID;
            string result = dbConnect.Select(query);
            if (!String.IsNullOrEmpty(result))
                fullName = result;

            return fullName;
        }

        //Amjad
        public void updateSms_receivedStatus(string qry)
        {
            Trace.WriteLine(string.Format("query: {0}", qry));
            //string outStr = "";
            try
            {
                if (dbConnectSMS.Insert(qry) != true)
                {
                    Trace.WriteLine("Error accessing DB");
                    //outStr = "Error accessing DB";
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("error! {0}", ex.Message);
                //outStr = string.Format("error! {0}", ex.Message);
            }

            //MessageBox.Show("query: "+ qry + "  our: "+outStr);
        }


        //Amjad
        public void postINsmsDB_send(List<string> numbers, string noticeMsg)
        {
            int i, lenOfNumbers = numbers.Count, id;
            string time = getHRMSystemDateTime();

            for (i = 0; i < lenOfNumbers; i++)
            {
                id = GetEmployeeID(numbers[i]);
                string Query = "INSERT INTO send_sms (emp_id, emp_phone_num, sms_content, status) VALUES (" + id.ToString() + "," + numbers[i] + "," + noticeMsg + ", 0)";
                //INSERT INTO send_sms (emp_sms_sendid, emp_phone_num, sms_content, STATUS ) VALUES ("53", "015555966", "Hello", "0")
                Trace.WriteLine(string.Format("query: {0}", Query));
                try
                {
                    if (dbConnectSMS.Insert(Query) != true)
                    {
                        Trace.WriteLine("Error accessing DB");
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("error! {0}", ex.Message);
                }
            }
        }

    }
}

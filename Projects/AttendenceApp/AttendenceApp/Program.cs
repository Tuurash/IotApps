/*
    Last Update: 10 March 2022 
    Target Framework .net 4.8
    Developed By: Mohaimanul Haque Turash
    For: ZKTeco F18 Machine
    Dependecies : MySql.Data.dll , zkemkeeper.dll (available in SDK)
 */

using AttendenceApp.Models;
using AttendenceApp.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace AttendenceApp
{
    class Program
    {
        class FingerPrintDataCollector
        {
            #region Object Properties
            private const string version = "14-10-2018 ";
            private static IniFile iniFile;
            private static zkemkeeper.CZKEM F18;
            private static DBConnect dbConnect;
            private static string logfile = @"userlog.txt";
            private static string settingFilename = "setting.ini"; //now target in project
            private static string dbLogFilePath = "";
            private static int logcount;
            private static int portNo = 0;
            private static string devModel = "";
            private static int devNo = 0;
            private static string ipAdd = "";
            private static int comm = 0;
            private static int employee_limit = 0;
            private static string server = "";
            private static string psd = "";
            private static bool _continue;

            //Office In-Out Time
            private static int Office_Endtime = 17;
            private static int late_allowed_mins = 45;
            private static int entrytime_HH = 8;


            private static int restartTime = 6;//6
            private static int debug;
            private const string hrmPunchInExceptionURL = "http://192.168.30.252/brotecsHRM/autoscripts/punchException.php?";
            static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
            private delegate bool ConsoleEventDelegate(int eventType);
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
            #endregion

            private static void Initialize()
            {
                devModel = "F18";
                devNo = 1;
                ipAdd = "192.168.30.230";
                portNo = 4370;
                comm = 0;
                employee_limit = 100;
                iniFile = new IniFile();
                logcount = 0;
                //logSet = 0;
                try
                {

                    // Reading Configuration Parameters from setting.ini file
                    readConfiguration();
                    if (debug > 0) Console.WriteLine("Initializing components...");

                    // Creating ZK F18 instance
                    F18 = new zkemkeeper.CZKEM();

                    int commKey = 1234;
                    F18.SetCommPassword(Convert.ToInt32(commKey));

                    //Creating Database Connection Instance
                    dbConnect = new DBConnect(server, psd, debug, dbLogFilePath);
                    _continue = false;
                }
                catch (Exception exc)
                {

                    //throw exc;

                    Console.WriteLine("Failed Registering The Device: \n Exception Details: \n " + exc);
                }
            }

            private static void readConfiguration()
            {
                IniHandler iniHandler = new IniHandler();

                if (!File.Exists(settingFilename))
                {
                    //name-key-section
                    iniHandler.Write("log_count", "558", "records");

                    iniHandler.Write("port", "4370", "configuration");
                    iniHandler.Write("ip", "192.168.30.200", "configuration");
                    iniHandler.Write("device_no", "1", "configuration");
                    iniHandler.Write("model", "R2", "configuration");
                    iniHandler.Write("comm_key", "1234", "configuration");
                    iniHandler.Write("debug", "0", "configuration");

                    iniHandler.Write("employee_limit", "24", "limits");

                    iniHandler.Write("serverIP", "192.168.30.252", "db_settings");
                    iniHandler.Write("serverKey", "null", "db_settings");
                }

                string temp = "";
                iniFile.Load(settingFilename);
                //iniFile.Load(settingFilename);
                debug = Int32.Parse(iniFile.GetKeyValue("configuration", "debug"));
                if (debug > 0) Console.WriteLine(String.Format("Section Count {0}", iniFile.Sections.Count));
                foreach (IniFile.IniSection sec in iniFile.Sections)
                {
                    if (debug > 0) Console.WriteLine(String.Format("Section {0} Key Count {1}", sec.Name, sec.Keys.Count));
                    foreach (IniFile.IniSection.IniKey key in sec.Keys)
                    {
                        if (debug > 0) Console.WriteLine(String.Format("Section {0} Key={1} Value={2}", sec.Name, key.Name, key.Value));
                    }
                }
                if (iniFile.GetKeyValue("restart", "restart_time") != string.Empty)
                {
                    temp = iniFile.GetKeyValue("restart", "restart_time");
                    restartTime = Convert.ToInt32(temp.Trim());
                    if (restartTime < 6) restartTime = 6;
                }
                if (iniFile.GetKeyValue("configuration", "ip") != string.Empty)
                {
                    temp = iniFile.GetKeyValue("configuration", "ip");
                    ipAdd = temp.Trim();
                }
                if (iniFile.GetKeyValue("configuration", "port") != string.Empty)
                {
                    portNo = Convert.ToInt32(iniFile.GetKeyValue("configuration", "port"));
                }
                if (iniFile.GetKeyValue("configuration", "model") != string.Empty)
                {
                    temp = iniFile.GetKeyValue("configuration", "model");
                    devModel = temp.Trim();
                }
                if (iniFile.GetKeyValue("configuration", "device_no") != string.Empty)
                {
                    devNo = Convert.ToInt32(iniFile.GetKeyValue("configuration", "device_no"));
                }
                if (iniFile.GetKeyValue("configuration", "comm_key") != string.Empty)
                {
                    comm = Convert.ToInt32(iniFile.GetKeyValue("configuration", "comm_key"));
                }
                if (iniFile.GetKeyValue("configuration", "debug") != string.Empty)
                {
                    debug = Convert.ToInt32(iniFile.GetKeyValue("configuration", "debug"));
                }
                if (iniFile.GetKeyValue("db_settings", "serverIP") != string.Empty)
                {
                    server = iniFile.GetKeyValue("db_settings", "serverIP").Trim();
                }
                if (iniFile.GetKeyValue("db_settings", "serverKey") != string.Empty)
                {
                    temp = iniFile.GetKeyValue("db_settings", "serverKey").Trim();
                    if (temp.Contains("null") || temp == string.Empty || temp == null || temp == "")
                    {
                        psd = "";
                    }
                    else
                    {
                        psd = temp;
                    }
                }
                if (iniFile.GetKeyValue("db_settings", "serverIP") != string.Empty)
                {
                    dbLogFilePath = iniFile.GetKeyValue("db_settings", "dblogpath").Trim();
                }
                if (iniFile.GetKeyValue("limits", "employee_limit") != string.Empty)
                {
                    int temp_limit = Convert.ToInt32(iniFile.GetKeyValue("limits", "employee_limit"));
                    if (temp_limit < 100)
                    {
                        employee_limit = 100;
                    }
                    else if (temp_limit >= 100 && temp_limit < 1000)
                    {
                        employee_limit = 1000;
                    }
                    else if (temp_limit >= 1000 && temp_limit < 10000)
                    {
                        employee_limit = 10000;
                    }
                }
                if (debug > 0) Console.WriteLine("Setting read successfull..");
            }

            private static int readLogCount(string date)
            {
                int count = 0;
                string query = "SELECT log_count FROM brotecshrm.ohrm_fingerprint_log_count WHERE DATE=" + date;
                string result = dbConnect.Select(query);
                if (result != string.Empty && result != null && result != "")
                {
                    count = Convert.ToInt32(result);
                    return count;
                }
                else
                {
                    // Selecting MAX date from 'ohrm_fingerprint_log_count.date'
                    query = "select MAX(date(ohrm_fingerprint_log_count.date)) from brotecshrm.ohrm_fingerprint_log_count";
                    result = dbConnect.Select(query);
                    string maxDate = "";
                    if (result != string.Empty && result != null && result != "") maxDate = "'" + result + "'";

                    // Selecting TOTAL log_count from 'ohrm_fingerprint_log_count of MAX date'  
                    query = "SELECT log_count FROM brotecshrm.ohrm_fingerprint_log_count WHERE ohrm_fingerprint_log_count.date=" + maxDate;
                    result = dbConnect.Select(query);
                    if (result != string.Empty && result != null && result != "")
                    {
                        count = Convert.ToInt32(result);
                    }
                    else
                    {
                        count = -1;
                    }
                    return count;
                }
            }

            private static void setLogCount(int count, string date)
            {
                int id = 0;
                string query = "SELECT id FROM brotecshrm.ohrm_fingerprint_log_count WHERE DATE=" + date;
                string result = dbConnect.Select(query);
                if (result != string.Empty && result != null && result != "")
                {
                    id = Convert.ToInt32(result);
                    //UPDATE brotecshrm.ohrm_fingerprint_log_count SET log_count=x WHERE id=x
                    query = "UPDATE brotecshrm.ohrm_fingerprint_log_count SET log_count=" + count + " WHERE id=" + id;
                    if (dbConnect.Update(query) != true)
                    {
                        if (debug > 0) Console.WriteLine("Error accessing DB");
                    }
                }
                else
                {
                    query = "SELECT MAX(id) FROM brotecshrm.ohrm_fingerprint_log_count";
                    result = dbConnect.Select(query);
                    if (result != string.Empty && result != null && result != "")
                    {
                        id = Convert.ToInt32(result) + 1;
                    }
                    else
                    {
                        id = 1;
                    }
                    query = "INSERT INTO brotecshrm.ohrm_fingerprint_log_count (id,date,log_count) VALUES(" + id + "," + date + "," + count + ")";
                    try
                    {
                        if (dbConnect.Insert(query) != true)
                        {
                            if (debug > 0) Console.WriteLine("Error accessing DB");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (debug > 0) Console.WriteLine("error! {0}", ex);
                    }
                }
                /*
                string strCount = count.ToString();
                iniFile.Load(settingFilename);
                if (iniFile.SetKeyValue("records", "log_count", strCount) == false)
                {
                    if(debug>0) Console.WriteLine("log count write unsuccessful");
                }
                iniFile.Save(settingFilename);
                */
            }

            #region DeviceInfo nd ConnectionMethods

            static void getDeviceInfo()
            {
                int dwMachineNumber = 1;
                int dwInfo = 0;
                int dwValue = 0;
                F18.GetDeviceInfo(dwMachineNumber, dwInfo, dwValue);
            }

            static bool isTFTMachine()
            {
                int machineNumber = 1;
                return F18.IsTFTMachine(machineNumber);
            }

            static string getSDKVersion()
            {
                string SDK_Version = "";
                bool success = F18.GetSDKVersion(ref SDK_Version);
                return SDK_Version;
            }

            static string getFirmwareVersion()
            {
                string Firmware_Version = "";
                int machineNumber = 1;
                bool success = F18.GetFirmwareVersion(machineNumber, ref Firmware_Version);
                return Firmware_Version;
            }

            static string getSerialNumber(int machine_number)
            {
                string Serial_Number = "";
                bool success = F18.GetSerialNumber(machine_number, out Serial_Number);
                return Serial_Number;
            }

            static bool getConnected(string ipAddress, int port, zkemkeeper.CZKEM axCZKEM)
            {
                int idwErrorCode = 0;
                bool isConnected = false;
                try
                {
                    if (axCZKEM.Connect_Net(ipAddress, Convert.ToInt32(port)))
                    {
                        isConnected = true;
                        if (debug > 0) Console.WriteLine("\nDevice is connected Successfully");
                    }
                    else
                    {
                        axCZKEM.GetLastError(ref idwErrorCode);
                        Console.WriteLine("Device connection Failed" + idwErrorCode);
                    }
                }
                catch (Exception exc)
                {

                    //throw exc;
                    Console.WriteLine("Failed Registering The Device: \n Exception Details: \n " + exc);
                }
                return isConnected;
            }

            static void getDeviceTime(ref int year, ref int month, ref int day, ref int hour, ref int min, ref int sec)
            {
                int machineNumber = 1;
                bool success = F18.GetDeviceTime(machineNumber, ref year, ref month, ref day, ref hour, ref min, ref sec);
                //Console.WriteLine(success);
            }

            static void deleteLogDataByTimeBefore(string dateToDeleteLogTo)
            {
                int machineNumber = 1;
                string dateToDeleteLogFrom = "2018-01-01 23:59:59";
                bool success = F18.DeleteAttlogBetweenTheDate(machineNumber, dateToDeleteLogFrom, dateToDeleteLogTo);
                if (success && debug > 0)
                    Console.WriteLine("Old log deleted");
            }

            #endregion

            static void Main(string[] args)
            {

                //string TargetFileName = "setting.ini";
                //System.IO.File.Copy(settingFilename, TargetFileName, true);
                KickOff();

                void KickOff()
                {
                    try
                    {
                        StartApplication();
                    }
                    catch (Exception)
                    {
                        RestartApplication();
                    }
                }

                void StartApplication()
                {
                    #region Declaration

                    string softwareVersion = "Build: 1.1.20181031";
                lev1:
                    DateTime timeIni = DateTime.Now;
                    Initialize();
                    int machineId = 1;
                    string serialNumber = "";
                    string sdkVer = "";
                    string firmware = "";
                    string time = "";
                    string testValueForTestingDeviceConnection = "";
                    //string pattern = "yyyy-MM-dd HH:mm:ss";
                    int yr = 0;
                    int mth = 0;
                    int day_Renamed = 0;
                    int hr = 0;
                    int min = 0;
                    int sec = 0;
                    string system_date = "";
                    // PunchList 
                    var punchList = new List<dynamic>();
                    if (debug > 0) Console.WriteLine("Connecting to ZKTeco F18 Device...");
                    string ipAddr = "192.168.30.230";
                    int port = 4370;
                    bool isConnected = false;

                    //int enrollNo = 0;
                    int ver = 0;
                    //int io = 0;
                    //int work = 0;
                    int log = 0;
                    int deleteMonth = 0;
                    int deleteDay = 0;
                    int deleteYear = 0;
                    //int employee_id = 0;
                    //string tmpData = "";

                    string userlog = "";
                    string query = "";
                    string result = "";
                    string result2 = "";
                    //logcount = readLogCount(system_date);

                    DateTime timeCurrent = new DateTime();
                    int diff;
                    bool databaseConnected = dbConnect.TestConnection();

                    #endregion

                    if (!isConnected)
                    {
                        isConnected = getConnected(ipAddr, port, F18);
                    }
                    Console.WriteLine("\n\t\tF18 Fingerprint Data Colletor Program\n\t\tBROTECS TECNOLOGIES LIMITED\n\t\t" + softwareVersion + "\n\n");
                    if (isConnected)
                    {
                        Console.WriteLine("");

                        #region DeviceRelatedInfo

                        if (isTFTMachine())
                        {
                            Console.WriteLine("Device type:  TFT Machine");
                        }

                        serialNumber = getSerialNumber(machineId);
                        Console.WriteLine("Serial Number: " + serialNumber);

                        sdkVer = getSDKVersion();
                        Console.WriteLine("SDK Version: " + sdkVer);

                        firmware = getFirmwareVersion();
                        Console.WriteLine("Firmware Version: " + firmware);

                        getDeviceTime(ref yr, ref mth, ref day_Renamed, ref hr, ref min, ref sec);
                        DateTime systemDate = new DateTime(yr, mth, day_Renamed, hr, min, sec);
                        system_date = "'" + systemDate.ToString("yyyy-MM-dd", null as DateTimeFormatInfo) + "'";
                        time = "Device Date and Time: " + Convert.ToString(day_Renamed) + "/" + Convert.ToString(mth) + "/" + Convert.ToString(yr) + " " + Convert.ToString(hr) + ":" + Convert.ToString(min) + ":" + Convert.ToString(sec);
                        Console.WriteLine(time + "\n\n");

                        #endregion

                    }

                    while (!_continue)
                    {
                        Thread.Sleep(1000);
                        timeCurrent = DateTime.Now;
                        diff = (int)(timeCurrent - timeIni).TotalSeconds;

                        if (diff > (restartTime * 3600))  // convertInSeconds <- (restartTime*60*60)
                        {
                            try
                            {
                                F18.Disconnect();
                                if (!F18.IsTFTMachine(1))
                                {
                                    Console.WriteLine("Connection to ZKTeco F18 closing...");
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Connection Already Close");
                            }
                            _continue = true;
                            Thread.Sleep(5000);

                            goto lev1;
                        }

                        //New Codes
                        /* 
                         * statusId: 
                             1 Number of administrators  
                             2 Number of registered users 
                             3 Number of fingerprint templates on the machine
                             4 Number of Passwords
                             5 Number of Operation Records
                             6 Number of Attendence Records
                             7 Fingerprint template capacity 
                             8 User capacity 
                             9 Attendance record capacity 
                             10 Remaining Fingerprint template capacity
                             11 Remaining User capacity
                             12 Remaining Attendance Record capacity
                             21 Number of faces
                             22 Face Capacity 
                         */
                        log = 0;
                        int statusId = 6;
                        if (!F18.GetSDKVersion(ref testValueForTestingDeviceConnection))
                        {
                            getConnected(ipAddr, port, F18);
                            databaseConnected = dbConnect.TestConnection();
                        }
                        //Console.WriteLine("DB Connection: " + databaseConnected);

                        #region DataRetrieveFromDevice

                        if (databaseConnected)
                        {
                            if (F18.GetDeviceStatus(machineId, statusId, ref log))
                            {
                                Thread.Sleep(100);
                                //logcount = readLogCount(system_date);
                                string fromTime = ""; //for ReadTimeGLogData( machineId , fromTime , toTime)
                                string toTime = "";
                                DateTime currentTime = DateTime.Now;
                                DateTime DeviceTakingAccurateLogFrom = new DateTime(2018, 10, 15, 00, 00, 00);
                                string maxPunchTimeInDb = (dbConnect.Select("SELECT MAX( punch_time ) FROM brotecshrm.ohrm_finger_print_device_recorde"));
                                if (maxPunchTimeInDb != null)
                                {
                                    DateTime maxDateTimeInDB = DateTime.Parse(maxPunchTimeInDb);
                                    if (maxDateTimeInDB < DeviceTakingAccurateLogFrom)
                                    {
                                        fromTime = DeviceTakingAccurateLogFrom.ToString(" yyyy-MM-dd HH:mm:ss ");
                                    }
                                    else
                                        fromTime = (maxDateTimeInDB.AddSeconds(1)).ToString(" yyyy-MM-dd HH:mm:ss ");
                                }
                                else
                                {
                                    DateTime todayStartTime = new DateTime(2018, 10, 15, 00, 00, 00); // 1st day of Device's accurate data 
                                    fromTime = todayStartTime.ToString(" yyyy-MM-dd HH:mm:ss ");
                                }

                                DateTime todayFullTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 23, 59, 59);
                                toTime = todayFullTime.ToString(" yyyy-MM-dd HH:mm:ss ");
                                //Console.WriteLine(fromTime);
                                //Console.WriteLine(toTime);

                                //Deleting old log
                                if (currentTime.Month > 1)
                                {
                                    deleteMonth = currentTime.Month - 1;
                                    deleteYear = currentTime.Year;
                                }
                                else
                                {
                                    deleteMonth = 12;
                                    deleteYear = currentTime.Year - 1;
                                }
                                deleteDay = 1;
                                DateTime dateOfDeletingLogBefore = new DateTime(deleteYear, deleteMonth, deleteDay);
                                String dateOfDeleteLogBefore = dateOfDeletingLogBefore.ToString(" yyyy-MM-dd HH:mm:ss ");
                                deleteLogDataByTimeBefore(dateOfDeleteLogBefore);
                                //Deleting Operation Log
                                if (currentTime.Day == 1)
                                {
                                    if (F18.ReadAllSLogData(machineId) && F18.ClearSLog(machineId))
                                    {
                                        Console.WriteLine("Last Month\'s Operation Log is deleted.");
                                    }
                                }
                                //Reading Data From Machine
                                if (F18.ReadTimeGLogData(machineId, fromTime, toTime))
                                {
                                    int late = 0;
                                    int early_left = 0;
                                    string login_ip = "'" + ipAdd + "'";
                                    int workloc = 1;
                                    int late_excuse = 0;
                                    string work_hr = "'NULL'";

                                    Thread.Sleep(100);
                                    //F18.EnableDevice(machineId, false);//disable the device

                                    string sdwEnrollNumber = "";
                                    int idwVerifyMode = 0; // 1 = FingerPrint 4 = RFID
                                    int idwInOutMode = 0; // 0 = Check In , 1= Check Out
                                    int idwYear = 0;
                                    int idwMonth = 0;
                                    int idwDay = 0;
                                    int idwHour = 0;
                                    int idwMinute = 0;
                                    int idwSecond = 0;
                                    int idwWorkcode = 0;
                                    int state = -1;
                                    //This list storing device data and release device for normal operation. So no time delay for SQL operations. At First making it empty
                                    punchList.Clear();
                                    while (F18.SSR_GetGeneralLogData(machineId, out sdwEnrollNumber, out idwVerifyMode,
                                                out idwInOutMode, out idwYear, out idwMonth, out idwDay, out idwHour, out idwMinute, out idwSecond, ref idwWorkcode))//get records from the memory
                                    {
                                        state = -2;
                                        ver = idwVerifyMode;
                                        string UserID = sdwEnrollNumber;
                                        int EmployeeId = (Convert.ToInt32(sdwEnrollNumber) / 100) % employee_limit;
                                        string PunchTime = idwYear + "-" + idwMonth + "-" + idwDay + " " + idwHour + ":" + idwMinute + ":" + idwSecond;
                                        string VerifyState = "";
                                        int punchedDevice = idwWorkcode; //No way to know device / extension
                                        if (idwInOutMode == 0)
                                        {
                                            VerifyState = "PUNCHED IN";
                                            state = 0;
                                        }
                                        else if (idwInOutMode == 1)
                                        {
                                            VerifyState = "PUNCHED OUT";
                                            state = 1;
                                        }
                                        else
                                        {
                                            state = -1;
                                            VerifyState = "INVALID";
                                        }

                                        //dynamic list ->adding punches
                                        dynamic punchInstance = new punchDetails();
                                        punchInstance.state = state;
                                        punchInstance.userId = UserID;
                                        punchInstance.employeeId = EmployeeId;
                                        punchInstance.punchTime = PunchTime;
                                        punchInstance.punchedDevice = punchedDevice;
                                        punchInstance.verifyState = VerifyState;
                                        punchList.Add(punchInstance);

                                    }
                                    foreach (var punch in punchList)
                                    {
                                        state = punch.state;
                                        string UserID = punch.userId;
                                        int EmployeeId = punch.employeeId;
                                        string PunchTime = punch.punchTime;
                                        string VerifyState = punch.verifyState;
                                        int punchedDevice = punch.punchedDevice;
                                        int hrm_id = 404;
                                        try
                                        {
                                            hrm_id = Convert.ToInt32(dbConnect.Select("SELECT MAX(id) FROM ohrm_finger_print_device_recorde")) + 1;
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("Error While Convertion hrm_id. Details: \n" + ex);
                                        }

                                        query = "INSERT INTO brotecshrm.ohrm_finger_print_device_recorde (id, employee_id,employee_device_id,punch_time,punch_sate,punched_device) VALUES(" + hrm_id + "," + EmployeeId + "," + UserID + ", '" + PunchTime + "' , '" + VerifyState + "' ," + punchedDevice + ")";
                                        //Console.WriteLine(query);
                                        try
                                        {
                                            if (dbConnect.Insert(query) != true)
                                            {
                                                if (debug > 2) Console.WriteLine("Error accessing DB");
                                            }
                                            if (debug > 1) Console.WriteLine(query);
                                        }
                                        catch (Exception ex)
                                        {
                                            if (debug > 2) Console.WriteLine("error! {0}", ex);
                                        }

                                        /**
                                            * check btl_working_date for new entry
                                            */
                                        DateTime workDate = DateTime.Parse(PunchTime);
                                        string work_dateTime = "'" + PunchTime + "'";
                                        string work_date = workDate.ToString("yyyy-MM-dd"); // btl_working_date table only keeping date 
                                        work_date = "'" + work_date + "'";
                                        int workday_status = getWorkingDayStatus(work_date);

                                        query = "SELECT MAX(id) FROM btl_working_date WHERE workdate=" + work_date;
                                        result = dbConnect.Select(query);
                                        if (result != string.Empty && result != null && result != "")
                                        {
                                            // do nothing
                                        }
                                        else
                                        {
                                            query = "INSERT INTO brotecshrm.btl_working_date (workdate,status) VALUES(" + work_date + "," + workday_status + ")";
                                            try
                                            {
                                                if (dbConnect.Insert(query) != true)
                                                {
                                                    if (debug > 2) Console.WriteLine("Error accessing DB");
                                                }
                                                if (debug > 2) Console.WriteLine(query);
                                            }
                                            catch (Exception ex)
                                            {
                                                if (debug > 2) Console.WriteLine("error! {0}", ex);
                                            }
                                        }

                                        string usr_time = "'" + PunchTime + "'";
                                        string usr_outtime = "";
                                        string usr_intime = "";
                                        string utc_outtime = "";
                                        string out_note = "";
                                        string in_note = "";
                                        int out_ofset = 0;
                                        int in_ofset = 0;
                                        //int Office_Endtime = 17;

                                        #region PUNCH IN
                                        if (state == 0)// For Punch IN                             
                                        {
                                            late = 0;
                                            early_left = 0;
                                            usr_intime = usr_time;



                                            if (debug > 0) Console.WriteLine("PUNCH IN Employee ID: " + EmployeeId + " Punch Time: " + PunchTime);
                                            List<string>[] state_list = new List<string>[2];
                                            query = "SELECT MAX(id),state FROM brotecshrm.ohrm_attendance_record WHERE employee_id=" + EmployeeId + " AND DATE(punch_in_user_time)=" + work_date;
                                            state_list = dbConnect.Select(query, 2);

                                            if (state_list[1].Contains("PUNCHED OUT") || state_list[1].Contains("PUNCHED IN"))
                                            {
                                                // update previous punch out entry
                                                query = "SELECT MAX(id) FROM brotecshrm.ohrm_attendance_record WHERE employee_id=" + EmployeeId + " AND " + "DATE(punch_in_user_time)=" + work_date + " AND " + "state='PUNCHED OUT'";
                                                result = dbConnect.Select(query);
                                                if (result != string.Empty && result != null && result != "")// Punch OUT -> update
                                                {
                                                    hrm_id = Convert.ToInt32(result);
                                                    //Console.WriteLine("Work Hour: " + workDate.Hour);
                                                    if (workDate.Hour < Office_Endtime)  // Office end time: 6pm or 18:00
                                                    {
                                                        query = "UPDATE brotecshrm.ohrm_attendance_record SET earlyleft=" + early_left + " WHERE id=" + hrm_id;
                                                        try
                                                        {
                                                            if (dbConnect.Insert(query) != true)
                                                            {
                                                                if (debug > 2) Console.WriteLine("Error accessing DB");
                                                            }
                                                            if (debug > 1) Console.WriteLine(query);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            if (debug > 2) Console.WriteLine("eenrollNorror! {0}", ex);
                                                        }
                                                    }
                                                }

                                                // multiple punch in check
                                                //last PUNCH IN id:
                                                query = "SELECT MAX(id) FROM brotecshrm.ohrm_attendance_record WHERE employee_id=" + EmployeeId + " AND " + "DATE(punch_in_user_time)=" + work_date + " AND " + "state='PUNCHED IN'";
                                                result = dbConnect.Select(query);
                                                // last PUNCH OUT id:
                                                query = "SELECT MAX(id) FROM brotecshrm.ohrm_attendance_record WHERE employee_id=" + EmployeeId + " AND " + "DATE(punch_in_user_time)=" + work_date + " AND " + "state='PUNCHED OUT'";
                                                result2 = dbConnect.Select(query);
                                                int last_in_id = -99999999;
                                                int last_out_id = -99999999;
                                                if (result != string.Empty && result != null && result != "")// Punch OUT -> update
                                                {
                                                    last_in_id = Convert.ToInt32(result);
                                                    if (result2 != string.Empty && result2 != null && result2 != "")
                                                    {
                                                        last_out_id = Convert.ToInt32(result2);
                                                    }
                                                    if (last_in_id > last_out_id && last_out_id != -99999999)
                                                    {
                                                        //Console.WriteLine("Multiple PunchIn checking");
                                                        hrm_id = Convert.ToInt32(result);
                                                        // set prev in with penalty
                                                        // get timediff/2 for the penalty
                                                        query = "SELECT SEC_TO_TIME((TO_SECONDS(" + usr_intime + ") - TO_SECONDS(punch_in_user_time))/2) FROM brotecshrm.ohrm_attendance_record WHERE id=" + hrm_id;
                                                        result = dbConnect.Select(query);
                                                        //Console.WriteLine("SEC to TIME: " + result);
                                                        if (result != string.Empty && result != null && result != "")
                                                        {
                                                            // penalty time with the punch_in time to get punchout time
                                                            query = "SELECT DATE_FORMAT(DATE_ADD(punch_in_user_time,INTERVAL '" + result + "' HOUR_SECOND),'%Y-%m-%d %H:%i:%s') FROM brotecshrm.ohrm_attendance_record WHERE id=" + hrm_id;
                                                            result = dbConnect.Select(query);
                                                            //Console.WriteLine("Penalty Time : " + result);
                                                            if (result != string.Empty && result != null && result != "")
                                                            {
                                                                usr_outtime = "'" + result + "'";
                                                                string intime = result;
                                                                result = "";
                                                                query = "SELECT DATE_FORMAT(" + usr_outtime + ",'%Y-%m-%d %H:%i:%s') as utc_outtime";
                                                                result = dbConnect.Select(query);
                                                                utc_outtime = usr_outtime; // added ' ' from string value
                                                                                           // get work hr
                                                                query = "SELECT TIMEDIFF(" + usr_outtime + ",punch_in_user_time) FROM brotecshrm.ohrm_attendance_record WHERE id=" + hrm_id;
                                                                result = dbConnect.Select(query);
                                                                work_hr = null;
                                                                string penalty = result;
                                                                //Console.WriteLine("Work Hour : " + result);
                                                                if (result != string.Empty && result != null && result != "")
                                                                {
                                                                    work_hr = "'" + result + "'";
                                                                }
                                                                // update the prev data
                                                                query = "UPDATE brotecshrm.ohrm_attendance_record SET punch_out_utc_time=" + utc_outtime + ", punch_out_note='" + out_note + "', punch_out_time_offset=" + out_ofset + ", punch_out_user_time=" + usr_outtime + ", workHour=" + work_hr + ", state='PUNCHED OUT' WHERE id=" + hrm_id;
                                                                //Console.WriteLine("Updating previous punch");
                                                                if (dbConnect.Update(query) != true)
                                                                {
                                                                    if (debug > 2) Console.WriteLine("Error accessing DB");
                                                                }
                                                                else
                                                                {
                                                                    if (debug > 1) Console.WriteLine(query);
                                                                }

                                                                // throw report to hrm url {emp_id=&current_punchin&penalty}
                                                                string url_data = string.Format("emp_id={0}&current_punchin={1}&penalty={2}", EmployeeId, intime, penalty);
                                                                if (debug > 2)
                                                                {
                                                                    Console.WriteLine("~~~~~~~~~");
                                                                    Console.WriteLine("Throwing exception report to HRM URL: " + hrmPunchInExceptionURL + url_data);
                                                                    Console.WriteLine("~~~~~~~~~");
                                                                }
                                                            }
                                                        }
                                                    }

                                                }

                                            }
                                            else
                                            {
                                                if ((workDate.Hour > entrytime_HH) || (workDate.Hour == entrytime_HH && workDate.Minute > late_allowed_mins))// late punch in
                                                {
                                                    late = 1;
                                                    in_note = getLateState(EmployeeId, system_date, usr_intime);
                                                }
                                            }

                                            hrm_id = Convert.ToInt32(dbConnect.Select("SELECT MAX(id) FROM brotecshrm.ohrm_attendance_record"));
                                            // 
                                            // if
                                            //      the max id date is greater than the punched in date then dont insert the data
                                            // else
                                            //      insert the data
                                            // 
                                            query = "SELECT DATE(punch_in_user_time) FROM brotecshrm.ohrm_attendance_record WHERE id=" + hrm_id;
                                            result = dbConnect.Select(query);
                                            if (result != string.Empty && result != null && result != "")
                                            {
                                                //string punch_date = offsetDate.ToString("MM/dd/yyyy");// punched date
                                                DateTime dbMAXDate = DateTime.Parse(result);
                                                //DateTime.TryParseExact(result, "MM-dd-yyyy", null, DateTimeStyles.None, out dbMAXDate);
                                                if (dbMAXDate > workDate)
                                                {
                                                    continue;
                                                }
                                            }
                                            hrm_id += 1;
                                            utc_outtime = "'NULL'";
                                            out_ofset = 0;
                                            usr_outtime = "'NULL'";
                                            work_hr = "'NULL'";
                                            in_note = "'NULL'";
                                            out_note = "NULL";
                                            early_left = 0;
                                            workloc = 0;   // 0=Office 1=Outside
                                                           //work = "";
                                            query = "INSERT INTO brotecshrm.ohrm_attendance_record (id, employee_id,punch_in_utc_time,punch_in_note,punch_in_time_offset,punch_in_user_time,punch_out_utc_time,punch_out_note,punch_out_time_offset,punch_out_user_time,state,late,earlyleft,loginip,workloc,lateexcuse,workHour) VALUES("
                                                + hrm_id + "," + EmployeeId + "," + usr_intime + "," + in_note + "," + in_ofset + "," + work_dateTime + "," + utc_outtime
                                                + "," + out_note + "," + out_ofset + "," + usr_outtime + "," + "'" + VerifyState + "'" + "," + late + "," + early_left + "," + login_ip + "," + workloc + "," + late_excuse + "," + work_hr + ")";
                                            //Console.WriteLine("QUERY: " + query);
                                            try
                                            {
                                                if (dbConnect.Insert(query) != true)
                                                {
                                                    if (debug > 2) Console.WriteLine("Error accessing DB");
                                                }
                                                if (debug > 1) Console.WriteLine(query);
                                            }
                                            catch (Exception ex)
                                            {
                                                if (debug > 2) Console.WriteLine("error! {0}", ex);
                                            }

                                            in_note = "'NULL'";
                                        }
                                        #endregion
                                        #region PUNCH OUT
                                        else if (state == 1) //Check out
                                        {
                                            if (debug > 0) Console.WriteLine("PUNCH OUT Employee ID: " + EmployeeId + "Punch Time: " + PunchTime);
                                            query = "SELECT MAX(id) FROM brotecshrm.ohrm_attendance_record WHERE employee_id=" + EmployeeId + " AND " + "DATE(punch_in_user_time)=" + work_date + " AND " + "state= 'PUNCHED IN'";
                                            result = dbConnect.Select(query);
                                            //Console.WriteLine( "MAX ID: " + query  );
                                            //Console.WriteLine("Getting Last Punch In time : " + result);
                                            if (result != string.Empty && result != null && result != "")// Punch OUT -> update
                                            {
                                                hrm_id = Convert.ToInt32(result);
                                                query = "";
                                                utc_outtime = usr_time;
                                                out_ofset = 0;
                                                early_left = 0;
                                                usr_outtime = usr_time;
                                                usr_intime = "NULL";
                                                string stateStatus = "'" + VerifyState + "'";
                                                if (workDate.Hour < Office_Endtime)// early left
                                                {
                                                    early_left = 1;
                                                }
                                                query = "SELECT TIMEDIFF(" + usr_outtime + ",punch_in_user_time) FROM brotecshrm.ohrm_attendance_record WHERE id=" + hrm_id;
                                                result = dbConnect.Select(query);
                                                work_hr = null;
                                                if (result != string.Empty && result != null && result != "")
                                                {
                                                    work_hr = "'" + result + "'";
                                                }
                                                query = "UPDATE brotecshrm.ohrm_attendance_record SET punch_out_utc_time=" + usr_outtime + ", punch_out_note='" + out_note + "', punch_out_time_offset=" + out_ofset + ", punch_out_user_time=" + usr_outtime + ", workHour=" + work_hr + ", state=" + stateStatus + ", earlyleft=" + early_left + " WHERE id=" + hrm_id;
                                                if (dbConnect.Update(query) != true)
                                                {
                                                    if (debug > 2) Console.WriteLine("Error accessing DB");
                                                }
                                                else
                                                {
                                                    if (debug > 1) Console.WriteLine(query);
                                                }
                                            }
                                        }
                                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(logfile, true))
                                        {
                                            file.WriteLine(query);
                                        }
                                        //if (debug >= 0) Console.WriteLine("HRM employee id: {0}", EmployeeId);
                                        string punchVia = "";
                                        if (ver == 1) { punchVia = "FingerPrint"; }
                                        if (ver == 4) { punchVia = "Card"; }
                                        userlog = "EmployeeId: " + Convert.ToString(EmployeeId) + "  Date:" + usr_time + " Verify: " + punchVia + " " + VerifyState;
                                        if (debug >= 0) Console.WriteLine(userlog);
                                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(logfile, true))
                                        {
                                            file.WriteLine(userlog);
                                        }

                                    }
                                    #endregion
                                }
                                else
                                {
                                    F18.EnableDevice(machineId, true);//enable the device
                                    int idwErrorCode = 0;
                                    F18.GetLastError(ref idwErrorCode);
                                }
                            }
                            else
                            {
                                int idwErrorCode = 0;
                                F18.GetLastError(ref idwErrorCode);
                            }
                        }

                        #endregion

                    }
                }
                void RestartApplication()
                {
                    StartApplication();
                }
            }

            private static string getLateState(int empployeeID, string sysTemDate, string intime)
            {
                string actualLateTime = "";
                string lastAllowedInTime = "";// = "'" + sysTemDate + " 09:30:00'";
                int id = 0;
                int extTime = 0;

                // defined states
                string informed = "Informed";
                string extendedTime = "00:00:00";
                string informedExtended = "Informed (Extended: ";
                string notInformed = "'Not Informed'";
                string lateState = string.Empty;

                string informedTime = "";

                /*
                 *Turash
                 *Changes made 3
                 */
                //int late_allowed_mins = 45;
                //int entrytime_HH = 8;

                if (late_allowed_mins >= 60)
                {
                    entrytime_HH += (late_allowed_mins / 60);
                    late_allowed_mins = (late_allowed_mins % 60);
                }

                string PermittedTimeString = entrytime_HH + ":" + late_allowed_mins + ":00";


                /*
                 * Edit: Topu
                 * Date: July 22, 2013
                 * considering smstype field in blt_lateSMS_detail table
                 */
                // check if any late sms for tis employee smstype = 0
                string query = "SELECT MAX(id) FROM brotecshrm.blt_lateSMS_detail WHERE empID=" + empployeeID + " AND DATE(informedTS)=" + sysTemDate + " AND (smstype=0 OR smstype=2)";
                string result = dbConnect.Select(query);
                if (result != string.Empty && result != null && result != "")
                {
                    id = Convert.ToInt32(result);
                    // get infromed time
                    query = "SELECT DATE_FORMAT(informedTS,'%Y-%m%-%d %H:%i:%s') FROM brotecshrm.blt_lateSMS_detail WHERE id=" + id;
                    result = dbConnect.Select(query);
                    if (result != string.Empty && result != null && result != "")
                    {
                        informedTime = result;
                    }

                    //get last allowed time stamp
                    query = "SELECT DATE_FORMAT(TIMESTAMP(" + sysTemDate + ",'" + PermittedTimeString + "'),'%Y-%m%-%d %H:%i:%s')";
                    result = dbConnect.Select(query);
                    if (result != string.Empty && result != null && result != "")
                    {
                        lastAllowedInTime = "'" + result + "'";
                    }

                    // get actual Late Time
                    query = "SELECT TIMEDIFF(" + intime + "," + lastAllowedInTime + ")";
                    result = dbConnect.Select(query);
                    if (result != string.Empty && result != null && result != "")
                    {
                        actualLateTime = "'" + result + "'";
                        // get difference between actual Late Time and probable Entry informed by employee in second
                        query = "SELECT TIME_TO_SEC(TIMEDIFF(" + actualLateTime + ",probableEntry)) FROM brotecshrm.blt_lateSMS_detail WHERE id=" + id;
                        result = dbConnect.Select(query);
                        if (result != string.Empty && result != null && result != "")
                        {
                            extTime = Convert.ToInt32(result);
                            query = "SELECT TIME_FORMAT(ADDTIME('" + PermittedTimeString + "',probableEntry),'%H:%i:%s') FROM blt_lateSMS_detail WHERE id=" + id;
                            result = dbConnect.Select(query);
                            if (result != string.Empty && result != null && result != "")
                            {
                                informed = "Probable entry: " + result + ", Informed";
                            }
                            // check if time extended
                            if (extTime > 0)
                            {
                                // convert second to time(hh:mm:ss)
                                extendedTime = dbConnect.Select("SELECT SEC_TO_TIME(" + extTime + ")");
                                informedExtended = "'" + informed + " at " + informedTime + " (Extended: " + extendedTime + ")'";
                                lateState = informedExtended;
                            }
                            else
                            {
                                lateState = "'" + informed + " at " + informedTime + "'";
                            }
                        }
                    }
                }
                else
                {
                    lateState = notInformed;
                }
                return lateState;
            }

            private static int getWorkingDayStatus(string workingDay)
            {
                string query = "SELECT id FROM blt_weekend WHERE weekend=DATE_FORMAT(" + workingDay + ",'%W')";
                string result = dbConnect.Select(query);
                if (result != string.Empty && result != null && result != "")
                {
                    return 1;

                }
                else
                {
                    return 0;
                }
            }
        }
    }
}

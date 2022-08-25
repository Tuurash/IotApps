/*
    Last Update: 19 May 2022 
    Target Framework .net 5
    Developed By: Mohaimanul Haque Turash
    For: ZKTeco F18 Machine, SFace900
    Dependecies : MySql.Data.dll , zkemkeeper.dll (available in SDK)
*/

using AttendenceApp.Models;
using AttendenceApp.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AttendenceApp
{
    class Program
    {
        class FingerPrintDataCollector
        {
            #region Object Properties

            private const string version = "05-06-2022(stable) ";
            private static zkemkeeper.CZKEM F18 = new zkemkeeper.CZKEM();
            private static DBConnect dbConnect;
            public static SqliteDbContext localContext = new SqliteDbContext();

            private static int employee_limit = 0;
            private static readonly string psd = "";
            private static bool _continue;

            //Office In-Out Time
            private static readonly int Office_Endtime = 17;
            private static int late_allowed_mins = 10;
            private static int entrytime_HH = 9;

            private static int debug;
            public static int idwErrorCode = 0;
            private const string hrmPunchInExceptionURL = "http://192.168.30.252/brotecsHRM/autoscripts/punchException.php?";

            public static DateTime currentTime;
            public static DateTime systemDate;
            public static string system_date = "";
            //string pattern = "yyyy-MM-dd HH:mm:ss";
            public static int yr = 0;
            public static int mth = 0;
            public static int day_Renamed = 0;
            public static int hr = 0;
            public static int min = 0;
            public static int sec = 0;

            public static string softwareVersion = "version: 2.2.6";
            public static int machineId = 1;
            public static string serialNumber = "";
            public static string sdkVer = "";
            public static string firmware = "";
            public static string time = "";

            public static int ver = 0;
            public static bool isConnected = false;


            //RetriveData variables
            public static int log = 0;
            public static int deleteMonth = 0;
            public static int deleteDay = 0;
            public static int deleteYear = 0;

            public static string LogDialogue = "";
            public static string query = "";
            public static string result = "";
            public static string result2 = "";

            public static int late = 0;
            public static int early_left = 0;
            public static string login_ip;
            public static int workloc = 1;
            public static int late_excuse = 0;
            public static string work_hr = "'NULL'";

            public static string sdwEnrollNumber = "";
            public static int idwVerifyMode = 0; // 1 = FingerPrint 4 = RFID
            public static int idwInOutMode = 0; // 0 = Check In , 1= Check Out
            public static int idwYear = 0;
            public static int idwMonth = 0;
            public static int idwDay = 0;
            public static int idwHour = 0;
            public static int idwMinute = 0;
            public static int idwSecond = 0;
            public static int idwWorkcode = 0;
            public static int state = -1;

            public static int hrm_id = 404;

            public static string CLastPunchState = "";
            public static DateTime CLastPunchTime = DateTime.MinValue;
            public static int CLastID = 0;



            #endregion

            private static void Initialize()
            {
                Console.WriteLine("Initializing components...");
                try
                {
                    int commKey = 0;
                    F18.SetCommPassword(Convert.ToInt32(commKey));
                    dbConnect = new DBConnect("Server Ip goes Here", psd); //Main Server
                    _continue = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Db Connection Exception occured \n");
                }
            }

            #region DeviceInfo nd ConnectionMethods

            static string getDeviceSerialNo(int MachineNumber)
            {
                string serial = "";
                var aa = F18.GetSerialNumber(MachineNumber, out serial);
                return serial;
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
                bool isConnected = false;

                try
                {
                    isConnected = axCZKEM.Connect_Net(ipAddress, port);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Failed Registering The Device: \n Exception Details: \n " + exc);
                }

                if (isConnected == true)
                {
                    if (debug > 0) Console.WriteLine("\nDevice is connected Successfully");
                }
                else
                {
                    axCZKEM.GetLastError(ref idwErrorCode);
                    Console.WriteLine("Device connection Failed. Error Code: " + idwErrorCode);
                }
                return isConnected;
            }

            static DateTime getDeviceTime(ref int year, ref int month, ref int day, ref int hour, ref int min, ref int sec)
            {
                int machineNumber = 1;
                F18.GetDeviceTime(machineNumber, ref year, ref month, ref day, ref hour, ref min, ref sec);
                return new DateTime(year, month, day, hour, min, sec);
            }

            //Deleting All old punch logs
            static void deleteLogDataByTimeBefore(string dateToDeleteLogTo)
            {
                int machineNumber = 1;
                string dateToDeleteLogFrom = "2018-01-01 23:59:59";
                bool success = F18.DeleteAttlogBetweenTheDate(machineNumber, dateToDeleteLogFrom, dateToDeleteLogTo);
                if (success && debug > 0)
                    Console.WriteLine("Old log deleted");

                //Deleting Operation Logs works for both devices
                deleteAllOperationLogs(machineNumber);
            }

            static void deleteAllOperationLogs(int machineId)
            {
                try
                {
                    //Disbaling the device first
                    F18.EnableDevice(machineId, false);
                    var isDeleted = F18.ClearSLog(machineId);
                    if (isDeleted)
                    {
                        //All logs are deleted
                        F18.RefreshData(machineId);
                        Console.WriteLine("All Operation Logs deleted.");
                        Log.Information("All Operation Logs deleted. \n");
                    }

                    else
                        Console.WriteLine("All Operation Logs deletation Failed.");
                    F18.EnableDevice(machineId, true);
                }
                catch (Exception ex)
                { Console.WriteLine(ex); }
            }

            #endregion

            static void Main(string[] args)
            {
                currentTime = SyncTime.AcceptedTime();

                #region logging 

                //Serilog
                Log.Logger = new LoggerConfiguration()
                                    .MinimumLevel.Debug()
                                    .WriteTo.File("D:/AttendenceApplogs/AttendenceAppLog_Merged.txt")
                                    .CreateLogger();
                Log.Information("Device Initiated");

                #endregion

                Initialize();
                employee_limit = 1000;
                debug = 0;

                bool boolDbIsConnected = dbConnect.TestConnection();
                if (boolDbIsConnected)
                {
                    while (true)
                    {
                        InitiateDevice("192.168.30.230", 4370, 101); // 8f
                        InitiateDevice("192.168.30.231", 4370, 102); // 10f

                        if (isConnected != false)
                            DumpToMainServer();
                    }

                }
                else
                    Log.Error("Database connection Failed");
            }

            private static void InitiateDevice(string IP, int PORT, int Serial)
            {
                isConnected = getConnected(IP, PORT, F18);
                login_ip = IP;
                if (!isConnected)
                {
                    Task.Delay(10000).Wait();
                    isConnected = getConnected(IP, PORT, F18);
                    if (isConnected)
                        RetrieveData(getDeviceSerialNo(Serial), IP, PORT);
                }
                if (isConnected)
                {
                    getFirmwareVersion();
                    RetrieveData(getDeviceSerialNo(Serial), IP, PORT);
                }
                else
                {
                    Task.Delay(10000).Wait();
                    isConnected = getConnected(IP, PORT, F18);
                    if (isConnected)
                        RetrieveData(getDeviceSerialNo(Serial), IP, PORT);
                }
            }

            private static void RetrieveData(string SerialNumber, string IP, int PORT)
            {

                if (!isConnected)
                {
                    Task.Delay(10000).Wait();
                    isConnected = getConnected(IP, PORT, F18);
                }

                List<dynamic> punchList = new List<dynamic>();

                log = 0;
                int statusId = 6;

                string fromTime = ""; //for ReadTimeGLogData( machineId , fromTime , toTime)
                string toTime = DateTime.Now.AddHours(5).ToString(" yyyy-MM-dd HH:mm:ss ");
                string maxPunchTimeInDb = "";

                #region Get last Punch 

                DateTime DeviceTakingAccurateLogFrom = new DateTime(2022, 07, 26, 13, 57, 00);


                maxPunchTimeInDb = localContext.Localpunches.Where(x => x.DeviceName == SerialNumber)
                                                            .OrderByDescending(x => x.PunchTime).Select(x => x.PunchTime)
                                                            .FirstOrDefault().ToString();
                if (maxPunchTimeInDb != null)
                {
                    DateTime maxDateTimeInDB = DateTime.Parse(maxPunchTimeInDb);
                    if (maxDateTimeInDB < DeviceTakingAccurateLogFrom)
                        fromTime = DeviceTakingAccurateLogFrom.ToString(" yyyy-MM-dd HH:mm:ss ");
                    else
                        fromTime = (maxDateTimeInDB.AddSeconds(1)).ToString(" yyyy-MM-dd HH:mm:ss ");
                }

                #endregion

                bool deviceActive = false;

                #region Device Activation

                try
                {
                    deviceActive = F18.GetDeviceStatus(machineId, statusId, ref log);
                    if (!deviceActive)
                    {
                        try
                        {
                            isConnected = F18.Connect_Net("192.168.30.231", 4370);
                            deviceActive = F18.GetDeviceStatus(machineId, statusId, ref log);
                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine("Failed Registering The Device: \n Exception Details: \n " + exc);
                        }
                    }
                }
                catch (Exception ex)
                { Log.Error("Device Connection Error:" + ex); }

                #endregion

                if (deviceActive)  //if Active
                {
                    //Reading Data From Machine
                    bool existData = false;
                    try { existData = F18.ReadTimeGLogData(machineId, fromTime, toTime); }
                    catch (Exception ex)
                    {
                        Log.Error("Data Fetching Error:" + ex);
                    }

                    if (existData)
                    {
                        if (!String.IsNullOrEmpty(SerialNumber))
                        {
                            //Temporary list(punchlist) cleared
                            punchList.Clear();

                            while (F18.SSR_GetGeneralLogData(machineId, out sdwEnrollNumber, out idwVerifyMode,
                                        out idwInOutMode, out idwYear, out idwMonth, out idwDay, out idwHour, out idwMinute, out idwSecond, ref idwWorkcode))//get records from the memory
                            {
                                #region Raw Device Data

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
                                    Log.Information(currentTime + "  punchState IN status 0\n");
                                }
                                else if (idwInOutMode == 1)
                                {
                                    VerifyState = "PUNCHED OUT";
                                    state = 1;
                                    Log.Information(currentTime + "  punchState OUT status 1\n");
                                }
                                else
                                {
                                    state = -1;
                                    VerifyState = "INVALID";
                                    Log.Information(currentTime + "  punchState ERROR\n");
                                }

                                #endregion

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

                            Task.Delay(500).Wait();

                            foreach (var punch in punchList)
                            {
                                state = punch.state;
                                string UserID = punch.userId;
                                int EmployeeId = punch.employeeId;
                                string PunchTime = DateTime.Parse(punch.punchTime).ToString("yyyy-MM-dd HH:mm:ss");
                                string VerifyState = punch.verifyState;
                                int punchedDevice = punch.punchedDevice;

                                #region local Sqlite Insertion

                                try
                                {
                                    localContext.Localpunches.Add(new PunchListLocal()
                                    {
                                        state = state,
                                        UserID = UserID,
                                        EmployeeId = EmployeeId,
                                        PunchTime = DateTime.Parse(PunchTime),
                                        VerifyState = VerifyState,
                                        PunchedDevice = punchedDevice,
                                        DeviceName = SerialNumber,
                                        isSynced = false,
                                    }); ;

                                    localContext.SaveChanges();

                                    Console.WriteLine("Data Inserted Locally ID: " + EmployeeId + " Verify State: " + VerifyState + " Time: " + PunchTime + " Serial: " + SerialNumber);
                                    Log.Information("Data Inserted Locally ID: " + EmployeeId + " Verify State: " + VerifyState + " Time: " + PunchTime + " Serial: " + SerialNumber);

                                    //var showTemp = localContext.Localpunches.ToList();

                                    Task.Delay(1000).Wait();
                                }
                                catch (Exception exc)
                                {
                                    Log.Error(exc, "local Db Insertion Error");
                                }

                                #endregion
                            }

                            //Deleting old log
                            if (currentTime.Day == 1)
                            {
                                TimeSpan Tstart = TimeSpan.Parse("01:00"); // 1 AM
                                TimeSpan Tend = TimeSpan.Parse("02:00");   // 2 AM
                                TimeSpan Tnow = DateTime.Now.TimeOfDay;

                                if (Tstart <= Tend)
                                {
                                    if (Tnow >= Tstart && Tnow <= Tend)
                                    {
                                        // current time is between start and stop
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

                                        //Sqlite TableClear
                                        var deletableData = localContext.Localpunches.Where(x => x.PunchTime < currentTime).Skip(2);
                                        localContext.Localpunches.RemoveRange(deletableData);
                                        localContext.SaveChanges();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            private static void DumpToMainServer()
            {
                int IsUpdated = 0; //local Db Update flag
                PunchListLocal punch = new PunchListLocal();

                #region finger_print_device_recorde Insertion

                var punchListLocal = localContext.Localpunches.Where(x => x.isSynced == false).OrderBy(x => x.PunchTime).ToList();

                foreach (var p in punchListLocal)
                {
                    //SELECT * FROM brotecshrm.ohrm_finger_print_device_recorde WHERE employee_id=45 AND employee_device_id=12204509 AND punch_time='2013-06-13 14:48:03' AND punch_sate='PUNCHED OUT'
                    query = @"SELECT * FROM brotecshrm.ohrm_finger_print_device_recorde 
                            WHERE employee_id=" + p.EmployeeId + " AND employee_device_id=" + p.UserID + " AND punch_time= '" + p.PunchTime.ToString("yyyy-MM-dd HH:mm:ss") + "' AND punch_sate= '" + p.VerifyState + "'";
                    var isInsertedAlready = dbConnect.Select(query);

                    if (String.IsNullOrEmpty(isInsertedAlready))
                    {
                        query = @"INSERT INTO brotecshrm.ohrm_finger_print_device_recorde (employee_id,employee_device_id,punch_time,punch_sate,punched_device) " +
                                "VALUES(" + p.EmployeeId + "," + p.UserID + ", '" + p.PunchTime.ToString("yyyy-MM-dd HH:mm:ss") + "' , '" + p.VerifyState + "' ," + p.PunchedDevice + ")";

                        int eID = p.EmployeeId;
                        string uID = p.UserID;
                        DateTime pTime = p.PunchTime;
                        string vState = p.VerifyState;

                        try
                        {
                            bool isInserted = dbConnect.Insert(query);
                            if (isInserted)
                            {
                                try
                                {
                                    punch = (from x in localContext.Localpunches
                                             where x.EmployeeId == eID && x.UserID == uID && x.PunchTime == pTime && x.VerifyState == vState
                                             select x).First();
                                    punch.isSynced = true;
                                    IsUpdated = localContext.SaveChanges();


                                    Task.Delay(1000).Wait();

                                    if (IsUpdated != 1)
                                    {
                                        punch = (from x in localContext.Localpunches
                                                 where x.EmployeeId == eID && x.UserID == uID && x.PunchTime == pTime && x.VerifyState == vState
                                                 select x).First();
                                        punch.isSynced = true;
                                        IsUpdated = localContext.SaveChanges();
                                        Task.Delay(1000).Wait();
                                    }
                                }
                                catch (Exception exc)
                                {
                                    Log.Error(exc, "Local Db Update failed");
                                }

                                if (isInserted && IsUpdated != 1)
                                {
                                    localContext.Localpunches.Remove(punch);
                                    Task.Delay(1000).Wait();
                                }

                                Log.Information("Insertion Success Id: Device Id: " + p.UserID + "  employee_id: " + p.EmployeeId);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Exception Occured \n");
                        }

                        #region Process Information

                        DateTime workDate = p.PunchTime;
                        string work_dateTime = "'" + p.PunchTime.ToString("yyyy-MM-dd HH:mm:ss") + "'";
                        string work_date = workDate.ToString("yyyy-MM-dd"); // btl_working_date table only keeping date 
                        work_date = "'" + work_date + "'";
                        int workday_status = getWorkingDayStatus(work_date);

                        query = "SELECT MAX(id) FROM btl_working_date WHERE workdate=" + work_date;
                        result = dbConnect.Select(query);

                        if (!String.IsNullOrEmpty(result))
                        {

                            query = "INSERT INTO brotecshrm.btl_working_date (workdate,status) VALUES(" + work_date + "," + workday_status + ")";
                            try
                            {
                                dbConnect.Insert(query);
                            }
                            catch (Exception) { }

                        }

                        string usr_time = "'" + p.PunchTime.ToString("yyyy-MM-dd HH:mm:ss") + "'";
                        string usr_outtime = "";
                        string usr_intime = "";
                        string utc_outtime = "";
                        string out_note = "";
                        string in_note = "";
                        int out_ofset = 0;
                        int in_ofset = 0;

                        #region PUNCH IN

                        if (vState == "PUNCHED IN")// For Punch IN                             
                        {
                            state = 0;
                            late = 0;
                            early_left = 0;
                            usr_intime = usr_time;


                            //query = @"SELECT * FROM brotecshrm.ohrm_attendance_record WHERE 
                            //        employee_id = " + p.EmployeeId + " AND DATE(punch_in_user_time)= " + work_date + " AND punch_out_utc_time = '0000-00-00 00:00:00'";

                            //string value = dbConnect.Select(query);

                            List<string>[] state_list = new List<string>[2];
                            query = "SELECT MAX(id),state FROM brotecshrm.ohrm_attendance_record WHERE employee_id=" + p.EmployeeId + " AND DATE(punch_in_user_time)=" + work_date;
                            state_list = dbConnect.Select(query, 2);

                            if (state_list[1].Contains("PUNCHED OUT") || state_list[1].Contains("PUNCHED IN"))
                            {
                                // Early Left Condition [ignored]
                                // Also in line 789

                                //query = "SELECT MAX(id) FROM brotecshrm.ohrm_attendance_record WHERE employee_id=" + p.EmployeeId + " AND " + "DATE(punch_in_user_time)=" + work_date + " AND " + "state='PUNCHED OUT'";
                                //result = dbConnect.Select(query);
                                //if (result != string.Empty && result != null && result != "")// Punch OUT -> update  //hrm_id=result=p.EmployeeId
                                //{
                                //    hrm_id = Convert.ToInt32(result);
                                //    //Console.WriteLine("Work Hour: " + workDate.Hour);
                                //    if (workDate.Hour < Office_Endtime)  // Office end time: 5pm or 17:00
                                //    {
                                //        query = "UPDATE brotecshrm.ohrm_attendance_record SET earlyleft=" + early_left + " WHERE id=" + hrm_id;
                                //        try { dbConnect.Insert(query); }
                                //        catch (Exception ex)
                                //        {
                                //            Log.Error(ex, currentTime + "  ENROLL Exception occured \n");
                                //        }
                                //    }
                                //}

                                // multiple punch in check
                                query = "SELECT MAX(id) FROM brotecshrm.ohrm_attendance_record WHERE employee_id=" + p.EmployeeId + " AND " + "DATE(punch_in_user_time)=" + work_date + " AND " + "state='PUNCHED IN'";
                                result = dbConnect.Select(query);
                                // last PUNCH OUT id:
                                query = "SELECT MAX(id) FROM brotecshrm.ohrm_attendance_record WHERE employee_id=" + p.EmployeeId + " AND " + "DATE(punch_in_user_time)=" + work_date + " AND " + "state='PUNCHED OUT'";
                                result2 = dbConnect.Select(query);
                                int last_in_id = -99999999;
                                int last_out_id = -99999999;
                                if (!String.IsNullOrEmpty(result2)) //update-> Punch OUT
                                {
                                    last_in_id = Convert.ToInt32(result);
                                    if (result2 != string.Empty && result2 != null && result2 != "")
                                        last_out_id = Convert.ToInt32(result2);
                                    if (last_in_id > last_out_id && last_out_id != -99999999)
                                    {
                                        hrm_id = Convert.ToInt32(result);

                                        /*
                                         * set prev in with penalty
                                         *  get timediff/2 for the penalty
                                            query = "SELECT SEC_TO_TIME((TO_SECONDS(" + usr_intime + ") - TO_SECONDS(punch_in_user_time))/2) FROM brotecshrm.ohrm_attendance_record WHERE id=" + hrm_id;
                                         * Turash
                                         *  removed penalty time
                                         */

                                        query = "SELECT SEC_TO_TIME((TO_SECONDS(" + usr_intime + ") - TO_SECONDS(punch_in_user_time))) FROM brotecshrm.ohrm_attendance_record WHERE id=" + hrm_id;
                                        result = dbConnect.Select(query); //Double punchin Interval 
                                        if (!String.IsNullOrEmpty(result))
                                        {
                                            query = "SELECT DATE_FORMAT(DATE_ADD(punch_in_user_time,INTERVAL '" + result + "' HOUR_SECOND),'%Y-%m-%d %H:%i:%s') FROM brotecshrm.ohrm_attendance_record WHERE id=" + hrm_id;
                                            result = dbConnect.Select(query); //Double punchin Interval Formatted

                                            //Counting Workhour
                                            if (!String.IsNullOrEmpty(result))
                                            {
                                                usr_outtime = "'" + result + "'";
                                                string intime = result;
                                                result = "";
                                                query = "SELECT DATE_FORMAT(" + usr_outtime + ",'%Y-%m-%d %H:%i:%s') as utc_outtime";
                                                result = dbConnect.Select(query); // last punch in Updated as out
                                                utc_outtime = usr_outtime;

                                                query = "SELECT TIMEDIFF(" + usr_outtime + ",punch_in_user_time) FROM brotecshrm.ohrm_attendance_record WHERE id=" + hrm_id;
                                                result = dbConnect.Select(query);
                                                work_hr = null;

                                                if (!String.IsNullOrEmpty(result))
                                                {
                                                    work_hr = "'" + result + "'";
                                                    Console.WriteLine("Work hour: " + work_hr);
                                                }
                                                // update the prev data
                                                query = @"UPDATE brotecshrm.ohrm_attendance_record 
                                                    SET punch_out_utc_time=" + utc_outtime + ", punch_out_note='" + out_note + "', punch_out_time_offset=" + out_ofset + ", punch_out_user_time=" + usr_outtime + ", workHour=" + work_hr + ", state='PUNCHED OUT' WHERE id=" + hrm_id;

                                                try { dbConnect.Update(query); }
                                                catch (Exception exc)
                                                {
                                                    Log.Error(exc, "Error Updating punch Data\n");
                                                }

                                                // throw report to hrm url {emp_id=&current_punchin&penalty}
                                                string url_data = string.Format("emp_id={0}&current_punchin={1}&penalty={2}", p.EmployeeId, intime, work_hr);
                                                Log.Information("Report to HRM URL: " + hrmPunchInExceptionURL + url_data);
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
                                    system_date = "'" + getDeviceTime(ref yr, ref mth, ref day_Renamed, ref hr, ref min, ref sec).ToString("yyyy-MM-dd", null) + "'";
                                    in_note = getLateState(p.EmployeeId, system_date, usr_intime);
                                }
                            }

                            hrm_id = Convert.ToInt32(dbConnect.Select("SELECT MAX(id) FROM brotecshrm.ohrm_attendance_record"));

                            // if the max id date is greater than the punched in date then dont insert the data
                            // else insert the data
                            query = "SELECT DATE(punch_in_user_time) FROM brotecshrm.ohrm_attendance_record WHERE id=" + hrm_id;
                            result = dbConnect.Select(query);
                            if (!String.IsNullOrEmpty(result)) //Ignoring Date
                            {
                                DateTime dbMAXDate = DateTime.Parse(result);
                                if (dbMAXDate > workDate)
                                    continue;
                            }

                            utc_outtime = "'NULL'";
                            out_ofset = 0;
                            usr_outtime = "'NULL'";
                            work_hr = "'NULL'";
                            in_note = "'NULL'";
                            out_note = "NULL";
                            early_left = 0;
                            workloc = 0;   // 0=Office 1=Outside

                            query = @"INSERT INTO brotecshrm.ohrm_attendance_record 
                                (employee_id,punch_in_utc_time,punch_in_note,punch_in_time_offset,punch_in_user_time,punch_out_utc_time,punch_out_note,punch_out_time_offset,punch_out_user_time,state,late,earlyleft,loginip,workloc,lateexcuse,workHour) VALUES("
                                     + p.EmployeeId + "," + usr_intime + "," + in_note + "," + in_ofset + "," + work_dateTime + "," + utc_outtime + "," + out_note + "," + out_ofset + "," + usr_outtime + "," + "'" + p.VerifyState + "'" + "," + late + "," + early_left + "," + "'" + login_ip + "'" + "," + workloc + "," + late_excuse + "," + work_hr + ")";

                            try
                            {
                                var isDbInserted = dbConnect.Insert(query);

                                //if (CLastID==p.Id && (currentTime - CLastPunchTime).TotalSeconds < 1 && p.VerifyState == CLastPunchState) { }
                                //else {
                                //    var isDbInserted = dbConnect.Insert(query);
                                //    if (isDbInserted)
                                //    {
                                //        CLastPunchState = p.VerifyState;
                                //        CLastPunchTime = currentTime;
                                //        CLastID = p.Id;
                                //    }
                                //}
                            }
                            catch (Exception exc)
                            {
                                Log.Error(exc, "Exception occured \n");
                            }

                        }

                        #endregion

                        #region PUNCH OUT

                        else if (vState == "PUNCHED OUT") //Check out
                        {
                            state = 1;
                            query = "SELECT MAX(id) FROM brotecshrm.ohrm_attendance_record WHERE employee_id=" + p.EmployeeId + " AND " + "DATE(punch_in_user_time)=" + work_date + " AND " + "state= 'PUNCHED IN'";
                            result = dbConnect.Select(query);

                            if (!String.IsNullOrEmpty(result))// Punch OUT -> update
                            {
                                hrm_id = Convert.ToInt32(result);
                                query = "";
                                utc_outtime = usr_time;
                                out_ofset = 0;
                                early_left = 0;
                                usr_outtime = usr_time;
                                usr_intime = "NULL";
                                string stateStatus = "'" + p.VerifyState + "'";
                                //if (workDate.Hour < Office_Endtime)// early left
                                //    early_left = 0;

                                query = "SELECT TIMEDIFF(" + usr_outtime + ",punch_in_user_time) FROM brotecshrm.ohrm_attendance_record WHERE id=" + hrm_id;
                                result = dbConnect.Select(query);
                                work_hr = null;
                                if (!String.IsNullOrEmpty(result))
                                    work_hr = "'" + result + "'";

                                query = @"UPDATE brotecshrm.ohrm_attendance_record 
                                    SET punch_out_utc_time=" + usr_outtime + ", punch_out_note='" + out_note + "', punch_out_time_offset=" + out_ofset + ", punch_out_user_time=" + usr_outtime + ", workHour=" + work_hr + ", state=" + stateStatus + ", earlyleft=" + early_left + " WHERE id=" + hrm_id;
                                try { dbConnect.Update(query); }
                                catch (Exception ex) { Log.Error(ex, currentTime + " Exception occured \n"); }
                            }
                        }

                        #endregion

                        string punchVia = "";
                        if (ver == 1) { punchVia = "FingerPrint"; }
                        if (ver == 4) { punchVia = "Card"; }
                        LogDialogue = "EmployeeId: " + Convert.ToString(p.EmployeeId) + "  Punch Time:" + usr_time + " Verify: " + punchVia + " " + p.VerifyState;
                        Console.WriteLine(LogDialogue);
                        Log.Information(LogDialogue + "\n");

                        #endregion
                    }
                    else
                    {
                        Log.Error("[Data Repeatation]  Already Exists. employee_id=" + p.EmployeeId + " AND employee_device_id=" + p.UserID + " AND punch_time= '" + p.PunchTime.ToString("yyyy-MM-dd HH:mm:ss") + "' AND punch_sate= '" + p.VerifyState + "'");
                    }
                }

                #endregion
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

                if (late_allowed_mins >= 60)
                {
                    entrytime_HH += (late_allowed_mins / 60);
                    late_allowed_mins = (late_allowed_mins % 60);
                }

                string PermittedTimeString = entrytime_HH + ":" + late_allowed_mins + ":00";

                // check if any late sms for tis employee smstype = 0
                string query = @"SELECT MAX(id) FROM brotecshrm.blt_lateSMS_detail WHERE empID=" + empployeeID + " AND DATE(informedTS)=" + sysTemDate + " AND (smstype=0 OR smstype=2)";
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
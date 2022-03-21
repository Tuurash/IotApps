/*
 *  Project Initiation: 21 March 2022
    Target Framework .net 5
    Developed By: Mohaimanul Haque Turash
    For: ZKTeco F18 Machine/ SFace900
    Dependecies : MySql.Data.dll , zkemkeeper.dll (available in SDK)
 */


using AttendenceApp.Data;
using AttendenceApp.Models;
using AttendenceApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace AttendenceApp
{
    class Program
    {
        //sdk
        private static zkemkeeper.CZKEM F18;
        private static CredsConfig context;
        private static DataServices dataServices;


        static void Main(string[] args)
        {
            #region SystemConfigurationsLocal

            CredsConfig SystemConfigurationLocal = new CredsConfig();

            Console.WriteLine("WElCOME.\n\n");

            if (File.Exists("LDB.db"))
            {
                Console.WriteLine("Existing Configaration File Exists.");

                Console.WriteLine("Press 1 to Check.");
                Console.WriteLine("Press 2 to ReConfigure");
                int a = int.Parse(Console.ReadLine());

                if (a == 1)
                {
                    ShowDBInfo();
                    ShowDeviceInfo();
                    ShowLimitInfo();
                    ShowLRecordsInfo();

                    Console.WriteLine("\n Press 2 to Change Database Info");
                    Console.WriteLine("\n Press 3 to Change Device Info");
                    Console.WriteLine("\n Press 4 to Change Limit Info");
                    Console.WriteLine("\n Press 5 to Change Records Info");
                    Console.WriteLine("\n Press 1 to Load the Config");
                    int ab = int.Parse(Console.ReadLine());
                    if (ab == 2)
                    { SetupDbInfo(); }
                    else if (ab == 3)
                    { SetupDevice(); }
                    else if (ab == 4)
                    { SetupLimit(); }
                    else if (ab == 5)
                    { SetupRecords(); }
                    else if (ab == 1)
                    {
                        Console.WriteLine("Config Loadig.");
                    }
                }
                else if (a == 2)
                {
                    SetupSystem();
                }
                else
                {
                    Console.WriteLine("\n Not Understood.Config Loadig.");
                }

            }
            else
            {
                SystemConfigurationLocal.Database.EnsureCreated();
                Console.WriteLine("No Configuration found. Please Setup for the First time.");

                SetupSystem();
            }


            #endregion

            //ConnectRemoteDatabase();
            bool IsDeviceConnected = ConnectDevice();

            if (IsDeviceConnected)
            {
                //Retrieve Data From Device
                RetrieDeviceDatas();
            }
        }

        private static DeviceConfiguration GlobalDeviceConfigInfos;

        private static bool ConnectDevice()
        {
            bool isConnected = false;

            context = new CredsConfig();

            DeviceConfiguration device = context.DeviceConfiguration.First();
            GlobalDeviceConfigInfos = device;

            if (device != null)
            {
                F18 = new zkemkeeper.CZKEM();   // Creating ZK F18 instance
                F18.SetCommPassword(device.CommKey);


                int idwErrorCode = 0;

                try
                {
                    if (F18.Connect_Net(device.Ip, device.Port))
                    {
                        isConnected = true;
                    }
                    else
                    {
                        F18.GetLastError(ref idwErrorCode);
                        Console.WriteLine("Device connection Failed" + idwErrorCode);
                    }
                }
                catch (Exception exc)
                {

                    //throw exc;
                    Console.WriteLine("Failed Registering The Device: \n Exception Details: \n " + exc);
                }
            }
            return isConnected;
        }

        private static void RetrieDeviceDatas()
        {
            string ipAdd = "192.168.30.230";
            var punchList = new List<dynamic>();
            int ver = 0;

            int log = 0;
            int deleteMonth = 0;
            int deleteDay = 0;
            int deleteYear = 0;

            int machineId = 1;



            dataServices = new DataServices();

            Thread.Sleep(100);

            string fromTime = ""; //for ReadTimeGLogData( machineId , fromTime , toTime)
            string toTime = "";

            DateTime MaxPunchInTime = dataServices.GetMaxPunchinTime();



            DateTime currentTime = DateTime.Now;
            DateTime DeviceTakingAccurateLogFrom = new DateTime(2018, 10, 15, 00, 00, 00);

            if (MaxPunchInTime != null)
            {
                if (MaxPunchInTime > DeviceTakingAccurateLogFrom)
                {
                    fromTime = DeviceTakingAccurateLogFrom.ToString(" yyyy-MM-dd HH:mm:ss ");
                }
                else { fromTime = (MaxPunchInTime.AddSeconds(1)).ToString(" yyyy-MM-dd HH:mm:ss "); }
            }
            else
            {
                DateTime todayStartTime = new DateTime(2018, 10, 15, 00, 00, 00); // 1st day of Device's accurate data 
                fromTime = todayStartTime.ToString(" yyyy-MM-dd HH:mm:ss ");
            }

            DateTime todayFullTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 23, 59, 59);
            toTime = todayFullTime.ToString(" yyyy-MM-dd HH:mm:ss ");

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
            DeleteLogDataByTimeBefore(dateOfDeleteLogBefore);
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
                    int hrm_id = 404;

                    ohrm_finger_print_device_recorde record = new ohrm_finger_print_device_recorde
                    {
                        Id = hrm_id,
                        EmployeeId = punch.employeeId,
                        EmployeeDeviceId = punch.userId,
                        PunchTime = punch.punchTime,
                        PunchSate = punch.verifyState,
                        PunchedDevice = punch.punchedDevice,
                    };
                    //Inserting PunchData
                    var InsertStatus = dataServices.InsertDeviceRecord(record);

                    /**
                        * check btl_working_date for new entry
                        */
                    DateTime workDate = DateTime.Parse(punch.punchTime);
                    string work_dateTime = "'" + punch.punchTime + "'";
                    string work_date = workDate.ToString("yyyy-MM-dd"); // btl_working_date table only keeping date 
                    work_date = "'" + work_date + "'";
                    int workday_status = GetWorkingDayStatus(work_date);

                    string MaxIdByDate = dataServices.GetMaxIdByWorkingDate(work_date);

                    if (String.IsNullOrEmpty(MaxIdByDate))
                    {
                        btl_working_date wdRecord = new btl_working_date
                        {
                            Workdate = work_date,
                            Status = workday_status,
                        };

                        bool WDRecordInsertionStatus = dataServices.InsertWorkDayRecord(wdRecord);
                    }

                    string usr_time = "'" + punch.punchTime + "'";
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



                        if (GlobalDeviceConfigInfos.IsDebug > 0) Console.WriteLine("PUNCH IN Employee ID: " + punch.employeeId + " Punch Time: " + punch.punchTime);
                        List<string>[] state_list = new List<string>[2];

                        state_list = dataServices.GetLast2StateList(work_date, punch.employeeId);

                        //line 735 Till Here
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

        private static int GetWorkingDayStatus(string workingDay)
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

        private static void DeleteLogDataByTimeBefore(string dateOfDeleteLogBefore)
        {
            int machineNumber = 1;
            string dateToDeleteLogFrom = "2018-01-01 23:59:59";
            bool success = F18.DeleteAttlogBetweenTheDate(machineNumber, dateToDeleteLogFrom, dateToDeleteLogTo);
            if (success)
                Console.WriteLine("Old log deleted");
        }

        private static void SetupSystem()
        {
            SetupDevice();
            SetupRecords();
            SetupLimit();
            SetupDbInfo();
        }

        #region DatabaseInfo

        private static void SetupDbInfo()
        {
            CredsConfig context = new CredsConfig();

            //context.Database.ExecuteSqlCommand("TRUNCATE TABLE [TableName]");
            context.MySqlConfigs.RemoveRange(context.MySqlConfigs);
            context.SaveChanges();

            Console.WriteLine("Database SETUP:\n");
            Console.WriteLine("Input Server IP");
            string serverIp = Console.ReadLine();

            MySqlConfigs mySqlConfigs = new MySqlConfigs
            {
                ServerIp = serverIp,
                Key = null,
                DatabaseName = "brotecshrm",
                Uid = "root",
                Port = "3306",
                Password = "",
            };

            context.Add(mySqlConfigs);
            context.SaveChanges();

            Console.WriteLine("\n Database Setup Complete. ");

            ShowDBInfo();
        }

        private static void ShowDBInfo()
        {

            CredsConfig context = new CredsConfig();
            var dbInfo = context.MySqlConfigs.First();
            Console.WriteLine("Database Server Ip: " + dbInfo.ServerIp);
        }

        #endregion

        #region Limit
        private static void SetupLimit()
        {
            CredsConfig context = new CredsConfig();


            context.EmployeeDirectives.RemoveRange(context.EmployeeDirectives);
            context.SaveChanges();

            Console.WriteLine("Limits SETUP:\n");
            Console.WriteLine("Input Employee Limit");
            int eLimit = int.Parse(Console.ReadLine());

            EmployeeDirectives employeeDirectives = new EmployeeDirectives
            {
                EmployeeLimit = eLimit,
            };

            context.Add(employeeDirectives);
            context.SaveChanges();

            Console.WriteLine("\n Limits Setup Complete. ");

            ShowLimitInfo();
        }

        private static void ShowLimitInfo()
        {
            CredsConfig context = new CredsConfig();
            var employeeDirectives = context.EmployeeDirectives.First();
            Console.WriteLine("Employee Limit: " + employeeDirectives.EmployeeLimit);
        }

        #endregion

        #region Records

        private static void SetupRecords()
        {
            CredsConfig context = new CredsConfig();

            context.LRecords.RemoveRange(context.LRecords);
            context.SaveChanges();

            Console.WriteLine("Records SETUP:\n");
            Console.WriteLine("Input Log Counts");
            int logCounts = int.Parse(Console.ReadLine());

            LRecords logRecords = new LRecords
            {
                LogCount = logCounts,
            };


            context.Add(logRecords);
            context.SaveChanges();

            Console.WriteLine("\n Records Setup Complete. ");

            ShowLRecordsInfo();
        }

        private static void ShowLRecordsInfo()
        {
            CredsConfig context = new CredsConfig();
            var logInfo = context.LRecords.First();
            Console.WriteLine("Records Count: " + logInfo.LogCount);
        }

        #endregion

        #region DeviceInfo

        private static void SetupDevice()
        {
            CredsConfig SystemConfigurationLocal = new CredsConfig();

            SystemConfigurationLocal.DeviceConfiguration.RemoveRange(SystemConfigurationLocal.DeviceConfiguration);
            SystemConfigurationLocal.SaveChanges();

            int deviceNo = 1;
            Console.WriteLine("DEVICE SETUP:\n");
            Console.WriteLine("Input Device IP No: i.e:  192.168.30.200");
            string ipNo = Console.ReadLine();

            Console.WriteLine("Input Device Port No: i.e: 4370");
            int portNo = int.Parse(Console.ReadLine());

            Console.WriteLine("Input Device Model No: i.e: R2");
            string model = Console.ReadLine();

            if (model == "R2")
            {
                deviceNo = 1;
            }
            else
            {
                Console.WriteLine("Input Device No: i.e: 2");
                deviceNo = int.Parse(Console.ReadLine());
            }

            Console.WriteLine("Input Comm Key: i.e: 1234");
            int commKey = int.Parse(Console.ReadLine());

            DeviceConfiguration deviceConfiguration = new DeviceConfiguration
            {
                Port = portNo,
                Ip = ipNo,
                DeviceNo = deviceNo,
                Model = model,
                CommKey = commKey,
                IsDebug = 0
            };

            SystemConfigurationLocal.Add(deviceConfiguration);
            SystemConfigurationLocal.SaveChanges();

            Console.WriteLine("\nDevice Setup Complete. ");

            ShowDeviceInfo();
        }

        private static void ShowDeviceInfo()
        {
            CredsConfig context = new CredsConfig();

            var deviceInfo = context.DeviceConfiguration.First();

            Console.WriteLine("Device Model No: " + deviceInfo.Model);
            Console.WriteLine("Device IP: " + deviceInfo.Ip);
            Console.WriteLine("Device Port: " + deviceInfo.Port);
            Console.WriteLine("Device Comm Key: " + deviceInfo.CommKey);
            if (deviceInfo.IsDebug == 0)
            {
                Console.WriteLine("Debug Mode: Yes");
            }
            else { Console.WriteLine("Debug Mode: No"); }
            Console.WriteLine("Device No: " + deviceInfo.DeviceNo);
        }
        #endregion
    }
}

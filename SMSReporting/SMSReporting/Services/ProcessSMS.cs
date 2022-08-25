using Sentry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BrotecsLateSMSReporting.Services
{
    public class ProcessSMS
    {
        DBHandler dbHandler = new DBHandler();
        string qry = "";

        private static int time_lmt_hr = 9;
        private static int time_lmt_mn = 30;

        private HttpPost httpPost = new HttpPost();

        public int authenticateSender(string number) => dbHandler.GetEmployeeID(number);
        List<string> collectNumsFromHRD_DB() => dbHandler.getAllActiveEmployeePhoneNumbers();


        #region SMS Parser

        enum SMStype
        {
            late = 0,
            leave = 1,
            excuse = 2,
            web_enable_disable = 3,
            notice = 4,
            invalid = -1,
        };

        public int getAdminID() => 8;

        private SMStype smsType(string Message)
        {
            if (Message.Contains("late"))
                return SMStype.late;           
            else if (Message.Contains("leave"))
                return SMStype.leave;           
            else if (Message.Contains("excuse"))
                return SMStype.excuse;
            else if (Message.Contains("web"))
                return SMStype.web_enable_disable;            
            else if (Message.StartsWith("n#"))    
                return SMStype.notice;

            return SMStype.invalid;
        }

        private string getSMSTime(string sentTime)
        {
            sentTime = sentTime.Trim();
            char[] separator = { ' ' };
            string[] smsTime = sentTime.Split(separator);
            return smsTime[1];
        }

        private bool validateSMSTime(string sentTime, SMStype smsType)
        {
            if (smsType == SMStype.leave || smsType == SMStype.web_enable_disable || smsType == SMStype.notice)
                return true;   // for leave,web_enable_disable [no time limit]; 
            
            string dbTime = getSMSTime(sentTime);

            DateTime smsTime = DateTime.Parse(dbTime);
            if ((smsTime.Hour > time_lmt_hr) || (smsTime.Hour == time_lmt_hr && (smsTime.Minute > time_lmt_mn)))
                return false;
            
            return true;
        }

        private void sendInfoForWeb(string param, string id, string timeStamp, string type) // remove the 4th argument when Zia vai is done
        {
            try
            {
                string url_data, postResult;
                url_data = param;
                postResult = httpPost.HttpPostToURL(mailerURL + url_data, param);
                Trace.WriteLine(string.Format("Http Post result: {0}", postResult));
            }
            catch (Exception exc)
            {
                SentrySdk.CaptureMessage("Exception At: " + exc);
            }
        }

        public void ParseIntoLocalDB(ShortMessage sms, bool fromDB)
        {
            /*
             * SMS Format
             * ~~~~~~~~~~~
             * 1. hhmm late 
             * 2. leave
             */
            string id = "";

            string sender = sms.Sender;
            string message = sms.Message.Trim();
            message = message.ToLower();
            char[] timeChar = new char[5];
            string time = "00:00:00";
            Regex rgx;
            Match match;
            SMStype type = smsType(message);

            string smsTimeFormatted = sms.Sent;

            try
            {
                smsTimeFormatted = DateTime.Parse(sms.Sent).ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch { smsTimeFormatted = DateTime.Parse(sms.Sent).ToString("yyyy-MM-dd HH:mm:ss"); }

            if (type == SMStype.invalid || validateSMSTime(sms.Sent, type) == false)
                qry = @"INSERT INTO received_sms (emp_id, mobile_num, recv_time, sms_content, sms_state, sms_type) " +
                       "VALUES(" + sms.smsID.ToString() + ",'" + sms.Sender + "','" + smsTimeFormatted + "','" + sms.Message + "'," + "'-1'" + "," + "'" + Convert.ToInt32(type).ToString() + "'" + ")";

            else if ((type == SMStype.notice || type == SMStype.web_enable_disable) && sms.smsID != 8) //8 its Murad vaia's ID Admin Id
                qry = "INSERT INTO received_sms (emp_id, mobile_num, recv_time, sms_content, sms_state, sms_type) VALUES(" + sms.smsID.ToString() + ",'" + sms.Sender + "','" + smsTimeFormatted + "','" + sms.Message + "'," + "'0'" + "," + "'" + Convert.ToInt32(type).ToString() + "'" + ")";
            else
                qry = @"INSERT INTO received_sms (emp_id, mobile_num, recv_time, sms_content, sms_state, sms_type) 
                        VALUES(" + sms.smsID.ToString() + ",'" + sms.Sender + "','" + smsTimeFormatted + "','" + sms.Message + "'," + "'0'" + "," + "'" + Convert.ToInt32(type).ToString() + "'" + ")";
            

            bool IsLocallyInserted = false;

            if (!String.IsNullOrEmpty(qry) && fromDB == false)
            {
                try
                {
                    IsLocallyInserted = dbHandler.ExecuteInsertionLocal(qry);
                }
                catch (Exception exc)
                {
                    SentrySdk.CaptureMessage("Exception At: " + exc);
                }
            }
            if (IsLocallyInserted)
            {
                if (validateSMSTime(sms.Sent, type))
                {
                    string employeeFullName = dbHandler.getEmpFullName(sms.smsID);

                    switch (type)
                    {
                        case SMStype.late:
                            // late
                            rgx = new Regex(@"(\d+) ");
                            match = rgx.Match(message);
                            if (match.Success)
                            {
                                timeChar[0] = match.Groups[1].Value[0]; // h
                                timeChar[1] = match.Groups[1].Value[1]; // h
                                timeChar[2] = ':';                      // :
                                timeChar[3] = match.Groups[1].Value[2]; // m
                                timeChar[4] = match.Groups[1].Value[3]; // m
                                time = new string(timeChar) + ":00"; // hh:mm:ss
                                SMSInfo lateInfo = new SMSInfo();
                                lateInfo.Sender = sms.Sender;
                                lateInfo.NetWorkTimeStemp = sms.Sent;
                                lateInfo.LateDuration = time;
                                lateInfo.SMSContent = sms.Message;
                                lateInfo.EmployeeID = sms.smsID;
                                lateInfo.SMSType = (int)type;

                                if (fromDB == false)
                                {
                                    dbHandler.PostReport(lateInfo);
                                }
                                sendInfoForWeb("type=001&parameters={late;" + dbHandler.getHRMSystemTime(sms.Sent) + ";" + dbHandler.getProbableEntry(time) + "}&empID=" + sms.smsID.ToString(), sms.smsID.ToString(), sms.Sent, "late");

                                qry = "SELECT id FROM received_sms WHERE emp_id=" + sms.smsID.ToString() + " AND recv_time='" + DateTime.Parse(smsTimeFormatted).ToString("yyyy-MM-dd") + "' AND sms_state='0'";
                                id = dbHandler.performSELECT_on_localDB(qry);

                                if (id != "" && id != null && id != String.Empty)
                                {
                                    qry = "UPDATE received_sms SET sms_state='1' WHERE id=" + id;
                                    dbHandler.ExecuteNonQueryLocal(qry);
                                }
                                else
                                {
                                    qry = "INSERT INTO received_sms (emp_id, mobile_num, recv_time, sms_content, sms_state, sms_type) VALUES(" + sms.smsID.ToString() + ",\"" + sms.Sender + "\",\"" + time + "\",\"" + sms.Message + "\"," + "'1'" + "," + "'0'" + ")";
                                    dbHandler.ExecuteInsertionLocal(qry);
                                }
                            }
                            else
                            {
                                sendInfoForWeb("type=002&parameters={late;" + sms.Message + "}&empID=" + sms.smsID.ToString(), sms.smsID.ToString(), sms.Sent, "late");

                                qry = "SELECT id FROM received_sms WHERE emp_id=" + sms.smsID.ToString() + " AND recv_time='" + DateTime.Parse(smsTimeFormatted).ToString("yyyy-MM-dd") + "' AND sms_state='0'";
                                id = dbHandler.performSELECT_on_localDB(qry);
                                qry = "";

                                if (id != "" && id != null && id != String.Empty)
                                {
                                    qry = "UPDATE received_sms SET sms_type='-1' WHERE id=" + id;

                                    try
                                    {
                                        dbHandler.ExecuteNonQueryLocal(qry);
                                    }
                                    catch (Exception exc) { SentrySdk.CaptureMessage("Exception At: " + exc); }

                                    qry = "UPDATE received_sms SET sms_state='1' WHERE id=" + id;
                                    dbHandler.ExecuteNonQueryLocal(qry);
                                }
                            }
                            break;

                        case SMStype.leave:
                            SMSInfo leaveInfo = new SMSInfo();
                            leaveInfo.Sender = sms.Sender;
                            leaveInfo.NetWorkTimeStemp = sms.Sent;
                            leaveInfo.LateDuration = "00:00:00"; // hh:mm:ss
                            leaveInfo.SMSContent = sms.Message;
                            leaveInfo.EmployeeID = sms.smsID;
                            leaveInfo.SMSType = (int)type;

                            if (fromDB == false)
                                dbHandler.PostReport(leaveInfo);

                            // Posting Leave information to HRP URL
                            sendInfoForWeb("type=005&parameters={" + leaveInfo.SMSContent + "}&empID=" + sms.smsID.ToString(), sms.smsID.ToString(), sms.Sent, "leave");   // Amjad; 27th Nov, 2013

                            qry = "SELECT id FROM received_sms WHERE emp_id=" + sms.smsID.ToString() + " AND recv_time='" + DateTime.Parse(smsTimeFormatted).ToString("yyyy-MM-dd") + "' AND sms_state='0'";
                            id = dbHandler.performSELECT_on_localDB(qry);
                            qry = "";

                            if (id != "" && id != null && id != String.Empty)
                                qry = "UPDATE received_sms SET sms_state='1' WHERE id=" + id;

                            break;

                        case SMStype.excuse:
                            rgx = new Regex(@"(\d+) ");
                            match = rgx.Match(message);
                            if (match.Success)
                            {
                                timeChar[0] = match.Groups[1].Value[0]; // h
                                timeChar[1] = match.Groups[1].Value[1]; // h
                                timeChar[2] = ':';                      // :
                                timeChar[3] = match.Groups[1].Value[2]; // m
                                timeChar[4] = match.Groups[1].Value[3]; // m
                                time = new string(timeChar) + ":00"; // hh:mm:ss
                                SMSInfo lateInfo = new SMSInfo();
                                lateInfo.Sender = sms.Sender;
                                lateInfo.NetWorkTimeStemp = sms.Sent;
                                lateInfo.LateDuration = time;
                                lateInfo.SMSContent = sms.Message;
                                lateInfo.EmployeeID = sms.smsID;
                                lateInfo.SMSType = (int)type;
                                //DBHandlerThread = new Thread(dbHandler.PostReport);
                                //DBHandlerThread.Start(lateInfo);
                                if (fromDB == false)
                                {
                                    dbHandler.PostReport(lateInfo);
                                }
                                sendInfoForWeb("type=001&parameters={excuse;" + dbHandler.getHRMSystemTime(sms.Sent) + ";" + dbHandler.getProbableEntry(time) + "}&empID=" + sms.smsID.ToString(), sms.smsID.ToString(), sms.Sent, "excuse");

                                qry = "SELECT id FROM received_sms WHERE emp_id=" + sms.smsID.ToString() + " AND recv_time='" + DateTime.Parse(smsTimeFormatted).ToString("yyyy-MM-dd") + "' AND sms_state='0'";
                                id = dbHandler.performSELECT_on_localDB(qry);
                                qry = "";

                                if (id != "" && id != null && id != String.Empty)
                                {
                                    qry = "UPDATE received_sms SET sms_state='1' WHERE id=" + id;
                                }
                            }

                            else
                            {
                                if (fromDB == false)
                                {
                                    sendInfoForWeb("type=002&parameters={excuse;" + sms.Message + "}&empID=" + sms.smsID.ToString(), sms.smsID.ToString(), sms.Sent, "excuse");
                                }

                                qry = "SELECT id FROM received_sms WHERE emp_id=" + sms.smsID.ToString() + " AND recv_time='" + DateTime.Parse(smsTimeFormatted).ToString("yyyy-MM-dd") + "' AND sms_state='0'";
                                id = dbHandler.performSELECT_on_localDB(qry);
                                qry = "";

                                if (id != "" && id != null && id != String.Empty)
                                {
                                    //qry = "INSERT INTO received_sms (emp_id, mobile_num, recv_time, sms_content, sms_state, sms_type) VALUES(" + sms.smsID.ToString() + ",\"" + sms.Sender + "\",\"" + time + "\",\"" + sms.Message + "\"," + "'1'" + "," + "'0'" + ")";
                                    qry = "UPDATE received_sms SET sms_type='-1' WHERE id=" + id;
                                    try
                                    {
                                        dbHandler.performUPDATE_on_localDB(qry);
                                    }
                                    catch (Exception exc) { SentrySdk.CaptureMessage("Exception At: " + exc); }
                                    qry = "UPDATE received_sms SET sms_state='1' WHERE id=" + id;
                                }
                            }
                            break;
                        // Topu; 19th Nov, 2013
                        case SMStype.web_enable_disable://Handle WEB punch enable/disable routine here
                            if (sms.smsID == getAdminID()) //getAdminID() Murad Vai's Id
                            {
                                string command = sms.Message.ToLower();
                                int indx = 0, len = "web".Length;
                                command = command.Remove(indx, len);
                                command = command.Trim();

                                string on_or_off = "";

                                if (command.Contains("on"))
                                {
                                    on_or_off = "1";

                                    indx = command.IndexOf("on");
                                    len = "on".Length;
                                    if (indx != -1) command = command.Remove(indx, len);
                                    command = command.Trim();
                                }
                                else if (command.Contains("off"))
                                {
                                    on_or_off = "0";

                                    indx = command.IndexOf("off");
                                    len = "off".Length;
                                    if (indx != -1) command = command.Remove(indx, len);
                                    command = command.Trim();
                                }

                                if (command.Contains("*"))  // enable or disable for all users
                                {
                                    //Posting enable information to HRP URL
                                    sendInfoForWeb("type=007&parameters={" + on_or_off + ";" + dbHandler.getHRMSystemDate() + "}&empID=all", sms.smsID.ToString(), sms.Sent, "web");

                                    if (on_or_off == "1") Trace.WriteLine("enable for all user command is sent!");
                                    else if (on_or_off == "0") Trace.WriteLine("disable for all user command is sent!");
                                }
                                else  // enable or disable for individual users
                                {
                                    string[] line = command.Split(' ');      // split the string based on space
                                    foreach (string word in line)
                                    {
                                        //Console.WriteLine(word);
                                        double Num;
                                        bool isNum = double.TryParse(word, out Num);
                                        if (isNum)       // if the word is a number
                                        {
                                            indx = command.IndexOf(word);
                                            len = word.Length;
                                            if (indx != -1) command = command.Remove(indx, len);
                                            command = command.Trim();

                                            //Posting enable information to HRP URL
                                            sendInfoForWeb("type=007&parameters={" + on_or_off + ";" + dbHandler.getHRMSystemDate() + "}&empID=" + word, sms.smsID.ToString(), sms.Sent, "web");
                                        }
                                        else Console.WriteLine("Expected a employee ID!");
                                    }
                                    if (on_or_off == "1") Trace.WriteLine("enable for individual users command is sent!");
                                    else if (on_or_off == "0") Trace.WriteLine("disable for individual users command is sent!");
                                }

                                qry = "SELECT id FROM received_sms WHERE emp_id=" + sms.smsID.ToString() + " AND recv_time='" + DateTime.Parse(smsTimeFormatted).ToString("yyyy-MM-dd") + "' AND sms_state='0'";
                                id = dbHandler.performSELECT_on_localDB(qry);
                                qry = "";

                                if (id != "" && id != null && id != String.Empty)
                                {
                                    //qry = "INSERT INTO received_sms (emp_id, mobile_num, recv_time, sms_content, sms_state, sms_type) VALUES(" + sms.smsID.ToString() + ",\"" + sms.Sender + "\",\"" + time + "\",\"" + sms.Message + "\"," + "'1'" + "," + "'0'" + ")";
                                    qry = "UPDATE received_sms SET sms_state='1' WHERE id=" + id;
                                }
                                // ..........................................
                            }
                            break;

                        // Amjad; 3rd DEC, 2013
                        case SMStype.notice:
                            string name = this.ToString();

                            break;

                        // Amjad; 9th NOV, 2013
                        case SMStype.invalid:

                            // valid sender sent sms in wrong format

                            time = "00:00:00"; // hh:mm:ss
                            SMSInfo lateInf = new SMSInfo();
                            lateInf.Sender = sms.Sender;
                            lateInf.NetWorkTimeStemp = sms.Sent;
                            lateInf.LateDuration = time;
                            lateInf.SMSContent = sms.Message;
                            lateInf.EmployeeID = sms.smsID;
                            lateInf.SMSType = -1;

                            dbHandler.PostReport(lateInf);

                            qry = "SELECT id FROM received_sms WHERE emp_id=" + sms.smsID.ToString() + " AND recv_time='" + DateTime.Parse(smsTimeFormatted).ToString("yyyy-MM-dd") + "' AND sms_state='0'";
                            id = dbHandler.performSELECT_on_localDB(qry);
                            qry = "";

                            if (id != "" && id != null && id != String.Empty)
                            {
                                //qry = "INSERT INTO received_sms (emp_id, mobile_num, recv_time, sms_content, sms_state, sms_type) VALUES(" + sms.smsID.ToString() + ",\"" + sms.Sender + "\",\"" + time + "\",\"" + sms.Message + "\"," + "'1'" + "," + "'0'" + ")";
                                qry = "UPDATE received_sms SET sms_type='-1' WHERE id=" + id;
                                if (dbHandler.performUPDATE_on_localDB(qry) == false)
                                {
                                    Trace.WriteLine("quey: {0}; was not successful", qry);
                                }
                                qry = "UPDATE received_sms SET sms_state='1' WHERE id=" + id;
                            }
                            break;


                        default:
                            // sms format error send feedback

                            break;
                    }
                }
                else
                {
                    string employeeFullName = dbHandler.getEmpFullName(sms.smsID);

                    if (type == SMStype.late)
                    {
                        sendInfoForWeb("type=003&parameters={late;" + dbHandler.getHRMSystemTime(sms.Sent) + "}&empID=" + sms.smsID.ToString(), sms.smsID.ToString(), sms.Sent, "late");
                    }
                    else if (type == SMStype.excuse)
                    {
                        sendInfoForWeb("type=003&parameters={excuse;" + dbHandler.getHRMSystemTime(sms.Sent) + "}&empID=" + sms.smsID.ToString(), sms.smsID.ToString(), sms.Sent, "excuse");
                    }
                }
            }
            else
                ParseIntoLocalDB(sms, fromDB);
        }
        #endregion

    }
}

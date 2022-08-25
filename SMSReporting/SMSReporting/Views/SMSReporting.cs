using BrotecsLateSMSReporting.Services;
using Sentry;
using System;
using System.Collections;  // for queue
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BrotecsLateSMSReporting
{
    public partial class SMSReporting : Form
    {
        #region Global variables

        private NotifyIcon notifyIcon;
        private IniFile iniFile;

        //// configuration files
        private static string configurationFilename = "./config.ini";

        // device setting
        private static SerialPort _serialPort;
        private static string port = "COM1";
        private static int baud = 9600;

        private static string server = "Localhost"; //Locall
        private static string psd = "";     // might change
        private static string usrid = "root";

        //time_limit conf
        private static int time_lmt_hr = 9;
        private static int time_lmt_mn = 30;
        private string greenBall = "./greenball.png";



        //Amjad; 23rd, Dec, 2013
        private bool balanceCheck = false; // used for checking balance
        private int prevState;
        private DateTime validDate;
        private double money;

        int dayLimit = 2;
        double tkLimit = 50.0;


        //Moddified sms Operation
        ProcessSMS processMessage = new ProcessSMS();

        DBHandler dbHandler;

        private Queue serialQueue;
        private string SMSLocation;

        public class IndataSMS
        {
            public ShortMessage sms;
            public bool smsPayloadFlag;
            public string outdata;

            public IndataSMS()
            {
                sms = new ShortMessage();
                smsPayloadFlag = false;
                outdata = String.Empty;
            }
        }
        private static IndataSMS mIndataSMS;

        #endregion

        #region Constructor
        public SMSReporting()
        {
            InitializeComponent();

            mIndataSMS = new IndataSMS();
            serialQueue = new Queue();

            Task.Run(async () => await readSerialQueue()).ConfigureAwait(false);

            if (!IsProcessRunningThenKillit("UIMain"))
            {
                Trace.WriteLine(string.Format("UImain is not active"));
            }
            Task.Delay(100);

            readConfiguration();

            #region Minimizing Form

            this.WindowState = FormWindowState.Minimized;
            this.Resize += (sender, e) =>
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    this.Hide();
                    notifyIcon.Visible = true;
                    notifyIcon.ShowBalloonTip(1000);
                }
            };

            notifyIcon = new NotifyIcon()
            {
                Text = "Brotecs SMS Reporting",
                BalloonTipText = "Brotecs SMS Reporting Running...",
                Icon = Icon.ExtractAssociatedIcon("./icon.ico")
            };

            notifyIcon.Click += (sender, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                notifyIcon.Visible = true;
            };

            #endregion

            dbHandler = new DBHandler();
        }

        #endregion

        #region Form Load/Close
        private void main_Load(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                await InitSerial(port, baud, Parity.None, 8, StopBits.One, Handshake.None);
                await loadTodaysMessages();
            });
        }
        private void main_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        #endregion

        private async Task loadTodaysMessages()
        {
            try
            {
                string currentDate = SyncTime.AcceptedTime().ToString("yyyy-MM-dd");

                List<List<string>> result = new List<List<string>>();

                if (currentDate != "" && currentDate != null && currentDate != String.Empty) result = dbHandler.getSMSReceivedOnDate(currentDate);
                if (result.Count > 0)
                {
                    int i, listLen = result.Count;
                    for (i = 0; i < listLen; i++)
                    {
                        result[i].Add(dbHandler.getEmpFullName(Convert.ToInt32(result[i][0])));
                        await addData(result[i][0], result[i][1], result[i][2], result[i][3], result[i][4]);
                    }
                }
            }
            catch (Exception exc)
            {
                SentrySdk.CaptureMessage("At loading Today's message" + exc);
            }
        }

        #region Serial Communication
        private async Task InitSerial(string port, int baud, Parity parity, int databits, StopBits stopbits, Handshake handshake)
        {
            _serialPort = new SerialPort();
            _serialPort.PortName = port;
            _serialPort.BaudRate = baud;
            _serialPort.Parity = parity;
            _serialPort.DataBits = databits;
            _serialPort.StopBits = stopbits;
            _serialPort.Handshake = handshake;
            _serialPort.ReadBufferSize = 4096;
            _serialPort.WriteBufferSize = 4096;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);

            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;
            _serialPort.Open();
            // check if device is active or not
            _serialPort.WriteLine("ATE0\r");
            await Task.Delay(100);
            _serialPort.WriteLine("ATE0V1\r");
            await Task.Delay(100);
            _serialPort.WriteLine("AT\r");
            await Task.Delay(100);
            _serialPort.WriteLine("AT+IFC=0,0\r");
            await Task.Delay(100);
            _serialPort.WriteLine("AT+CMGF=1\r");
            await Task.Delay(100);
            _serialPort.WriteLine("AT&W\r");
            await Task.Delay(100);
            _serialPort.WriteLine("AT+COPS?\r");
            await Task.Delay(100);
            _serialPort.WriteLine("AT+CMGL=\"REC UNREAD\",1\r");
        }

        private async Task parseIndata(string indata)
        {
            string outdata = "";
            string strRegex = "";
            if (indata.Contains("+CMTI:"))
            {

                strRegex = @"\+CMTI: ""(\w+)"",(\d+)";
                Regex rgx = new Regex(strRegex);
                Match match = rgx.Match(indata);
                if (match.Success)
                {

                    SMSLocation = match.Groups[2].Value;
                    //}
                }
                outdata = "AT+CMGR=" + SMSLocation + "\r";
                _serialPort.WriteLine(outdata);
            }
            else if (indata.Length > 0 && mIndataSMS.smsPayloadFlag == true)
            {
                mIndataSMS.sms.Message = indata;

                await processData(mIndataSMS.sms.smsID.ToString(), mIndataSMS.sms.Sender, DateTime.ParseExact(mIndataSMS.sms.Sent, "yy/MM/dd HH:mm:ss", null).ToString(), mIndataSMS.sms.Message, dbHandler.getEmployeeFullName(mIndataSMS.sms.smsID));
                await addData(mIndataSMS.sms.smsID.ToString(), mIndataSMS.sms.Sender, DateTime.ParseExact(mIndataSMS.sms.Sent, "yy/MM/dd HH:mm:ss", null).ToString(), mIndataSMS.sms.Message, dbHandler.getEmployeeFullName(mIndataSMS.sms.smsID));

                this.updateTExtBoxTest(mIndataSMS.sms.Message);

                _serialPort.WriteLine(mIndataSMS.outdata);
                await Task.Delay(300);
                mIndataSMS.smsPayloadFlag = false;
            }
            else if (indata.Contains("+CMGR:") || indata.Contains("+CMGL:")) //"Ignored!", "UNAUTHORIZED SENDER"
            {
                strRegex = @"\+CMG([R|L]):( | \d+,)""(.*)"",""(\+\d+)"",(|""""|"".*""),""(.*),(.*)\+\d+""";//\r\n(.*)\r\n
                Regex rgx = new Regex(strRegex);
                Match match = rgx.Match(indata);
                if (match.Success)
                {
                    //ShortMessage sms = new ShortMessage();
                    mIndataSMS.sms.smsID = -1;
                    mIndataSMS.sms.Sender = match.Groups[4].Value; // Groups - 4
                    if (mIndataSMS.sms.Sender.Length >= 14)
                    {
                        mIndataSMS.sms.Sender = mIndataSMS.sms.Sender.Substring(0, 14);
                        mIndataSMS.sms.smsID = dbHandler.GetEmployeeID(mIndataSMS.sms.Sender);
                    }

                    if (mIndataSMS.sms.smsID < 0)
                    {
                        await processData(mIndataSMS.sms.smsID.ToString(), mIndataSMS.sms.Sender, dbHandler.getHRMSystemDateTime(), "Ignored!", "UNAUTHORIZED SENDER");
                        await addData(mIndataSMS.sms.smsID.ToString(), mIndataSMS.sms.Sender, dbHandler.getHRMSystemDateTime(), "Ignored!", "UNAUTHORIZED SENDER");
                    }
                    else
                    {
                        mIndataSMS.sms.Status = match.Groups[3].Value; // Groups - 3
                        mIndataSMS.sms.Alphabet = match.Groups[5].Value; // Groups - 5
                        mIndataSMS.sms.Sent = match.Groups[6].Value + " " + match.Groups[7].Value; // Groups - 6 & 7
                        mIndataSMS.smsPayloadFlag = true;
                        //mIndataSMS.sms.Message = match.Groups[8].Value; // Groups - 8
                    }

                    if (match.Groups[1].Value.Contains("R"))  // Groups - 1
                    {
                        // removing all SIM sms
                        mIndataSMS.outdata = "AT+CMGDA=\"DEL READ\"\r";
                    }
                    else if (match.Groups[1].Value.Contains("L"))
                    {
                        // removing the sms
                        SMSLocation = match.Groups[2].Value.Trim().Substring(0, 1); // Groups - 2
                        mIndataSMS.outdata = "AT+CMGD=" + SMSLocation + ",0\r";
                    }
                }
            }
            else if (indata.Contains("+COPS:"))
            {
                // +COPS: 0,0,"Grameenphone",0
                //\+COPS: (\d+),(\d+),"(\w+)",(\d+)
                strRegex = @"\+COPS: (\d+),(\d+),""(\w+)""";
                Regex rgx = new Regex(strRegex);
                Match match = rgx.Match(indata);
                if (match.Success)
                {
                    this.label_Operator.BeginInvoke((MethodInvoker)delegate ()
                    {
                        this.label_Operator.Text = match.Groups[3].Value;
                    });
                }
            }
            else if (indata.Contains("OK"))
            {
                this.pictureBoxDataReceive.BeginInvoke((MethodInvoker)delegate ()
                {
                    this.pictureBoxDataReceive.Image = Image.FromFile(greenBall);
                });
            }
            else if (indata.Contains(">"))
            {
                //writeSMSMessageFlag = true;
            }
            if (balanceCheck)
            {
                if (prevState == 0 && indata.Contains("Call Ready"))
                {
                    ++prevState;
                    modemCommand("at*psstk=\"SETUP MENU\",1,4\r");
                }
                else if (prevState == 1 && indata.Contains("END SESSION"))
                {
                    ++prevState;
                    modemCommand("AT*PSSTK=\"GET ITEM LIST\",8\r");
                }
                else if (prevState == 2 && indata.Contains("Prepaid Recharge & Balance"))
                {
                    ++prevState;
                    modemCommand("AT*PSSTK=\"MENU SELECTION\",5\r");
                }
                else if (prevState == 3 && indata.Contains("Prepaid Recharge & Balance"))
                {
                    ++prevState;
                    modemCommand("AT*PSSTK=\"SELECT ITEM\",1,1,0,0\r");
                }
                else if (prevState == 4 && indata.Contains("NOTIFICATION"))
                {
                    ++prevState;
                    modemCommand("AT*PSSTK=\"NOTIFICATION\",1,0\r");
                }
                else if (prevState == 5 && indata.Contains("BDT "))
                {
                    ++prevState;
                    //Match match = Regex.Match(indata, @"Your account balance is BDT ([^d]+) valid till ([^d]+/[^d]+/[^d]+)\.", RegexOptions.IgnoreCase);
                    Match match = Regex.Match(indata, @"BDT ([\d]+.[\d]+) valid till ([\d]+)/([\d]+)/([\d]+)");
                    string key = "";

                    try
                    {
                        if (match.Success)
                        {
                            key = "Your account balance is BDT " + match.Groups[1].Value + ", valid till " + match.Groups[2].Value + "/" + match.Groups[3].Value + "/" + match.Groups[4].Value + ".";

                            money = Convert.ToDouble(match.Groups[1].Value);
                            string vDate = (match.Groups[3].Value + "/" + match.Groups[2].Value + "/" + match.Groups[4].Value);
                            //string vDate = (match.Groups[2].Value + "/" + match.Groups[3].Value + "/" + match.Groups[4].Value);
                            validDate = Convert.ToDateTime(vDate);
                            //validDate = DateTime.ParseExact(vDate, "d/M/yyyy", null);

                            balanceCheck = false;
                            prevState = 0;

                            //_serialPort.Close();
                        }
                        else
                        {
                            key = "regular expression didn't match for balance checking!";
                        }
                    }
                    catch (Exception exc)
                    {
                        SentrySdk.CaptureMessage("Exception At" + exc);
                        key = "exception:" + exc.ToString() + "occured";
                    }

                    this.updateTExtBoxTest("Modem << " + key);
                    Trace.WriteLine("Modem << " + key);
                }
            }
        }

        private async Task readSerialQueue()
        {
            char inChar = '\0';
            bool rcvStateFlag = false;
            int rcvCharCount = 0;
            const int inputStringLenght = 2048;
            char[] inputString = new char[inputStringLenght];
            while (true)
            {
                await Task.Delay(10);
                if (serialQueue.Count > 0)
                {
                    inChar = (char)serialQueue.Dequeue();
                    switch (rcvStateFlag)
                    {
                        case false:
                            if (inChar == '\n')
                            {
                                rcvStateFlag = true;
                                rcvCharCount = 0;
                            }
                            break;

                        case true:
                            if (inChar == '\r')
                            {
                                rcvStateFlag = false;
                                inputString[rcvCharCount] = '\0';
                                if (rcvCharCount > 0)
                                {
                                    string parseString = new string(inputString);
                                    await parseIndata(parseString.Substring(0, rcvCharCount));
                                }
                                rcvCharCount = 0;
                            }
                            else
                            {
                                inputString[rcvCharCount++] = inChar;
                                if (rcvCharCount > inputStringLenght)
                                {
                                    rcvCharCount = 0;
                                    rcvStateFlag = false;
                                }
                            }
                            break;
                    }
                }
            }
        }

        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            char[] recChars = indata.ToCharArray();
            foreach (char ch in recChars)
            {
                serialQueue.Enqueue((char)ch);
            }

            this.updateTExtBoxTest("Modem << " + indata);
            Trace.WriteLine("Modem << " + indata);
        }

        #endregion

        #region Configuration Read/Write
        private void readConfiguration()
        {
            iniFile = new IniFile();
            string temp = "";

            iniFile.Load(configurationFilename);
            // get port name
            if (iniFile.GetKeyValue("device_setting", "port") != string.Empty)
            {
                temp = iniFile.GetKeyValue("device_setting", "port");
                port = temp.Trim();
            }
            // get baud rate
            if (iniFile.GetKeyValue("device_setting", "baud") != string.Empty)
            {
                temp = iniFile.GetKeyValue("device_setting", "baud");
                baud = Convert.ToInt32(temp);
            }

            if (iniFile.GetKeyValue("configuration", "time_limit") != string.Empty)
            {
                string tmlmt = iniFile.GetKeyValue("configuration", "time_limit").Trim();

                int ii;
                time_lmt_hr = 0;
                time_lmt_mn = 0;
                for (ii = 0; tmlmt[ii] != ':'; ii++)
                {
                    time_lmt_hr *= 10;
                    time_lmt_hr += (tmlmt[ii] - '0');
                }

                for (ii = ii + 1; ii < tmlmt.Length; ii++)
                {
                    time_lmt_mn *= 10;
                    time_lmt_mn += (tmlmt[ii] - '0');
                }
            }
            if (iniFile.GetKeyValue("balanceVilidity", "limitTK") != string.Empty)
            {
                tkLimit = Convert.ToDouble(iniFile.GetKeyValue("balanceVilidity", "limitTK").Trim());
            }
            if (iniFile.GetKeyValue("balanceVilidity", "limitDAY") != string.Empty)
            {
                dayLimit = Convert.ToInt32(iniFile.GetKeyValue("balanceVilidity", "limitDAY").Trim());
            }
        }
        #endregion

        private void configureSMSPortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModemSetting modemSetting = new ModemSetting(configurationFilename);
            if (modemSetting.ShowDialog() == DialogResult.OK)
            {
                if (modemSetting.IsSettingChanged(ref port, ref baud))
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }
                    InitSerial(port, baud, Parity.None, 8, StopBits.One, Handshake.None);
                    modemSetting.Close();
                }
            }

        }

        private bool IsProcessRunningThenKillit(string name)
        {
            foreach (Process runningProcess in Process.GetProcesses())
            {
                if (runningProcess.ProcessName.StartsWith(name))
                {
                    //process found so it's running so return true
                    try
                    {
                        runningProcess.Kill();
                        return true;
                    }
                    catch (Exception exc)
                    {
                        SentrySdk.CaptureMessage("Exception At" + exc);
                        return false;
                    }
                }
            }

            //process not found, return false
            return false;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string VIRSION = iniFile.GetKeyValue("version_no", "version");
            string REVISION = iniFile.GetKeyValue("version_no", "revision");

            AboutBox abtBox = new AboutBox();
            abtBox.labelVersion.Text = "Version: " + VIRSION;
            abtBox.labelCompanyName.Text = "Company name: BroTecs Limited";
            abtBox.labelCopyright.Text = "Copyright: \u00a9BroTecs";
            abtBox.textBoxDescription.Text = "Revision: " + REVISION + Environment.NewLine + "Thank you";
            //if (modemSetting.ShowDialog() == DialogResult.OK)
            abtBox.ShowDialog();
        }

        public void modemCommand(string outData)
        {
            try
            {
                this.updateTExtBoxTest("Modem >> " + outData);
                Trace.WriteLine("Modem >> " + outData);
                _serialPort.WriteLine(outData);
                Task.Delay(3000);
            }
            catch (Exception exc)
            {
                SentrySdk.CaptureMessage("Exception At: " + exc);
                MessageBox.Show(exc.Source);
            }
        }

        public async Task checkBalance()      // if it returns false then device need to reboot
        {
            if (_serialPort.IsOpen)
            {
                modemCommand("AT+CFUN=1,1\r");
                await Task.Delay(10);
            }
            else
            {
                Trace.WriteLine("serialPort is not open to check balance");
                this.updateTExtBoxTest("serialPort is not open to check balance");
                //Console.WriteLine("serialPort is not open to send Notice Meassage");
            }
        }

        private async Task ReadFromViewItem(ShortMessage current_sms)
        {

            if (current_sms.smsID != -1)
            {
                await Task.Delay(100);

                Logging.WriteLog("[UNREAD] Sms found at: " + current_sms.Sent + " Sender: " + current_sms.Sender + "\nMessage: " + current_sms.Message + "\n");
                DateTime sentDate = DateTime.MinValue;
                try
                {
                    sentDate = DateTime.ParseExact(current_sms.Sent, "yy/MM/dd HH:mm:ss", null);
                }
                catch
                {
                    sentDate = DateTime.Parse(current_sms.Sent);
                }

                bool isSmssTodays = sentDate.Date >= DateTime.Now.Date;

                if (isSmssTodays)
                    processMessage.ParseIntoLocalDB(current_sms, false);
                else
                    Logging.WriteLog("[IGNORED] Sms found at: " + current_sms.Sent + " Sender: " + current_sms.Sender + "\nMessage: " + current_sms.Message + "\n\n");
            }
        }

        private async Task processData(string id, string num, string sent_time, string smsContent, string name)
        {
            int employeeID = processMessage.authenticateSender(num);

            ShortMessage msg = new ShortMessage
            {
                smsID = employeeID,
                Sender = num,
                Sent = sent_time,
                Message = smsContent,
            };
            await Task.Run(async () => await ReadFromViewItem(msg));
        }

        private async Task addData(string id, string num, string sent_time, string smsContent, string name)
        {
            await Task.Delay(10);

            if (DateTime.Parse(sent_time).Date == DateTime.Today.Date)
            {
                ListViewItem iv = new ListViewItem(id);
                iv.SubItems.Add(name);
                iv.SubItems.Add(num);
                iv.SubItems.Add(smsContent);
                iv.SubItems.Add(sent_time);

                this.listView1.BeginInvoke((MethodInvoker)delegate ()
                {
                    this.listView1.Items.Add(iv);
                });
            }
        }

        private void sMSLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sms_log smsLOG = new sms_log();
            smsLOG.ShowDialog();
        }

        private void button_Refresh_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            Task.Delay(10);
            Task.Run(async () => await loadTodaysMessages());
        }

        private void SMSReporting_TextChanged(object sender, EventArgs e)
        {
            textBox_log.SelectionStart = textBox_log.Text.Length;
            textBox_log.ScrollToCaret();
        }

        private void updateTExtBoxTest(string strLog)
        {
            this.textBox_log.BeginInvoke((MethodInvoker)delegate ()
            {
                this.textBox_log.Text += strLog + Environment.NewLine;
            });
        }

        private void balance_Click(object sender, EventArgs e)
        {
            if (balanceCheck == true)
            {
                MessageBox.Show("Balance checking system busy, please try again after a while", "SIM Credit Balance");
            }
            else
            {
                prevState = 0;
                balanceCheck = true;
                Task.Run(async () =>
                {
                    await checkBalance();
                    await Task.Delay(1000 * 30);
                });

                if (balanceCheck == true)
                {
                    prevState = 0;
                    balanceCheck = false;
                    MessageBox.Show("Please try again later!", "SIM Credit Balance");
                }
                else
                    MessageBox.Show("Your account balance is BDT " + money.ToString() + " valid till " + validDate.ToString() + ".", "SIM Credit Balance");

            }
        }

    }
}

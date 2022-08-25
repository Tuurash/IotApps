using Sentry;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading.Tasks;

namespace BrotecsLateSMSReporting
{
    public class MessagingActions
    {
        private static SerialPort _serialPort;

        public bool sendSMS(string num, string sms)
        {
            string messages = sms, cellNo = num;

            if (_serialPort.IsOpen)
            {
                try
                {
                    string outdata;
                    outdata = string.Format("AT+CMGS=\"{0}\"\r", cellNo);
                    Trace.WriteLine("Modem >> " + outdata);
                    _serialPort.WriteLine(outdata);
                    Task.Delay(5000);
                    outdata = string.Format("{0}{1}", messages, (char)0x1A);

                    Trace.WriteLine("Modem >> " + outdata);
                    _serialPort.WriteLine(outdata);
                    return true;
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureMessage("Exception At: " + ex);
                    return false;
                }
            }
            else
            {
                Trace.WriteLine("serialPort is not open to send Notice Meassage");
                return false;
            }
        }


        // if checkBalance returns false then device need to reboot
        public void checkBalance()
        {
            if (_serialPort.IsOpen)
            {
                modemCommand("AT+CFUN=1,1\r");
            }
            else
            {
                Trace.WriteLine("serialPort is not open to check balance");
            }
        }

        public void modemCommand(string outData)
        {
            try
            {
                Trace.WriteLine("Modem >> " + outData);
                _serialPort.WriteLine(outData);
                Task.Delay(3000);
            }
            catch (Exception exc)
            {
                SentrySdk.CaptureMessage("Exception At: " + exc);
            }
        }
    }
}

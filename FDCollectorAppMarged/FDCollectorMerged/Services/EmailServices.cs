using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Mail;
using System.Net.Mime;


namespace AttendenceApp.Services
{
    class EmailServices
    {
        public static void SendMail(string exceptionMsg)
        {
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(@"brotecshrm@gmail.com");
                mail.To.Add("masba.habib@brotecs.com");
                mail.To.Add("murad.hasan@brotecs.com");
                mail.Subject = "HRM Attendence System Server Connection Error";
                mail.Body = "<font style='color:red'>Error Connecting to Main server</font></br></br>Exception No:</br>" + exceptionMsg;
                mail.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(@"brotecshrm@gmail.com", "brotecs1230");
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
        }


    }
}



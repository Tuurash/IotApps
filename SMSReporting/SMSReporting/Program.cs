using Sentry;
using System;
using System.Windows.Forms;

namespace BrotecsLateSMSReporting
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (SentrySdk.Init(o =>
            {
                o.Dsn = "https://a3c015d08e0e439281802b948c1f267e@o1261480.ingest.sentry.io/6439292";
                o.Debug = true;
                o.TracesSampleRate = 1.0;
            }))
            {
                // App code goes here. Dispose the SDK before exiting to flush events.
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new SMSReporting());
            }
        }
    }
}

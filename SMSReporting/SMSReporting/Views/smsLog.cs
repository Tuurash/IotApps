using Sentry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;


// Amjad; 17th Nov, 2013

namespace BrotecsLateSMSReporting
{
    public partial class sms_log : Form
    {
        DBHandler dbHandler = new DBHandler();
        ListViewItem iv;

        List<List<string>> result = new List<List<string>>();
        public sms_log()
        {
            InitializeComponent();
            dateTimePicker1.Value = DateTime.Now;
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e) => Task.Run(async () => await UpdateSMSLogList());

        private async Task UpdateSMSLogList()
        {
            Cursor.Current = Cursors.WaitCursor;

            DateTime dateTime = dateTimePicker1.Value;
            string currentDate = dateTime.Year + "-" + dateTime.Month + "-" + dateTime.Day;

            result.Clear();
            await Task.Delay(1);
            if (currentDate != "" && currentDate != null)
                result = dbHandler.getSMSReceivedOnDate(currentDate);

            if (result.Count > 0)
            {
                try
                {
                    this.listView2.BeginInvoke((MethodInvoker)delegate ()
                    {
                        this.listView2.Items.Clear();
                    });
                }
                catch (Exception exc)
                {
                    SentrySdk.CaptureMessage("Exception At: " + exc);
                }

                int i, listLen = result.Count;
                for (i = 0; i < listLen; i++)
                {
                    result[i].Add(dbHandler.getEmpFullName(Convert.ToInt32(result[i][0])));
                    addData(result[i][0], result[i][1], result[i][2], result[i][3], result[i][4]);
                }

                Cursor.Current = Cursors.Default;
            }
        }

        private void addData(string id, string num, string sent_time, string smsContent, string name)
        {
            iv = new ListViewItem(id);
            iv.SubItems.Add(name);
            iv.SubItems.Add(num);
            iv.SubItems.Add(smsContent);
            iv.SubItems.Add(sent_time);

            this.listView2.BeginInvoke((MethodInvoker)delegate ()
            {
                this.listView2.Items.Add(iv);
            });
        }

    }
}

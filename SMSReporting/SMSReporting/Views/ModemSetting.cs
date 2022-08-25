using BrotecsLateSMSReporting.Services;
using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace BrotecsLateSMSReporting
{
    public partial class ModemSetting : Form
    {
        private IniFile iniFile;
        private string _port;
        private int _baud;
        private string _configurationFilename;
        private bool comboBoxCOMPortChanged;
        private bool comboBoxBaudRateChanged;

        private bool SettingChanged;

        public ModemSetting(string configurationFilename)
        {
            InitializeComponent();
            comboBoxBaudRateChanged = false;
            comboBoxCOMPortChanged = false;
            SettingChanged = false;
            iniFile = new IniFile();
            _configurationFilename = configurationFilename;
            iniFile.Load(_configurationFilename);
            string temp;
            // get port name
            if (iniFile.GetKeyValue("device_setting", "port") != string.Empty)
            {
                temp = iniFile.GetKeyValue("device_setting", "port");
                _port = temp.Trim();
            }
            // get baud rate
            if (iniFile.GetKeyValue("device_setting", "baud") != string.Empty)
            {
                temp = iniFile.GetKeyValue("device_setting", "baud");
                _baud = Convert.ToInt32(temp);
            }
        }

        private void ModemSetting_Load(object sender, EventArgs e)
        {
            int index = 0;
            int count = 0;
            foreach (string port in SerialPort.GetPortNames())
            {
                comboBox_COMPort.Items.Add(port);
                if (_port.Equals(port))
                {
                    index = count;
                }
                count++;
            }
            comboBox_COMPort.SelectedIndex = index;
            string baud = _baud.ToString();
            comboBox_BaudRate.Items.Add("4800");
            comboBox_BaudRate.Items.Add("9600");
            comboBox_BaudRate.Items.Add("19200");
            comboBox_BaudRate.Items.Add("38400");
            comboBox_BaudRate.Items.Add("57600");
            comboBox_BaudRate.Items.Add("115200");
            comboBox_BaudRate.SelectedIndex = comboBox_BaudRate.Items.IndexOf(baud);
        }

        private void button_Save_Click(object sender, EventArgs e)
        {
            _port = (string)comboBox_COMPort.SelectedItem;
            _baud = Convert.ToInt32((string)comboBox_BaudRate.SelectedItem);
            iniFile.SetKeyValue("device_setting", "port", _port);
            iniFile.SetKeyValue("device_setting", "baud", _baud.ToString());
            iniFile.Save(_configurationFilename);
            button_Save.Enabled = false;
        }

        private void comboBox_COMPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!comboBoxCOMPortChanged)
            {
                comboBoxCOMPortChanged = true;
                SettingChanged = false;
                button_Save.Enabled = false;
            }
            else
            {
                button_Save.Enabled = true;
                SettingChanged = true;
            }
        }

        private void comboBox_BaudRate_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!comboBoxBaudRateChanged)
            {
                comboBoxBaudRateChanged = true;
                SettingChanged = false;
                button_Save.Enabled = false;
            }
            else
            {
                button_Save.Enabled = true;
                SettingChanged = true;
            }
        }

        private void button_Close_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Hide();
        }

        public bool IsSettingChanged(ref string port, ref int baud)
        {
            if (SettingChanged)
            {
                port = _port;
                baud = _baud;
            }
            return SettingChanged;
        }
    }
}

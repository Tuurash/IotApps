/*
  Reimagined System.ini
  Developed By: Mohaimanul Haque Turash
*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendenceApp.Models
{
    //[records]
    public class LRecords
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }


        //log_count=558
        public int LogCount { get; set; }
    }

    //[configuration]
    public class DeviceConfiguration
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }



        //port= 4370
        //ip= 192.168.30.200
        //device_no= 1
        //model= R2
        //comm_key = 1234
        //debug = 0
        public int Port { get; set; }
        public int IsDebug { get; set; }
        public int DeviceNo { get; set; }
        public int CommKey { get; set; }
        public string Ip { get; set; }
        public string Model { get; set; }
    }

    //[limits]
    public class EmployeeDirectives
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }



        //employee_limit=24
        public int EmployeeLimit { get; set; }
    }

    //[db_settings]
    public class MySqlConfigs
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }


        //serverIP = 192.168.30.105
        //serverKey = null
        public string ServerIp { get; set; }
        public string Key { get; set; }

        public string Port { get; set; }
        public string DatabaseName { get; set; }
        public string Uid { get; set; }
        public string Password { get; set; }
    }
}

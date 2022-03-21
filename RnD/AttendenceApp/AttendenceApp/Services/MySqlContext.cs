using AttendenceApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace AttendenceApp.Services
{
    public class MySqlContext : DbContext
    {
        MySqlConfigs Configuration;

        public MySqlContext()
        {
            CredsConfig context = new CredsConfig();
            MySqlConfigs dbConfig = context.MySqlConfigs.First();

            Configuration = dbConfig;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (Configuration != null)
            {
                try
                {
                    string connectionString = "SERVER=" + Configuration.ServerIp + ";" + "PORT=" + Configuration.Port + ";" + "DATABASE=" + Configuration.DatabaseName + ";" + "UID=" + Configuration.Uid + ";" + "PASSWORD=" + Configuration.Password + ";";
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Remote Database Connection Failed. Error: Details \n" + exc);
                }
            }
        }

        public DbSet<ohrm_fingerprint_log_count> FingerprintLogCount { get; set; }
        public DbSet<ohrm_finger_print_device_recorde> FingerprintDeviceRecords { get; set; }
        public DbSet<blt_lateSMS_detail> LateSmsDetails { get; set; }
        public DbSet<ohrm_attendance_record> AttendenceRecords { get; set; }
        public DbSet<btl_working_date> WorkingDatas { get; set; }

        public MySqlContext(DbContextOptions<MySqlContext> options) : base(options)
        {
        }
    }
}

/*
 * Reimagined
  Configuiring System Dinamically
  Developed By: Mohaimanul Haque Turash
*/


using AttendenceApp.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Reflection;

namespace AttendenceApp.Services
{
    public class CredsConfig : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@"Filename=LDB.db", option =>
             {
                 option.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
             });
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<LRecords> LRecords { get; set; }
        public DbSet<DeviceConfiguration> DeviceConfiguration { get; set; }
        public DbSet<EmployeeDirectives> EmployeeDirectives { get; set; }
        public DbSet<MySqlConfigs> MySqlConfigs { get; set; }
    }
}

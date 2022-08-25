using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendenceApp.Services
{
    public class SqliteDbContext : DbContext
    {
        public DbSet<PunchListLocal> Localpunches { get; set; }
        public string DbPath { get; }

        public SqliteDbContext()
        {
            var folder = AppDomain.CurrentDomain.BaseDirectory;
            DbPath = folder+"/LocalPunchDb.db";
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<PunchListLocal>()
            .HasKey(p => new { p.Id });
    }

    public class PunchListLocal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }


        public int state { get; set; }
        public string UserID { get; set; }
        public int EmployeeId { get; set; }
        public DateTime PunchTime { get; set; }
        public int PunchedDevice { get; set; }
        public string VerifyState { get; set; }
        public bool isSynced { get; set; }
        public string DeviceName { get; set; }
    }
}

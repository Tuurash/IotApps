using AttendenceApp.Models;
using AttendenceApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttendenceApp.Data
{
    public class DataServices
    {
        private MySqlContext context;

        public DataServices()
        {
            context = new MySqlContext();
        }

        //Insert DeviceRecord
        public bool InsertDeviceRecord(ohrm_finger_print_device_recorde record)
        {
            ohrm_finger_print_device_recorde deviceRecords = new ohrm_finger_print_device_recorde
            {
                Id = GetMaxId(),
                EmployeeId = record.EmployeeId,
                EmployeeDeviceId = record.EmployeeDeviceId,
                PunchTime = record.PunchTime,
                PunchSate = record.PunchSate,
                PunchedDevice = record.PunchedDevice,
            };

            try
            {
                context.Add(deviceRecords);
                context.SaveChanges();
                return true;
            }
            catch (Exception exc)
            {
                Console.WriteLine("Error While Insertion in ohrm_finger_print_device_recorde \n Error: \n" + exc);
                return false;
            }
        }

        //Insert WorkDay
        public bool InsertWorkDayRecord(btl_working_date workDateRecord)
        {
            btl_working_date dateRecords = new btl_working_date
            {
                Workdate = workDateRecord.Workdate,
                Status = workDateRecord.Status,
            };
            try
            {
                context.Add(dateRecords);
                context.SaveChanges();
                return true;
            }
            catch (Exception exc)
            {
                Console.WriteLine("Error While Insertion in btl_working_date \n Error: \n" + exc);
                return false;
            }
        }

        //Get Max PunchIn Time 
        public DateTime GetMaxPunchinTime()
        {
            return DateTime.Parse(context.FingerprintDeviceRecords.Max(x => x.PunchTime).ToString());
        }

        //Get MAX Id=>Last value
        public int GetMaxId()
        {
            return int.Parse(context.FingerprintDeviceRecords.Max(x => x.Id).ToString()) + 1;
        }

        public string GetMaxIdByWorkingDate(string WorkDate)
        {
            return context.WorkingDatas.Where(x => x.Workdate == WorkDate).Select(x => x.Id).Max().ToString();
        }

        public List<string> GetLast2StateList(string WorkDate, int EmpId)
        {
            return context.AttendenceRecords.
                Where(x => (x.PunchInUserTime == WorkDate) && (x.EmployeeId == EmpId))
                .Select(x => x.State)
                .TakeLast(2)
                .ToList();
        }

        //query = "SELECT MAX(id) FROM brotecshrm.ohrm_attendance_record
        //WHERE employee_id=" + EmployeeId + "
        //AND " + "DATE(punch_in_user_time)=" + work_date + "
        //AND " + "state='PUNCHED OUT'";

    }
}

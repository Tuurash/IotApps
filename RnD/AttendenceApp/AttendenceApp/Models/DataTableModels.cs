using Newtonsoft.Json;

namespace AttendenceApp.Models
{
    public class ohrm_fingerprint_log_count
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("log_count")]
        public int LogCount { get; set; }
    }

    public class ohrm_finger_print_device_recorde
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("employee_device_id")]
        public string EmployeeDeviceId { get; set; }

        [JsonProperty("punch_time")]
        public string PunchTime { get; set; }

        [JsonProperty("punch_sate")]
        public string PunchSate { get; set; }

        [JsonProperty("punched_device")]
        public int PunchedDevice { get; set; }
    }

    public class blt_lateSMS_detail
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("empID")]
        public int EmpID { get; set; }

        [JsonProperty("empTel")]
        public string EmpTel { get; set; }

        [JsonProperty("informedTS")]
        public string InformedTS { get; set; }

        [JsonProperty("probableEntry")]
        public string ProbableEntry { get; set; }

        [JsonProperty("smsPayload")]
        public string SmsPayload { get; set; }

        [JsonProperty("networkTS")]
        public string NetworkTS { get; set; }

        [JsonProperty("smstype")]
        public int Smstype { get; set; }
    }

    public class ohrm_attendance_record
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("punch_in_utc_time")]
        public string PunchInUtcTime { get; set; }

        [JsonProperty("punch_in_note")]
        public string PunchInNote { get; set; }

        [JsonProperty("punch_in_time_offset")]
        public string PunchInTimeOffset { get; set; }

        [JsonProperty("punch_in_user_time")]
        public string PunchInUserTime { get; set; }

        [JsonProperty("punch_out_utc_time")]
        public string PunchOutUtcTime { get; set; }

        [JsonProperty("punch_out_note")]
        public string PunchOutNote { get; set; }

        [JsonProperty("punch_out_time_offset")]
        public string PunchOutTimeOffset { get; set; }

        [JsonProperty("punch_out_user_time")]
        public string PunchOutUserTime { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("late")]
        public string Late { get; set; }

        [JsonProperty("earlyleft")]
        public string Earlyleft { get; set; }

        [JsonProperty("loginip")]
        public string Loginip { get; set; }

        [JsonProperty("workloc")]
        public string Workloc { get; set; }

        [JsonProperty("lateexcuse")]
        public string Lateexcuse { get; set; }

        [JsonProperty("workHour")]
        public string WorkHour { get; set; }

        [JsonProperty("late_leave_count")]
        public string LateLeaveCount { get; set; }
    }

    public class btl_working_date
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("workdate")]
        public string Workdate { get; set; }

        [JsonProperty("status")]
        //public string Status { get; set; }
        public int Status { get; set; }
    }

}

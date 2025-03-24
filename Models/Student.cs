namespace AttendanceSystem.Models
{
    public class Student
    {
        public int StudentID { get; set; }
        public string Name { get; set; }
        public int? FingerprintID { get; set; }
        public ICollection<Attendance> Attendances { get; set; }

    }
}

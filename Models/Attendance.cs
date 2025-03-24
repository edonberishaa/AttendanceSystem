namespace AttendanceSystem.Models
{
    public class Attendance
    {
        public int AttendanceID { get; set; }
        public int StudentID { get; set; }
        public Student Student { get; set; }
        public int SubjectID { get; set; }
        public Subject Subject { get; set; }
        public DateTime LessonDate { get; set; }
        public bool Present { get; set; }

    }
}

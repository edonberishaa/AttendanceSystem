using AttendanceSystem.Models;

namespace AttendanceSystem.DTOs
{
    public class AttendanceDTO
    {
        public int AttendanceID { get; set; }
        public int StudentID { get; set; }
        public int SubjectID { get; set; }
        public DateTime LessonDate { get; set; }
        public bool Present { get; set; }

    }
}

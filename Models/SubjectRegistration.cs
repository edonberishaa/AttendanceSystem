namespace AttendanceSystem.Models
{
    public class SubjectRegistration
    {
        public int SubjectRegistrationID { get; set; }
        public int StudentID { get; set; }
        public int SubjectID { get; set; }

        public virtual Student Student { get; set; }
        public virtual Subject Subject { get; set; }
    }
}

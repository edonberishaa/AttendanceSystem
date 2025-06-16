using System;
using System.ComponentModel.DataAnnotations;

namespace AttendanceSystem.Models
{
    public class SessionState
    {
        [Key]
        public int SubjectID { get; set; } // Foreign key to Subjects table
        public bool IsActive { get; set; } // Indicates if the session is active
        public DateTime StartDate { get; set; } // Start date of the session
        public DateTime? EndDate { get; set; } // End date of the session (nullable)
        public virtual Subject Subject { get; set; }
    }
}
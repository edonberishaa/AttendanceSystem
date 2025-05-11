using System.ComponentModel.DataAnnotations;

namespace AttendanceSystem.ViewModels
{
    public class RegisterVM
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }

        [Required(ErrorMessage ="Please select a role!")]
        public string Role { get; set; }

    }
}

using AttendanceSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceSystem.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        public AdminController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult AssignSubject()
        {
            var professors = _context.Users.ToList();
            ViewBag.Professors = professors;
            return View();
        }
        [HttpPost]
        public IActionResult AssignSubject(int subjectId,string professorId)
        {
            var subject = _context.Subjects.FirstOrDefault(s => s.SubjectID == subjectId);
            if(subject != null)
            {
                subject.ProfessorID = professorId;
                _context.SaveChanges();
            }
            return RedirectToAction("Index", "Home");
        }
    }
}

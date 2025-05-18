using AttendanceSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Required for Include and ToListAsync
using AttendanceSystem.Data; // ✅ This is likely where ApplicationDbContext is defined

namespace AttendanceSystem.Controllers
{
    public class InsightsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context; public InsightsController(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var subjects = isAdmin
                ? await _context.Subjects.Include(s => s.Professor).ToListAsync()
                : await _context.Subjects
                    .Where(s => s.ProfessorID == user.Id)
                    .Include(s => s.Professor)
                    .ToListAsync();

            return View("Insights", subjects);
        }
        public async Task<IActionResult> GetAttendanceData()
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var query = _context.Subjects.AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(s => s.ProfessorID == user.Id);
            }

            var data = await query.Select(s => new {
                subjectId = s.SubjectID,
                presentCount = _context.Attendances.Count(a => a.SubjectID == s.SubjectID && a.Present == true),
                absentCount = _context.Attendances.Count(a => a.SubjectID == s.SubjectID && a.Present == false)
            }).ToListAsync();

            return Json(data);
        }

    }
}


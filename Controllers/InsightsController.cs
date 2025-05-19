using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers
{
    public class InsightsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public InsightsController(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            List<Subject> subjects;

            if (isAdmin)
            {
                subjects = await _context.Subjects.ToListAsync();
            }
            else
            {
                subjects = await _context.Subjects
                    .Where(s => s.ProfessorID == user.Id)
                    .ToListAsync();
            }

            return View("Insights", subjects);
        }

        [HttpGet]
        public async Task<IActionResult> GetProfessors()
        {
            var professors = await _userManager.GetUsersInRoleAsync("Professor");

            var result = professors.Select(p => new
            {
                id = p.Id,
                name = p.FullName // match this to the "FullName" column
            });

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetSubjects(string professorId = null)
        {
            var query = _context.Subjects.AsQueryable();

            if (!string.IsNullOrEmpty(professorId))
            {
                query = query.Where(s => s.ProfessorID == professorId);
            }

            var subjects = await query.ToListAsync();
            return Json(subjects.Select(s => new { SubjectID = s.SubjectID, Name = s.SubjectName }));
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceData(
            [FromQuery] List<int> subjectIds,
            [FromQuery] DateTime? date)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var query = _context.Attendances.AsQueryable();

            // Role-based filtering
            if (!isAdmin)
            {
                // Professor: Only their subjects
                var professorSubjects = await _context.Subjects
                    .Where(s => s.ProfessorID == user.Id)
                    .Select(s => s.SubjectID)
                    .ToListAsync();

                subjectIds = subjectIds.Any()
                    ? subjectIds.Intersect(professorSubjects).ToList()
                    : professorSubjects;

                query = query.Where(a => subjectIds.Contains(a.SubjectID));
            }
            else if (subjectIds.Any())
            {
                // Admin: Filter by selected subjects
                query = query.Where(a => subjectIds.Contains(a.SubjectID));
            }

            // Date filter
            if (date.HasValue)
            {
                query = query.Where(a => a.LessonDate == date.Value);
            }

            var result = await query
                .GroupBy(a => a.SubjectID)
                .Select(g => new
                {
                    SubjectId = g.Key,
                    PresentCount = g.Count(a => a.Present),
                    AbsentCount = g.Count(a => !a.Present)
                })
                .ToListAsync();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableDates([FromQuery] List<int> subjectIds)
        {
            var dates = await _context.Attendances
                .Where(a => subjectIds.Contains(a.SubjectID))
                .Select(a => a.LessonDate)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            return Json(dates);
        }
    }
}
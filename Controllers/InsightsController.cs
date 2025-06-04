using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Authorization;
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
                name = p.FullName
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
            return Json(subjects.Select(s => new
            {
                subjectID = s.SubjectID,
                name = s.SubjectName
            }));
        }

        [HttpGet]
        public IActionResult GetAttendanceData([FromQuery] List<int> subjectIds, [FromQuery] string date)
        {
            var query = _context.Attendances
                .Include(a => a.Subject)
                .Where(a => subjectIds.Contains(a.SubjectID));

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
            {
                query = query.Where(a => a.LessonDate.Date == parsedDate.Date);
            }

            var result = query
                .GroupBy(a => new { a.SubjectID, a.Subject.SubjectName })
                .Select(g => new
                {
                    subjectId = g.Key.SubjectID,
                    subjectName = g.Key.SubjectName,
                    presentCount = g.Count(a => a.Present),
                    absentCount = g.Count(a => !a.Present)
                })
                .ToList();

            return Json(result);
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetAvailableDates([FromQuery] List<int> subjectIds)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            subjectIds ??= new List<int>();

            if (!isAdmin)
            {
                var professorSubjects = await _context.Subjects
                    .Where(s => s.ProfessorID == user.Id)
                    .Select(s => s.SubjectID)
                    .ToListAsync();

                subjectIds = subjectIds.Any()
                    ? subjectIds.Intersect(professorSubjects).ToList()
                    : professorSubjects;
            }

            if (!subjectIds.Any())
            {
                return Json(new List<string>());
            }

            // Get dates as DateTime first
            var dateTimes = await _context.Attendances
                .Where(a => subjectIds.Contains(a.SubjectID))
                .Select(a => a.LessonDate.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            // Convert to ISO strings in memory
            var dates = dateTimes
                .Select(d => d.ToString("yyyy-MM-dd"))
                .ToList();

            return Json(dates);
        }
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetAttendanceTrend([FromQuery] List<int> subjectIds)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (!isAdmin)
            {
                var professorSubjects = await _context.Subjects
                    .Where(s => s.ProfessorID == user.Id)
                    .Select(s => s.SubjectID)
                    .ToListAsync();

                subjectIds = subjectIds?.Any() == true
                    ? subjectIds.Intersect(professorSubjects).ToList()
                    : professorSubjects;
            }

            // Database query (server-side)
            var trendData = await _context.Attendances
                .Where(a => subjectIds.Contains(a.SubjectID))
                .GroupBy(a => a.LessonDate.Date)
                .Select(g => new
                {
                    Date = g.Key, // Keep as DateTime
                    PresentCount = g.Sum(x => x.Present ? 1 : 0),
                    AbsentCount = g.Sum(x => x.Present ? 0 : 1)
                })
                .OrderBy(x => x.Date)
                .ToListAsync(); // Execute query here

            // Client-side processing
            var formattedData = trendData
                .Select(x => new
                {
                    Date = x.Date.ToString("yyyy-MM-dd"), // Format in memory
                    x.PresentCount,
                    x.AbsentCount
                })
                .ToList();

            return Json(formattedData);
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceDistribution([FromQuery] List<int> subjectIds)
        {
            var students = await _context.Students.ToListAsync();
            var percentages = new List<double>();

            foreach (var student in students)
            {
                double percent = await CalculateAttendancePercentageAsync(student.StudentID, subjectIds);
                percentages.Add(percent);
            }

            return Json(new
            {
                Good = percentages.Count(p => p > 75),
                Average = percentages.Count(p => p >= 50 && p <= 75),
                Poor = percentages.Count(p => p < 50)
            });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetProfessorPerformance()
        {
            var professors = await _userManager.GetUsersInRoleAsync("Professor");
            var performanceList = new List<object>();

            foreach (var p in professors)
            {
                var subjects = await _context.Subjects
                    .Where(s => s.ProfessorID == p.Id)
                    .Select(s => s.SubjectID)
                    .ToListAsync();

                var total = await _context.Attendances
                    .CountAsync(a => subjects.Contains(a.SubjectID));

                var present = await _context.Attendances
                    .CountAsync(a => subjects.Contains(a.SubjectID) && a.Present);

                var attendanceRate = total > 0
                    ? Math.Round((present / (double)total) * 100, 2)
                    : 0;

                performanceList.Add(new
                {
                    Professor = p.FullName,
                    AttendanceRate = attendanceRate
                });
            }

            return Json(performanceList);
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentWiseAttendance([FromQuery] List<int> subjectIds)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (!isAdmin)
            {
                var professorSubjects = await _context.Subjects
                    .Where(s => s.ProfessorID == user.Id)
                    .Select(s => s.SubjectID)
                    .ToListAsync();

                subjectIds = subjectIds?.Any() == true
                    ? subjectIds.Intersect(professorSubjects).ToList()
                    : professorSubjects;
            }

            if (!subjectIds?.Any() ?? true)
                return Json(new List<object>());

            // Changed to use Name instead of FullName
            var data = await _context.Attendances
                .Where(a => subjectIds.Contains(a.SubjectID))
                .GroupBy(a => a.StudentID)
                .Select(g => new
                {
                    StudentID = g.Key,
                    Total = g.Count(),
                    Present = g.Count(x => x.Present)
                })
                .Join(_context.Students,
                    a => a.StudentID,
                    s => s.StudentID,
                    (a, s) => new
                    {
                        Name = s.Name, // This is the changed line
                        Percentage = a.Total > 0
                            ? Math.Round((a.Present / (double)a.Total) * 100, 2)
                            : 0
                    })
                .ToListAsync();

            return Json(data);
        }

        private async Task<double> CalculateAttendancePercentageAsync(int studentId, List<int> subjectIds)
        {
            var total = await _context.Attendances
                .CountAsync(a => subjectIds.Contains(a.SubjectID) && a.StudentID == studentId);

            if (total == 0) return 0;

            var present = await _context.Attendances
                .CountAsync(a => subjectIds.Contains(a.SubjectID) && a.StudentID == studentId && a.Present);

            return Math.Round((present / (double)total) * 100, 2);
        }
    }
}
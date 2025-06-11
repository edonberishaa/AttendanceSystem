using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AttendanceSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


namespace AttendanceSystem.Controllers
{
    public class InsightsController : Controller
    {
        private readonly AppDbContext _context;

        public InsightsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (isAdmin)
            {
                ViewBag.Subjects = _context.Subjects.ToList();
            }
            else
            {
                ViewBag.Subjects = _context.Subjects
                    .Where(s => s.ProfessorID == userId)
                    .ToList();
            }

            ViewBag.Dates = _context.Attendances
                .Select(a => a.LessonDate.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            return View("Insights");
        }


        [HttpGet]
        public JsonResult GetAttendanceData(List<int> subjectIds, DateTime? date)
        {
            var query = _context.Attendances
                .Include(a => a.Subject)
                .AsQueryable();

            if (subjectIds != null && subjectIds.Any())
            {
                query = query.Where(a => subjectIds.Contains(a.SubjectID));
            }

            if (date.HasValue)
            {
                query = query.Where(a => a.LessonDate.Date == date.Value.Date);
            }

            var groupedData = query
                .GroupBy(a => a.Subject)
                .Select(g => new
                {
                    subjectName = g.Key.SubjectName,
                    presentCount = g.Count(r => r.Present),
                    absentCount = g.Count(r => !r.Present)
                })
                .ToList();

            return Json(groupedData);
        }



        [HttpGet]
        public JsonResult GetAvailableDates([FromQuery] List<int> subjectIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var query = _context.Attendances
                .Include(a => a.Subject)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(a => a.Subject.ProfessorID == userId);
            }

            var dates = query
                .Where(a => subjectIds.Contains(a.SubjectID))
                .Select(a => a.LessonDate.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            return Json(dates);
        }

        [HttpGet]
        public JsonResult GetAttendanceTrend(int subjectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var query = _context.Attendances
                .Include(a => a.Subject)
                .Where(a => a.SubjectID == subjectId);

            if (!isAdmin)
            {
                query = query.Where(a => a.Subject.ProfessorID == userId);
            }

            var trendData = query
                .GroupBy(a => a.LessonDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    presentCount = g.Count(r => r.Present),
                    absentCount = g.Count(r => !r.Present)
                })
                .ToList();

            return Json(trendData);
        }

        [HttpGet]
        public JsonResult GetAverageAttendanceRate([FromQuery] List<int> subjectIds)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");
                var query = _context.Attendances
                    .Include(a => a.Subject)
                    .AsQueryable();

                if (!isAdmin)
                {
                    query = query.Where(a => a.Subject.ProfessorID == userId);
                }

                if (subjectIds != null && subjectIds.Any())
                {
                    query = query.Where(a => subjectIds.Contains(a.SubjectID));
                }

                var result = query
                    .GroupBy(a => a.Subject)
                    .Select(g => new
                    {
                        subjectName = g.Key.SubjectName,
                        attendanceRate = g.Count(a => a.Present) * 100.0 / g.Count()
                    })
                    .OrderByDescending(x => x.attendanceRate)
                    .ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        public JsonResult GetStudentAttendancePercentages(int subjectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var query = _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Subject)
                .Where(a => a.SubjectID == subjectId)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(a => a.Subject.ProfessorID == userId);
            }

            var result = query
                .GroupBy(a => new { a.StudentID, a.Student.Name })
                .Select(g => new
                {
                    studentName = g.Key.Name,
                    attendanceRate = g.Count(a => a.Present) * 100.0 / g.Count()
                })
                .OrderBy(x => x.studentName)
                .ToList();

            return Json(result);
        }


        [HttpGet]
        public JsonResult GetAttendanceDistributionByDay()
        {
            try
            {
                var data = _context.Attendances
                    .AsEnumerable()
                    .GroupBy(a => a.LessonDate.DayOfWeek)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Day = g.Key.ToString(),
                        AttendanceRate = g.Count(a => a.Present) * 100.0 / g.Count()
                    })
                    .ToList();

                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }









    }
}
    

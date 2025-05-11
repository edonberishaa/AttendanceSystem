using AttendanceSystem.Data;
using AttendanceSystem.Hubs;
using AttendanceSystem.Models;
using AttendanceSystem.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading.Tasks;

namespace AttendanceSystem.Controllers
{
    [Authorize]
    public class ProfessorController : Controller
    {
        private readonly ArduinoService _arduino;
        private readonly AppDbContext _context;
        private static SerialPort? _serialPort;
        private static ConcurrentQueue<string> SerialLogs = new ConcurrentQueue<string>();
        private readonly IHubContext<ArduinoHub> _arduinoHub;
        private readonly UserManager<ApplicationUser> _userManager;

        public static class ArduinoHelper
        {
            public static bool IsConnected = false;
        }
        public ProfessorController(AppDbContext context, IHubContext<ArduinoHub> arduinoHub, ArduinoService arduino, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _arduinoHub = arduinoHub;
            _arduino = arduino;
            _userManager = userManager;
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult AllProfessors()
        {
            var users = (from u in _context.Users
                         join ur in _context.UserRoles on u.Id equals ur.UserId into userRoles
                         from ur in userRoles.DefaultIfEmpty() // LEFT JOIN
                         join r in _context.Roles on ur.RoleId equals r.Id into roles
                         from r in roles.DefaultIfEmpty() // LEFT JOIN
                         where r == null || r.NormalizedName != "ADMIN"
                         select u).Distinct().ToList();
            return View(users);
        }
        [HttpPost]
        public IActionResult RemoveProfessor(string professorID)
        {
            var professor = _context.Users.FirstOrDefault(u => u.Id == professorID);

            if (professor == null)
            {
                return NotFound("Professor not found.");
            }

            // Remove all subjects and related attendances associated with the professor
            var subjects = _context.Subjects.Where(s => s.ProfessorID == professorID).ToList();
            foreach (var subject in subjects)
            {
                var attendances = _context.Attendances.Where(a => a.SubjectID == subject.SubjectID);
                _context.Attendances.RemoveRange(attendances);
                _context.Subjects.Remove(subject);
            }

            // Remove the professor from the Users table
            _context.Users.Remove(professor);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Professor removed successfully.";
            return RedirectToAction("AllProfessors");
        }

        [HttpPost]
        public async Task<IActionResult> StartSession(int subjectId)
        {
            var professorEmail = User.Identity.Name;
            var professor = _context.Users.FirstOrDefault(u => u.Email == professorEmail);

            if (professor == null)
            {
                return NotFound("Professor not found.");
            }

            var subject = _context.Subjects.FirstOrDefault(s => s.SubjectID == subjectId && s.ProfessorID == professor.Id);
            if (subject == null)
            {
                return NotFound("Subject not found or not assigned to you.");
            }
            var session = _context.SessionStates.FirstOrDefault(s => s.SubjectID == subjectId);
            if (session == null)
            {
                session = new SessionState
                {
                    SubjectID = subjectId,
                    IsActive = true,
                    StartDate = DateTime.Now
                };
                _context.SessionStates.Add(session);
            }
            else
            {
                session.IsActive = true;
                session.StartDate = DateTime.Now;
                session.EndDate = null;
            }
            await _context.SaveChangesAsync();
            await _arduinoHub.Clients.All.SendAsync("SessionStarted", subjectId);
            bool success = _arduino.SendCommand("VERIFY");
            return RedirectToAction("AttendanceDashboard", new { subjectId });
        }

        [HttpPost]
        public async Task<IActionResult> EndSession(int subjectId)
        {
            var professorEmail = User.Identity.Name;
            var professor = _context.Users.FirstOrDefault(u => u.Email == professorEmail);

            if (professor == null)
            {
                return NotFound("Professor not found.");
            }

            var subject = _context.Subjects.FirstOrDefault(s => s.SubjectID == subjectId && s.ProfessorID == professor.Id);
            if (subject == null)
            {
                return NotFound("Subject not found or not assigned to you.");
            }
            var session = _context.SessionStates.FirstOrDefault(s => s.SubjectID == subjectId);

            if (session != null && session.IsActive)
            {
                session.IsActive = false;
                session.EndDate = DateTime.Now;

                var enrolledStudents = _context.Attendances
                    .Where(a => a.SubjectID == subjectId)
                    .Select(a => a.StudentID)
                    .Distinct()
                    .ToList();
                var lessonDate = DateTime.Today;

                foreach (var studentId in enrolledStudents)
                {
                    var attendaceRecord = _context.Attendances
                        .FirstOrDefault(a =>
                        a.StudentID == studentId &&
                        a.SubjectID == subjectId &&
                        a.LessonDate == lessonDate);

                    if (attendaceRecord == null)
                    {
                        _context.Attendances.Add(new Attendance
                        {
                            StudentID = studentId,
                            SubjectID = subjectId,
                            LessonDate = lessonDate,
                            Present = false
                        });
                    }
                    else if (!attendaceRecord.Present)
                    {
                        attendaceRecord.Present = false;
                    }
                }
                await _context.SaveChangesAsync();
                await _arduinoHub.Clients.All.SendAsync("SessionEnded", subjectId);
                bool success = _arduino.SendCommand("EndSession");
            }
            return RedirectToAction("AttendanceDashboard", new { subjectId });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyFingerprint(int fingerprintId, int subjectId)
        {
            var professorEmail = User.Identity.Name;
            var professor = _context.Users.FirstOrDefault(u => u.Email == professorEmail);
            if (professor == null)
            {
                return NotFound("Professor not found.");
            }

            var subject = _context.Subjects.FirstOrDefault(s => s.SubjectID == subjectId && s.ProfessorID == professor.Id);
            if (subject == null)
            {
                return NotFound("Subject not found or not assigned to you!");
            }

            var session = _context.SessionStates.FirstOrDefault(s => s.SubjectID == subjectId && s.IsActive);
            if (session == null || !session.IsActive)
            {
                return Json(new { success = false, message = "Session is not active. Cannot mark attendance." });
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.FingerprintID == fingerprintId);
            if (student == null)
            {
                return Json(new { success = false, message = "Student not found with the provided fingerprint ID." });
            }
            var enrollment = await _context.Attendances
                .FirstOrDefaultAsync(a => a.StudentID == student.StudentID && a.SubjectID == subjectId);

            if (enrollment == null)
            {
                return Json(new { success = false, message = "Student is not enrolled in this subject." });
            }

            var lessonDate = DateTime.Today;

            var attendanceRecord = await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.StudentID == student.StudentID &&
                    a.SubjectID == subjectId &&
                    a.LessonDate == lessonDate);

            if (attendanceRecord == null)
            {
                attendanceRecord = new Attendance
                {
                    StudentID = student.StudentID,
                    SubjectID = subjectId,
                    LessonDate = lessonDate,
                    Present = true
                };
                _context.Attendances.Add(attendanceRecord);
            }
            else
            {
                attendanceRecord.Present = true;
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Attendace marked successfully" });
        }
        [HttpGet]
        public IActionResult RegisterStudent(int subjectId)
        {
            var professorEmail = User.Identity.Name;

            var professor = _context.Users.FirstOrDefault(u => u.Email == professorEmail);
            if (professor == null)
            {
                return NotFound("Professor not found.");
            }

            var subject = _context.Subjects.FirstOrDefault(s => s.SubjectID == subjectId && s.ProfessorID == professor.Id);
            if (subject == null)
            {
                return NotFound("Subject not found or not assigned to you.");
            }

            ViewBag.SubjectID = subjectId;
            ViewBag.SubjectName = subject.SubjectName;

            var registeredStudentIds = _context.Attendances
                .Where(a => a.SubjectID == subjectId)
                .Select(a => a.StudentID)
                .ToList();

            var unregisteredStudents = _context.Students
                .Where(s => !registeredStudentIds.Contains(s.StudentID))
                .ToList();

            return View(unregisteredStudents);
        }
        [HttpPost]
        public IActionResult RegisterStudent(int subjectId, int studentId)
        {
            Console.WriteLine($"subjectId : {subjectId}");
            Console.WriteLine($"studentid : {studentId}");

            var professorEmail = User.Identity.Name;

            var professor = _context.Users.FirstOrDefault(u => u.Email == professorEmail);
            if (professor == null)
            {
                return NotFound("Professor not found.");
            }

            var subject = _context.Subjects.FirstOrDefault(s => s.SubjectID == subjectId && s.ProfessorID == professor.Id);
            if (subject == null)
            {
                return NotFound("Subject not found or not assigned to you.");
            }

            var existingRecord = _context.Attendances.FirstOrDefault(a =>
                a.StudentID == studentId &&
                a.SubjectID == subjectId);

            if (existingRecord != null)
            {
                ModelState.AddModelError("", "This student is already registered in the subject.");
                return RedirectToAction("RegisterStudent", new { subjectId });
            }

            var attendance = new Attendance
            {
                StudentID = studentId,
                SubjectID = subjectId,
                LessonDate = DateTime.Today,
                Present = true
            };

            _context.Attendances.Add(attendance);
            _context.SaveChanges();

            return RedirectToAction("ViewStudents", new { subjectId });
        }

        [HttpPost]
        public IActionResult RemoveStudent(int subjectId, int studentId)
        {
            var professorEmail = User.Identity.Name;
            var professor = _context.Users.FirstOrDefault(u => u.Email == professorEmail);

            if (professor == null)
            {
                return NotFound("Professor not found.");
            }

            var subject = _context.Subjects.FirstOrDefault(s => s.SubjectID == subjectId && s.ProfessorID == professor.Id);
            if (subject == null)
            {
                return NotFound("Subject not found or not assigned to you.");
            }
            var attendanceRecord = _context.Attendances.FirstOrDefault(a => a.StudentID == studentId && a.SubjectID == subjectId);
            if (attendanceRecord == null)
            {
                return NotFound("Student is not registered in this subject.");
            }
            _context.Attendances.Remove(attendanceRecord);
            _context.SaveChanges();

            return RedirectToAction("ViewStudents", new { subjectId });
        }
        public async Task<IActionResult> Dashboard()
        {
            var professorEmail = User.Identity.Name;
            var user = await _userManager.GetUserAsync(User);
            ViewBag.ProfessorFullName = user.FullName;

            var professor = _context.Users.FirstOrDefault(u => u.Email == professorEmail);
            if (professor == null)
            {
                return NotFound("Professor not found!");
            }
            var subjects = _context.Subjects
                .Where(s => s.ProfessorID == professor.Id)
                .ToList();
            return View(subjects);
        }
        public IActionResult ViewStudents(int subjectId)
        {
            var professorEmail = User.Identity.Name;
            var professor = _context.Users.FirstOrDefault(u => u.Email == professorEmail);

            if (professor == null)
            {
                return NotFound("Professor not found!");
            }
            var subject = _context.Subjects
                .FirstOrDefault(s => s.SubjectID == subjectId && s.ProfessorID == professor.Id);
            if (subject == null)
            {
                return NotFound("Subject not found or not assigned to you!");
            }
            var students = _context.Attendances
               .Where(a => a.SubjectID == subjectId)
               .Include(a => a.Student)
               .Select(a => a.Student)
               .Distinct()
               .ToList();

            ViewBag.SubjectName = subject.SubjectName;
            ViewBag.SubjectId = subjectId;
            return View(students);
        }
        public IActionResult AttendanceDashboard(int subjectId, DateTime? date = null, int? week = null)
        {
            var professorEmail = User.Identity.Name;

            var professor = _context.Users.FirstOrDefault(u => u.Email == professorEmail);
            if (professor == null)
            {
                return NotFound("Professor not found.");
            }
            var subject = _context.Subjects
                .FirstOrDefault(s => s.SubjectID == subjectId && s.ProfessorID == professor.Id);
            if (subject == null)
            {
                return NotFound("Subject not found or not assigned to you!");
            }
            ViewBag.SubjectName = subject.SubjectName;
            ViewBag.SubjectId = subjectId;

            var attendanceQuery = _context.Attendances
                .Where(a => a.SubjectID == subjectId)
                .Include(a => a.Student)
                .AsQueryable(); // Add AsQueryable() to ensure the type is IQueryable<Attendance>

            if (date.HasValue)
            {
                attendanceQuery = attendanceQuery.Where(a => a.LessonDate == date.Value.Date); // Use .Date to compare only the date part
            }
            else if (week.HasValue)
            {
                // Calculate start and end dates for the selected week
                var startDate = new DateTime(DateTime.Today.Year, 1, 1).AddDays((week.Value - 1) * 7);
                var endDate = startDate.AddDays(6);

                attendanceQuery = attendanceQuery.Where(a => a.LessonDate >= startDate && a.LessonDate <= endDate);
            }
            var attendanceRecords = attendanceQuery.ToList();

            return View(attendanceRecords);
        }

        public IActionResult Attendance()
        {
            var professorEmail = User.Identity.Name;
            var professor = _context.Users.FirstOrDefault(u => u.Email == professorEmail);
            if (professor == null)
            {
                return NotFound("Professor not found.");
            }
            var subjects = _context.Subjects.Where(s => s.ProfessorID == professorEmail);
            return View(subjects);
        }

        public IActionResult ExportAttendanceToExcel(int subjectId, int? week, DateTime? date = null)
        {
            var professorEmail = User.Identity.Name;

            var professor = _context.Users.FirstOrDefault(u => u.Email == professorEmail);
            if (professor == null)
            {
                return NotFound("Professor not found.");
            }
            var subject = _context.Subjects.FirstOrDefault(s => s.SubjectID == subjectId);
            if (subject == null)
            {
                return NotFound("Subject not found or not assigned to you!");
            }

            var attendanceRecords = _context.Attendances
                .Where(a => a.SubjectID == subjectId)
                .Include(a => a.Student)
                .AsQueryable();

            if (date.HasValue)
            {
                attendanceRecords = attendanceRecords.Where(a => a.LessonDate == date.Value.Date);
            }
            else if (week.HasValue)
            {
                // Calculate start and end dates for the selected week
                var startDate = new DateTime(DateTime.Today.Year, 1, 1).AddDays((week.Value - 1) * 7);
                var endDate = startDate.AddDays(6);

                attendanceRecords = attendanceRecords.Where(a => a.LessonDate >= startDate && a.LessonDate <= endDate);
            }

            var records = attendanceRecords.ToList();

            if (!records.Any())
            {
                return NotFound("NO attendance records found for the selected week");
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Attendance");

                worksheet.Cell(1, 1).Value = "Student Name";
                worksheet.Cell(1, 2).Value = "Date";
                worksheet.Cell(1, 3).Value = "Attendance Status";

                int row = 2;
                foreach (var record in records)
                {
                    worksheet.Cell(row, 1).Value = record.Student.Name;
                    worksheet.Cell(row, 2).Value = record.LessonDate.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 3).Value = record.Present ? "1" : "0";
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Attendance_Date{date}_{subject.SubjectName}.xlsx"
                        );
                }
            }
        }

        [HttpGet]
        public JsonResult GetArduinoStatus()
        {
            bool isConnected = _serialPort != null && _serialPort.IsOpen;
            return Json(new { status = isConnected ? "connected" : "waiting" });
        }

        [HttpGet]
        public IActionResult GetSerialLog()
        {
            return Json(SerialLogs.ToArray());
        }
    }
}

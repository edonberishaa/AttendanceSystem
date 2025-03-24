using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers
{
    [Authorize]
    public class ProfessorController : Controller
    {
        private readonly AppDbContext _context;
        public ProfessorController(AppDbContext context)
        {
            _context = context;
        }
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

        public IActionResult RemoveStudent(int subjectId,int studentId)
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
            if(attendanceRecord == null)
            {
                return NotFound("Student is not registered in this subject.");
            }
            _context.Attendances.Remove(attendanceRecord);
            _context.SaveChanges();

            return RedirectToAction("ViewStudents",new {subjectId});
        }

        public IActionResult Dashboard()
        {
            var professorEmail = User.Identity.Name;

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
    }
}

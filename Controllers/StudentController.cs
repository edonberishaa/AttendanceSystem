using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;

        public StudentController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult AllStudents()
        {
            var students = _context.Students.ToList();
            return View(students);
        }
        public IActionResult AddStudent()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddStudent(string name,int? fingerprintId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Name is required");
                return View();
            }
            var student = new Student
            {
                Name = name,
                FingerprintID = fingerprintId
            };
            _context.Students.Add(student);
            _context.SaveChanges();
            TempData["Message"] = "Student added sucessfully!";
            return RedirectToAction("AllStudents");
        }
        public IActionResult EditStudent(int id)
        {
            var student = _context.Students.FirstOrDefault(s => s.StudentID == id);
            if (student == null)
            {
                return NotFound("Student not found");
            }
            return View(student);
        }
        [HttpPost]
        public IActionResult EditStudent(int id,string name,int? fingerprintId)
        {
            var student = _context.Students.FirstOrDefault(s => s.StudentID==id);
            if (student == null)
            {
                return NotFound("Student not found");
            }

            student.Name = name;
            student.FingerprintID = fingerprintId;

            _context.SaveChanges();
            return RedirectToAction("AllStudents");
        }
        public IActionResult DeleteStudent(int id)
        {
            var student = _context.Students.FirstOrDefault(s => s.StudentID == id);
            if (student == null)
            {
                return NotFound("Student not found");
            }
            _context.Students.Remove(student);
            _context.SaveChanges();
            return RedirectToAction("AllStudents");
        }
    }
}

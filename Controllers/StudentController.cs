using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Ports;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using AttendanceSystem.Services;
using NuGet.Packaging.Core;

namespace AttendanceSystem.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly ArduinoService _arduino;
        private readonly AppDbContext _context;
        private static ConcurrentQueue<string> SerialLogs = new ConcurrentQueue<string>();
        private readonly IHubContext<SerialHub> _hubContext;

        public StudentController(AppDbContext context, IHubContext<SerialHub> hubContext, ArduinoService arduino)
        {
            _context = context;
            _hubContext = hubContext;
            _arduino = arduino;
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
        public IActionResult AddStudent(string name, int? fingerprintId)
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
            TempData["Message"] = "Student added successfully!";
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
        public IActionResult EditStudent(int id, string name, int? fingerprintId)
        {
            var student = _context.Students.FirstOrDefault(s => s.StudentID == id);
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

        [HttpGet]
        public JsonResult GetArduinoStatus()
        {
            return Json(new { status = _arduino.IsArduinoConnected() ? "connected" : "waiting" });
        }

        [HttpPost]
        public IActionResult TriggerEnrollment()
        {
            bool success = _arduino.SendCommand("enroll");
            return Json(new
            {
                success,
                message = success ? "Started fingerprint enrollment." : "Could not connect to Arduino."
            });
        }


        [HttpGet]
        public IActionResult GetSerialLog()
        {
            return Json(SerialLogs.ToArray());
        }
    }
}

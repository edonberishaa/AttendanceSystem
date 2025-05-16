using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Ports;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using AttendanceSystem.Services;
using NuGet.Packaging.Core;
using AttendanceSystem.Hubs;
using Newtonsoft.Json;

namespace AttendanceSystem.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly ArduinoService _arduino;
        private readonly AppDbContext _context;
        private static ConcurrentQueue<string> SerialLogs = new ConcurrentQueue<string>();
        private readonly IHubContext<ArduinoHub> _arduinoContext;

        public StudentController(AppDbContext context, IHubContext<ArduinoHub> hubContext, ArduinoService arduino)
        {
            _context = context;
            _arduinoContext = hubContext;
            _arduino = arduino;
        }

        [HttpGet]
        public IActionResult AllStudents()
        {
            var students = _context.Students.ToList();
            return View(students);
        }

        [Authorize(Roles ="Admin")]
        [HttpGet]
        public IActionResult AddStudent()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult AddStudent(string name, int fingerprintId)
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
        [Authorize(Roles = "Admin")]
        public IActionResult EditStudent(int id)
        {
            var student = _context.Students.FirstOrDefault(s => s.StudentID == id);
            if (student == null)
            {
                return NotFound("Student not found");
            }
            return View(student);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult EditStudent(int id, string name, int fingerprintId)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteStudent(int id)
        {
            var student = _context.Students.FirstOrDefault(s => s.StudentID == id);
            if (student == null)
            {
                return NotFound("Student not found");
            }
            HttpContext.Session.SetString("DeletedStudent",JsonConvert.SerializeObject(student));
            
            _context.Students.Remove(student);
            _context.SaveChanges();

            TempData["DeletedStudent"] = "true";
            TempData["DeletedStudentName"] = student.Name;

            return RedirectToAction("AllStudents");
        }
        [HttpPost]
        public IActionResult UndoDelete()
        {
            var deletedStudentJson = HttpContext.Session.GetString("DeletedStudent");

            if(!string.IsNullOrEmpty(deletedStudentJson))
            {
                var deletedStudent = JsonConvert.DeserializeObject<Student>(deletedStudentJson);

                _context.Students.Add(deletedStudent);
                _context.SaveChanges();

                HttpContext.Session.Remove("DeletedStudent");

                return Json(new {success = true});
            }
            return Json(new {success = false});
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
        public IActionResult IsArduinoConnected()
        {
            bool isConnected = _arduino.IsArduinoConnected();
            return Json(new {connected = isConnected});
        }


        [HttpGet]
        public IActionResult GetSerialLog()
        {
            return Json(SerialLogs.ToArray());
        }
    }
}

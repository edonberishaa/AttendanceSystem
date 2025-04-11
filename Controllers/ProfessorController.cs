using AttendanceSystem.Data;
using AttendanceSystem.Hubs;
using AttendanceSystem.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.IO.Ports;

namespace AttendanceSystem.Controllers
{
    [Authorize]
    public class ProfessorController : Controller
    {
        private readonly AppDbContext _context;
        private static SerialPort _serialPort;
        private static ConcurrentQueue<string> SerialLogs = new ConcurrentQueue<string>();
        private readonly IHubContext<ArduinoHub> _arduinoHub;

        public static class ArduinoHelper
        {
            public static bool IsConnected = false;
        }
        public ProfessorController(AppDbContext context, IHubContext<ArduinoHub> arduinoHub)
        {
            _context = context;
            _arduinoHub = arduinoHub;
            Task.Run(() => InitializeSerialPort());
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

        public IActionResult ExportAttendanceToExcel(int subjectId,int? week,DateTime? date = null)
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

            using(var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Attendance");

                worksheet.Cell(1, 1).Value = "Student Name";
                worksheet.Cell(1, 2).Value = "Date";
                worksheet.Cell(1, 3).Value = "Attendance Status";

                int row = 2;
                foreach(var record in records)
                {
                    worksheet.Cell(row, 1).Value = record.Student.Name;
                    worksheet.Cell(row, 2).Value = record.LessonDate.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 3).Value = record.Present ? "1" : "0";
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                using(var stream = new MemoryStream())
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

        private async Task InitializeSerialPort()
        {
            string arduinoPort = null;
            string[] previousPorts = SerialPort.GetPortNames();

            while (arduinoPort == null)
            {
                string[] currentPorts = SerialPort.GetPortNames();
                arduinoPort = FindNewPort(previousPorts, currentPorts);
                previousPorts = currentPorts;

                if (arduinoPort != null && VerifyArduino(arduinoPort))
                {
                    ArduinoHelper.IsConnected = true;
                    break;
                }

                arduinoPort = null;
                Thread.Sleep(100);
            }

            Console.WriteLine("Arduino connected on port " + arduinoPort);
            _serialPort = new SerialPort(arduinoPort, 9600)
            {
                DtrEnable = true,
                RtsEnable = true
            };

            try
            {
                _serialPort.Open();
                _serialPort.DataReceived += SerialDataReceived; // Listen for incoming data
                Console.WriteLine("Serial Port Opened.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Opening Serial Port: " + e.Message);
            }
        }

        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    string response = _serialPort.ReadLine().Trim();
                    SerialLogs.Enqueue(response); // Store message in log

                    Console.WriteLine("Received from Arduino: " + response);
                    _arduinoHub.Clients.All.SendAsync("ReceiveSerialLog", response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading from Serial Port: " + ex.Message);
            }
        }

        [HttpPost]
        public IActionResult VerifyFingerprint()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.WriteLine("verify"); // Send command
                    Console.WriteLine("Sent 'verify' command to Arduino.");
                    return Json(new { success = true, message = "Verifying started. Waiting for fingerprint..." });
                }
                return Json(new { success = false, message = "Serial port is not open." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        static string FindNewPort(string[] oldPorts, string[] newPorts)
        {
            foreach (string port in newPorts)
            {
                if (Array.IndexOf(oldPorts, port) == -1)
                {
                    return port;
                }
            }
            return null;
        }

        static bool VerifyArduino(string port)
        {
            try
            {
                using (SerialPort arduino = new SerialPort(port, 9600))
                {
                    arduino.Open();
                    Thread.Sleep(3000); // Allow Arduino to reset

                    for (int i = 0; i < 10; i++) // Try reading multiple lines
                    {
                        if (arduino.BytesToRead > 0)
                        {
                            string data = arduino.ReadLine().Trim();
                            if (data.Contains("ArduinoFingerPrintSensorReady"))
                            {
                                Console.WriteLine("Arduino detected: " + data);
                                return true;
                            }
                        }
                        Thread.Sleep(100); // Wait a bit before trying again
                    }
                }
            }
            catch (Exception)
            {
                // Ignore errors and retry
            }
            return false;
        }

        [HttpGet]
        public IActionResult GetSerialLog()
        {
            return Json(SerialLogs.ToArray());
        }
    }
}

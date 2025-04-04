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
        private readonly AppDbContext _context;
        private static SerialPort _serialPort;
        private static ConcurrentQueue<string> SerialLogs = new ConcurrentQueue<string>();
        private readonly IHubContext<SerialHub> _hubContext;

        public static class ArduinoHelper
        {
            public static bool IsConnected = false;
        }


        public StudentController(AppDbContext context, IHubContext<SerialHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
            Task.Run(() => InitializeSerialPort());
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
                    _hubContext.Clients.All.SendAsync("ReceiveSerialLog", response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading from Serial Port: " + ex.Message);
            }
        }

        [HttpPost]
        public IActionResult EnrollFingerprint()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.WriteLine("enroll"); // Send command
                    Console.WriteLine("Sent 'enroll' command to Arduino.");
                    return Json(new { success = true, message = "Enrollment started. Waiting for fingerprint..." });
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

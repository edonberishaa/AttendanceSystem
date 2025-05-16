using System.Diagnostics;
using AttendanceSystem.Data;
using AttendanceSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AttendanceSystem.Services;

namespace AttendanceSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            RunPythonGraphScript();  // Run the Python graph generator

            var studentsCount = _context.Students.Count();
            var professorsCount = _context.Users
                .Where(user => _context.UserRoles
                    .Any(role => role.UserId == user.Id && role.RoleId == _context.Roles.FirstOrDefault(r => r.Name == "Professor").Id))
                .Count();

            var model = new
            {
                StudentsCount = studentsCount,
                ProfessorsCount = professorsCount
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        private void RunPythonGraphScript()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python", // or full python.exe path if needed
                Arguments = "PythonScripts/generate_graph.py",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Directory.GetCurrentDirectory() // important to set working directory
            };

            using var process = Process.Start(psi);
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            string errors = process.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(errors))
            {
                _logger.LogError("Python script error: " + errors);
            }
            else
            {
                _logger.LogInformation("Python script output: " + output);
            }
        }

    }
}

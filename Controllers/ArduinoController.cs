using Microsoft.AspNetCore.Mvc;
using AttendanceSystem.Services;

namespace AttendanceSystem.Controllers
{
    [Route("Arduino/[action]")]
    public class ArduinoController : Controller
    {
        private readonly ArduinoService _arduinoService;

        public ArduinoController(ArduinoService arduinoService)
        {
            _arduinoService = arduinoService;
        }

        [HttpGet]
        public IActionResult IsConnected()
        {
            return Json(new { connected = _arduinoService.IsConnected });
        }
    }
}

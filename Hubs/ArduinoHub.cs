using Microsoft.AspNetCore.SignalR;

namespace AttendanceSystem.Hubs
{
    public class ArduinoHub : Hub
    {
        public async Task SendSerialLog(string message)
        {
            await Clients.All.SendAsync(message);
        }
    }
}

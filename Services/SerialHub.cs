using Microsoft.AspNetCore.SignalR;

namespace AttendanceSystem.Services
{
    public class SerialHub : Hub
    {
        public async Task SendSerialLog(string message)
        {
            await Clients.All.SendAsync("ReceiveSerialLog", message);
        }
    }
}
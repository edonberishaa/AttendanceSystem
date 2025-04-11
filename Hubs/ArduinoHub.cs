using Microsoft.AspNetCore.SignalR;

namespace AttendanceSystem.Hubs
{
    public class ArduinoHub : Hub
    {
        public async Task NotifySessionStarted(int subjectId)
        {
            await Clients.All.SendAsync("SessionStarted", subjectId);
        }
        public async Task NotifySessionEnded(int subjectId)
        {
            await Clients.All.SendAsync("SessionEnded", subjectId);
        }

        // Notify clients about fingerprint verification results
        public async Task NotifyFingerprintResult(string message)
        {
            await Clients.All.SendAsync("ReceiveVerificationResult", message);
        }
        public async Task SendSerialLog(string message)
        {
            await Clients.All.SendAsync(message);
        }
    }
}

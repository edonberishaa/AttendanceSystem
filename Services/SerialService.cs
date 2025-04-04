using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AttendanceSystem.Services
{
    public class SerialService : IDisposable
    {
        private SerialPort _serialPort;
        private readonly ILogger<SerialService> _logger;
        private readonly IHubContext<SerialHub> _hubContext;
        private bool _isRunning;

        public SerialService(ILogger<SerialService> logger, IHubContext<SerialHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
            _isRunning = true;

            // Start the port detection in background
            Task.Run(() => InitializeSerialPort());
        }

        private async Task InitializeSerialPort()
        {
            string arduinoPort = null;
            string[] previousPorts = SerialPort.GetPortNames();

            while (_isRunning && arduinoPort == null)
            {
                string[] currentPorts = SerialPort.GetPortNames();
                arduinoPort = FindNewPort(previousPorts, currentPorts);
                previousPorts = currentPorts;

                if (arduinoPort != null && VerifyArduino(arduinoPort))
                {
                    _serialPort = new SerialPort(arduinoPort, 9600);
                    _serialPort.Open();
                    _logger.LogInformation("Arduino connected on port " + arduinoPort);

                    // Start reading in background
                    Task.Run(() => ReadFromPort());
                    break;
                }

                await Task.Delay(100);
            }
        }

        private async Task ReadFromPort()
        {
            while (_isRunning && _serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    string data = _serialPort.ReadLine();
                    if (data.StartsWith("ID #"))
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveFingerprintId", data.Substring(4));
                    }
                    await _hubContext.Clients.All.SendAsync("ReceiveSerialData", data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading from serial port");
                    await Task.Delay(1000);
                }
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

        private bool VerifyArduino(string port)
        {
            try
            {
                using (SerialPort arduino = new SerialPort(port, 9600))
                {
                    arduino.Open();
                    Thread.Sleep(2000);

                    for (int i = 0; i < 10; i++)
                    {
                        if (arduino.BytesToRead > 0)
                        {
                            string data = arduino.ReadLine().Trim();
                            if (data.Contains("ArduinoFingerPrintSensorReady"))
                            {
                                return true;
                            }
                        }
                        Thread.Sleep(100);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        public void SendCommand(string command)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.WriteLine(command);
            }
        }

        public int? GetFingerprintId()
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                return null;
            }

            _serialPort.WriteLine("GET_ID");

            try
            {
                string response = _serialPort.ReadLine();
                if (int.TryParse(response, out int fingerprintId))
                {
                    return fingerprintId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading fingerprint ID: {ex.Message}");
            }

            return null;
        }

        public void Dispose()
        {
            _isRunning = false;
            _serialPort?.Close();
            _serialPort?.Dispose();
        }
    }
}
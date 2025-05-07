using System.IO.Ports;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using AttendanceSystem.Hubs;

namespace AttendanceSystem.Services
{
    public class ArduinoService
    {
        private SerialPort _serialPort;
        private readonly ConcurrentQueue<string> _serialLogs = new();
        private readonly IHubContext<ArduinoHub> _hubContext;
        private readonly object _lock = new();

        public bool IsConnected { get; private set; } = false;

        public ArduinoService(IHubContext<ArduinoHub> hubContext)
        {
            _hubContext = hubContext;
            Task.Run(() => InitializeSerialPort());
        }

        public ArduinoService()
        {
            _hubContext = null; // Mocked in tests
            _serialPort = null; // Mocked in tests
        }
        public bool IsArduinoConnected() => _serialPort != null && _serialPort.IsOpen;

        public string[] GetSerialLogs() => _serialLogs.ToArray();

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
                    IsConnected = true;
                    break;
                }

                arduinoPort = null;
                Thread.Sleep(200);
            }

            _serialPort = new SerialPort(arduinoPort, 9600)
            {
                DtrEnable = true,
                RtsEnable = true
            };
            Console.WriteLine("Arduino connected on port: " + arduinoPort);

            try
            {
                _serialPort.Open();
                Console.WriteLine("Serial Port Opened!!!");
                _serialPort.DataReceived += SerialDataReceived;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error opening serial port: " + e.Message);
            }
        }

        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (_lock)
            {
                try
                {
                    string response = _serialPort.ReadLine().Trim();
                    _serialLogs.Enqueue(response);
                    _hubContext.Clients.All.SendAsync("ReceiveSerialLog", response);
                    Console.WriteLine(response);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Serial Read Error: " + ex.Message);
                }
            }
        }

        private static string FindNewPort(string[] oldPorts, string[] newPorts) {
            return newPorts.Except(oldPorts).FirstOrDefault();
        }

        private static bool VerifyArduino(string port)
        {
            try
            {
                using (SerialPort testPort = new SerialPort(port, 9600))
                {
                    testPort.Open();
                    Thread.Sleep(3000);

                    for (int i = 0; i < 10; i++)
                    {
                        if (testPort.BytesToRead > 0)
                        {
                            string data = testPort.ReadLine().Trim();
                            if (data.Contains("ArduinoFingerPrintSensorReady"))
                            {
                                Console.WriteLine("Arduino is Ready!!!");
                                return true;
                            }
                        }
                        Thread.Sleep(100);
                    }
                }
            }
            catch (Exception) { }
            return false;
        }

        public bool SendCommand(string command)
        {
            try
            {
                if (IsArduinoConnected())
                {
                    _serialPort.WriteLine(command);
                    Console.WriteLine($"Sent '{command}' command to Arduino.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send command: " + ex.Message);
            }
            return false;
        }

    }
}

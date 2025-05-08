using System;
using System.IO.Ports;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using AttendanceSystem.Hubs;

namespace AttendanceSystem.Services
{
    public class ArduinoService : IDisposable
    {
        private readonly ILogger<ArduinoService> _logger;
        private readonly IHubContext<ArduinoHub> _hubContext;
        private readonly ConcurrentQueue<string> _serialLogs = new();
        private readonly object _lock = new();
        private SerialPort _serialPort;
        private string _lastKnownPort;
        private string[] _previousPorts;
        private CancellationTokenSource _cts;

        public bool IsConnected => IsArduinoConnected();

        public ArduinoService(IHubContext<ArduinoHub> hubContext, ILogger<ArduinoService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
            _previousPorts = SerialPort.GetPortNames();
            _cts = new CancellationTokenSource();

            _logger.LogInformation("Arduino service initialized");
            Task.Run(() => MonitorConnection(_cts.Token));
        }

        public bool IsArduinoConnected()
        {
            lock (_lock)
            {
                return _serialPort?.IsOpen == true;
            }
        }

        public string[] GetSerialLogs() => _serialLogs.ToArray();

        private async Task MonitorConnection(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (!IsArduinoConnected())
                    {
                        _logger.LogWarning("Arduino disconnected. Attempting to reconnect...");
                        await InitializeSerialPort(ct);
                    }
                    await Task.Delay(1000, ct);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Connection monitoring canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection monitoring error");
            }
        }

        private async Task InitializeSerialPort(CancellationToken ct)
        {
            try
            {
                if (!string.IsNullOrEmpty(_lastKnownPort))
                {
                    _logger.LogInformation("Attempting last known port: {Port}", _lastKnownPort);
                    if (VerifyArduino(_lastKnownPort))
                    {
                        ConnectToPort(_lastKnownPort);
                        return;
                    }
                    _lastKnownPort = null;
                }

                string arduinoPort = null;
                while (arduinoPort == null && !ct.IsCancellationRequested)
                {
                    var currentPorts = SerialPort.GetPortNames();
                    arduinoPort = FindNewPort(_previousPorts, currentPorts);

                    if (arduinoPort != null)
                    {
                        if (!currentPorts.Contains(arduinoPort)) continue;

                        await Task.Delay(500, ct);

                        if (VerifyArduino(arduinoPort))
                        {
                            _lastKnownPort = arduinoPort;
                            ConnectToPort(arduinoPort);
                            return;
                        }
                        arduinoPort = null;
                    }

                    await Task.Delay(1000, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Port initialization failed");
            }
        }

        private void ConnectToPort(string port)
        {
            lock (_lock)
            {
                try
                {
                    if (_serialPort != null)
                    {
                        _serialPort.DataReceived -= SerialDataReceived;
                        _serialPort.Dispose();
                    }

                    _serialPort = new SerialPort(port, 9600)
                    {
                        DtrEnable = true,
                        RtsEnable = true,
                        Handshake = Handshake.RequestToSend,
                        ReadTimeout = 2000,
                        WriteTimeout = 2000
                    };

                    _serialPort.Open();
                    _serialPort.DataReceived += SerialDataReceived;
                    _logger.LogInformation("Connected to {Port}", port);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to {Port}", port);
                    _serialPort?.Dispose();
                    _serialPort = null;
                }
            }
        }

        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (_lock)
            {
                try
                {
                    var response = _serialPort.ReadLine().Trim();
                    _serialLogs.Enqueue(response);
                    _hubContext.Clients.All.SendAsync("ReceiveSerialLog", response);
                    _logger.LogDebug("Received: {Response}", response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Data receive error");
                    _serialPort?.Dispose();
                    _serialPort = null;
                }
            }
        }

        private bool VerifyArduino(string port)
        {
            const int maxRetries = 3;
            for (var retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    if (!SerialPort.GetPortNames().Contains(port))
                    {
                        _logger.LogWarning("Port {Port} unavailable (attempt {Attempt})", port, retry + 1);
                        continue;
                    }

                    using var testPort = new SerialPort(port, 9600);
                    testPort.Open();

                    var timeout = DateTime.UtcNow.AddSeconds(5);
                    while (DateTime.UtcNow < timeout)
                    {
                        if (testPort.BytesToRead > 0)
                        {
                            var data = testPort.ReadLine().Trim();
                            if (data.Contains("ArduinoFingerPrintSensorReady"))
                            {
                                _logger.LogInformation("Arduino verified");
                                return true;
                            }
                        }
                        Thread.Sleep(200);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Verification attempt {Attempt} failed", retry + 1);
                }
                finally
                {
                    Thread.Sleep(1000);
                }
            }
            return false;
        }

        public bool SendCommand(string command)
        {
            lock (_lock)
            {
                try
                {
                    if (!IsArduinoConnected()) return false;

                    _serialPort.WriteLine(command);
                    _logger.LogInformation("Sent command: {Command}", command);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command send failed");
                    _serialPort?.Dispose();
                    _serialPort = null;
                    return false;
                }
            }
        }

        private static string FindNewPort(string[] oldPorts, string[] newPorts)
        {
            return newPorts.Except(oldPorts).FirstOrDefault();
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            lock (_lock)
            {
                if (_serialPort == null) return;

                _serialPort.DataReceived -= SerialDataReceived;
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }
            _logger.LogInformation("Arduino service disposed");
            GC.SuppressFinalize(this);
        }
    }
}
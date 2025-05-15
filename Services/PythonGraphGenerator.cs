using System.Diagnostics;

public static class PythonGraphGenerator
{
    public static void RunScript()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = "generate_attendance_graphs.py",  // or use full path
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (var process = Process.Start(psi))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Console.WriteLine("Python output: " + output);
            if (!string.IsNullOrEmpty(error))
                Console.WriteLine("Python error: " + error);
        }
    }
}

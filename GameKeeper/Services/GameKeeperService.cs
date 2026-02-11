using System.Diagnostics;
using System.IO;

namespace GameKeeper.Services;

public class GameKeeperService
{
    private (bool Success, string ErrorMessage) RunInjector(int processId, string method)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            var is64Bit = ProcessUtils.Is64BitProcess(process);
            var suffix = is64Bit ? "x64" : "x86";

            var injectorPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Injector_{suffix}.exe");
            var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"GameKeeperCore_{suffix}.dll");

            if (!File.Exists(injectorPath))
            {
                return (false, $"Injector not found at: {injectorPath}");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = injectorPath,
                Arguments = $"{processId} \"{dllPath}\" {method}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var injectorProcess = Process.Start(startInfo);
            if (injectorProcess == null)
            {
                return (false, "Failed to start injector process.");
            }

            // Capture output and error
            // Read to end before waiting to avoid deadlocks
            var output = injectorProcess.StandardOutput.ReadToEnd();
            var error = injectorProcess.StandardError.ReadToEnd();

            injectorProcess.WaitForExit();

            if (injectorProcess.ExitCode == 0)
            {
                return (true, string.Empty);
            }

            var message = string.IsNullOrWhiteSpace(error) ? output : $"{output}\nError: {error}";
            return (false, message.Trim());
        }
        catch (Exception ex)
        {
            return (false, $"Exception: {ex.Message}");
        }
    }

    public (bool Success, string ErrorMessage) Attach(int processId)
    {
        return RunInjector(processId, "attach");
    }

    public (bool Success, string ErrorMessage) Detach(int processId)
    {
        try
        {
            using var _ = Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            return (true, string.Empty);
        }

        return RunInjector(processId, "detach");
    }
}
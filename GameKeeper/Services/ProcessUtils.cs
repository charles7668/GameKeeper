using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GameKeeper.Services;

public static class ProcessUtils
{
    [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);

    public static bool IsSameArchitecture(Process process)
    {
        if (!Environment.Is64BitOperatingSystem)
        {
            return true; // 32-bit OS, everything is 32-bit
        }

        try
        {
            if (!IsWow64Process(process.Handle, out var isWow64))
            {
                return false; // Function failed
            }

            var isCurrentProcess64Bit = Environment.Is64BitProcess;
            var isTargetProcess64Bit = !isWow64;

            return isCurrentProcess64Bit == isTargetProcess64Bit;
        }
        catch
        {
            return false; // Access denied or process exited
        }
    }
}
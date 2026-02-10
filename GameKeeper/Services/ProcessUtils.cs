using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GameKeeper.Services;

public static class ProcessUtils
{
    [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);

    public static bool Is64BitProcess(Process process)
    {
        if (!Environment.Is64BitOperatingSystem) return false;

        try
        {
            if (!IsWow64Process(process.Handle, out var isWow64)) return false;
            return !isWow64;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsSameArchitecture(Process process)
    {
        if (!Environment.Is64BitOperatingSystem)
        {
            return true; // 32-bit OS, everything is 32-bit
        }

        return Environment.Is64BitProcess == Is64BitProcess(process);
    }
}
using System.Runtime.InteropServices;

namespace GameKeeper.Services;

internal static class DllLoaderApi
{
    private const string DllName = "DllLoader.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool GK_InjectCoreDll(int processId, string dllPath);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool GK_TryAttach(int processId, string dllPath);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool GK_TryDetach(int processId, string dllPath);
}
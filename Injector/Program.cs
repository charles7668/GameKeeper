using System.Runtime.InteropServices;

namespace Injector;

internal class Program
{
#if IS_X64
    private const string DllLoaderName = "DllLoader_x64.dll";
#else
    private const string DllLoaderName = "DllLoader_x86.dll";
#endif

    [DllImport(DllLoaderName, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool GK_InjectCoreDll(int processId, string dllPath);

    [DllImport(DllLoaderName, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool GK_TryAttach(int processId, string dllPath);

    [DllImport(DllLoaderName, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool GK_TryDetach(int processId, string dllPath);

    private static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: Injector <pid> <dllPath> <attach|detach>");
            return 1;
        }

        if (!int.TryParse(args[0], out var pid))
        {
            Console.WriteLine("Invalid PID");
            return 1;
        }

        var dllPath = args[1];
        var method = args[2].ToLower();

        if (!File.Exists(dllPath))
        {
            Console.WriteLine($"DLL not found: {dllPath}");
            return 1;
        }

        try
        {
            var result = false;
            if (method == "attach")
            {
                // Inject then Attach
                Console.WriteLine($"Injecting into {pid}...");
                if (GK_InjectCoreDll(pid, dllPath))
                {
                    Console.WriteLine("Injection successful. Attaching...");
                    result = GK_TryAttach(pid, dllPath);
                }
                else
                {
                    Console.WriteLine("Injection failed.");
                }
            }
            else if (method == "detach")
            {
                Console.WriteLine($"Detaching from {pid}...");
                result = GK_TryDetach(pid, dllPath);
            }
            else
            {
                Console.WriteLine("Unknown method. Use 'attach' or 'detach'.");
                return 1;
            }

            if (result)
            {
                Console.WriteLine("Success.");
                return 0;
            }

            Console.WriteLine("Operation failed.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
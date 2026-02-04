using System;
using System.Runtime.InteropServices;

namespace TestConsole
{
    internal class Program
    {
        [DllImport("DllLoader.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool GK_InjectCoreDll(uint dwProcessId, [MarshalAs(UnmanagedType.LPStr)] string dllPath);

        [DllImport("DllLoader.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool GK_TryAttach(uint dwProcessId, [MarshalAs(UnmanagedType.LPStr)] string dllPath);

        [DllImport("DllLoader.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool GK_TryDetach(uint dwProcessId, [MarshalAs(UnmanagedType.LPStr)] string dllPath);

        private const string CORE_DLL_PATH = @"d:\project\GameKeeper\TestConsole\bin\x86\Debug\net8.0\GameKeeperCore.dll";

        static void Main(string[] args)
        {
            Console.Write("Please enter PID: ");
            string input = Console.ReadLine();

            if (uint.TryParse(input, out uint pid))
            {
                Console.WriteLine($"Calling GK_InjectCoreDll for PID {pid}...");
                bool injectResult = GK_InjectCoreDll(pid, CORE_DLL_PATH);
                Console.WriteLine($"Inject result: {injectResult}");

                if (injectResult)
                {
                    Console.WriteLine("Press 'c' to toggle Attach/Detach...");
                    bool bAttached = false;
                    while (true)
                    {
                        var key = Console.ReadKey(true);
                        if (key.KeyChar == 'c' || key.KeyChar == 'C')
                        {
                            if (!bAttached)
                            {
                                Console.WriteLine("Calling GK_TryAttach...");
                                bool attachResult = GK_TryAttach(pid, CORE_DLL_PATH);
                                Console.WriteLine($"Attach result: {attachResult}");
                                if (attachResult) bAttached = true;
                            }
                            else
                            {
                                Console.WriteLine("Calling GK_TryDetach...");
                                bool detachResult = GK_TryDetach(pid, CORE_DLL_PATH);
                                Console.WriteLine($"Detach result: {detachResult}");
                                if (detachResult) bAttached = false;
                            }
                        }
                        else if (key.Key == ConsoleKey.Escape)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Invalid PID format.");
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}

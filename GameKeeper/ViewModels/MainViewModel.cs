using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GameKeeper.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        Refresh();
    }

    private const int GWLP_WNDPROC = -4;

    [ObservableProperty]
    private ObservableCollection<Process> _processes = [];

    [ObservableProperty]
    private Process? _selectedProcess;

    [ObservableProperty]
    private nint _wndProcAddress;

    [RelayCommand]
    private void Attach()
    {
        if (SelectedProcess != null)
        {
            WndProcAddress = GetWindowLongPtr(SelectedProcess.MainWindowHandle, GWLP_WNDPROC);
        }
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern nint GetWindowLong32(nint hWnd, int nIndex);

    private static nint GetWindowLongPtr(nint hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
        {
            return GetWindowLongPtr64(hWnd, nIndex);
        }

        return GetWindowLong32(hWnd, nIndex);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern nint GetWindowLongPtr64(nint hWnd, int nIndex);

    [RelayCommand]
    private void Refresh()
    {
        Processes.Clear();
        var processList = Process.GetProcesses()
            .Where(p => p.MainWindowHandle != nint.Zero)
            .OrderBy(p => p.ProcessName);

        foreach (var p in processList)
        {
            Processes.Add(p);
        }
    }
}
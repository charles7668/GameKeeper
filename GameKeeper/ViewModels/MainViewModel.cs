using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GameKeeper.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        Refresh();
    }

    [ObservableProperty]
    private ObservableCollection<Process> _processes = [];

    [ObservableProperty]
    private Process? _selectedProcess;

    [RelayCommand]
    private void Attach()
    {
        // TODO: Implement Attach logic
    }

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
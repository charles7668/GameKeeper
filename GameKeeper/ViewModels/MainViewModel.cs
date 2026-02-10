using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameKeeper.Services;

namespace GameKeeper.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        _gameKeeperService = new GameKeeperService();
        Refresh();
    }

    private readonly GameKeeperService _gameKeeperService;

    [ObservableProperty]
    private ObservableCollection<Process> _attachedProcesses = [];

    [ObservableProperty]
    private ObservableCollection<Process> _processes = [];

    [ObservableProperty]
    private Process? _selectedProcess;

    [RelayCommand]
    private void Attach()
    {
        if (SelectedProcess != null && !AttachedProcesses.Contains(SelectedProcess))
        {
            if (_gameKeeperService.Attach(SelectedProcess.Id))
            {
                AttachedProcesses.Add(SelectedProcess);
            }
        }
    }

    [RelayCommand]
    private void Detach(Process process)
    {
        if (_gameKeeperService.Detach(process.Id))
        {
            AttachedProcesses.Remove(process);
        }
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
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

    public string Title
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionString = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
            return $"GameKeeper {versionString}";
        }
    }

    [RelayCommand]
    private void Attach()
    {
        if (SelectedProcess != null && !AttachedProcesses.Contains(SelectedProcess))
        {
            var result = _gameKeeperService.Attach(SelectedProcess.Id);
            if (result.Success)
            {
                AttachedProcesses.Add(SelectedProcess);
            }
            else
            {
                MessageBox.Show(result.ErrorMessage, "Attach Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void Detach(Process process)
    {
        var result = _gameKeeperService.Detach(process.Id);
        if (result.Success)
        {
            AttachedProcesses.Remove(process);
        }
        else
        {
            MessageBox.Show(result.ErrorMessage, "Detach Failed", MessageBoxButton.OK, MessageBoxImage.Error);
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
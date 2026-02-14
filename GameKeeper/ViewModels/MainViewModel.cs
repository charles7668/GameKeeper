using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameKeeper.Services;

using System.Security.Principal;
namespace GameKeeper.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        _gameKeeperService = new GameKeeperService();
        Refresh();
        IsRunAsAdminVisible = !IsRunningAsAdministrator();
    }

    private bool IsRunningAsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private readonly GameKeeperService _gameKeeperService;

    [ObservableProperty]
    private ObservableCollection<Process> _attachedProcesses = [];

    [ObservableProperty]
    private ObservableCollection<Process> _processes = [];

    [ObservableProperty]
    private Process? _selectedProcess;

    [ObservableProperty]
    private bool _isRunAsAdminVisible;

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

    [RelayCommand]
    private void RunAsAdmin()
    {
        var exeName = Process.GetCurrentProcess().MainModule?.FileName;
        if (exeName != null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
            startInfo.UseShellExecute = true;
            startInfo.Verb = "runas";
            try
            {
                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
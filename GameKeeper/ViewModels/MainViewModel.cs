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
    private ObservableCollection<ProcessItemViewModel> _attachedProcesses = [];

    [ObservableProperty]
    private ObservableCollection<ProcessItemViewModel> _processes = [];

    [ObservableProperty]
    private ProcessItemViewModel? _selectedProcess;

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
        if (SelectedProcess != null && AttachedProcesses.All(p => p.Id != SelectedProcess.Id))
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
    private void Capture(ProcessItemViewModel process)
    {
        if (process == null)
        {
            return;
        }

        var captureWindow = new CaptureWindow(process.Process);
        captureWindow.Show();
    }

    [RelayCommand]
    private void Detach(ProcessItemViewModel process)
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

    public bool DetachAll()
    {
        var failures = new List<string>();

        foreach (var process in AttachedProcesses.ToList())
        {
            var result = _gameKeeperService.Detach(process.Id);
            if (result.Success)
            {
                AttachedProcesses.Remove(process);
            }
            else
            {
                failures.Add($"{process.ProcessName} ({process.Id}): {result.ErrorMessage}");
            }
        }

        if (failures.Count == 0)
        {
            return true;
        }

        MessageBox.Show(
            string.Join(Environment.NewLine, failures),
            "Detach Failed",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        return false;
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
            Processes.Add(new ProcessItemViewModel(p));
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

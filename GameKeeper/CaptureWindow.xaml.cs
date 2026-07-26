using System.Diagnostics;
using System.Windows;
using GameKeeper.Services;

namespace GameKeeper;

public partial class CaptureWindow
{
    public CaptureWindow(Process process)
    {
        _process = process;
        InitializeComponent();
        Title = $"{process.ProcessName} ({process.Id}) - Capture";
    }

    private readonly Process _process;
    private WindowCaptureService? _captureService;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        StartCapture();
    }

    protected override void OnClosed(EventArgs e)
    {
        _captureService?.Dispose();
        base.OnClosed(e);
    }

    private void StartCapture()
    {
        try
        {
            _process.Refresh();
            if (_process.HasExited || _process.MainWindowHandle == IntPtr.Zero)
            {
                ShowStatus("The selected process does not have a capturable main window.");
                return;
            }

            _captureService = new WindowCaptureService(_process.MainWindowHandle);
            _captureService.FrameReady += (_, frame) =>
            {
                CaptureImage.Source = frame;
                StatusOverlay.Visibility = Visibility.Collapsed;
            };
            _captureService.Failed += (_, message) => ShowStatus(message);
            _captureService.Closed += (_, _) => Close();
            _captureService.Start();
        }
        catch (Exception ex)
        {
            ShowStatus(ex.Message);
        }
    }

    private void ShowStatus(string message)
    {
        StatusText.Text = message;
        StatusOverlay.Visibility = Visibility.Visible;
    }
}
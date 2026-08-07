using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GameKeeper.Services;

namespace GameKeeper;

public partial class CaptureWindow
{
    public CaptureWindow(Process process)
    {
        _process = process;
        InitializeComponent();
        Title = $"{process.ProcessName} ({process.Id}) - Capture";
        CaptureImage.MouseLeftButtonDown += OnCaptureImageMouseLeftButtonDown;
        CaptureImage.MouseLeftButtonUp += OnCaptureImageMouseLeftButtonUp;
        CaptureImage.MouseMove += OnCaptureImageMouseMove;
    }

    private readonly Process _process;
    private WindowCaptureService? _captureService;

    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmMouseMove = 0x0200;
    private const int MkLButton = 0x0001;
    private static readonly int SetCursorOverrideMessage = RegisterWindowMessage("GameKeeper.SetCursorOverride");

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        StartCapture();
    }

    protected override void OnClosed(EventArgs e)
    {
        SetTargetCursorOverride(false);
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
            SetTargetCursorOverride(true);
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

    private void OnCaptureImageMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CaptureImage.CaptureMouse();
        ForwardLeftButtonMessage(e, WmLButtonDown, MkLButton);
        e.Handled = true;
    }

    private void OnCaptureImageMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ForwardLeftButtonMessage(e, WmLButtonUp, 0);
        CaptureImage.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void OnCaptureImageMouseMove(object sender, MouseEventArgs e)
    {
        var buttonState = e.LeftButton == MouseButtonState.Pressed ? MkLButton : 0;
        ForwardLeftButtonMessage(e, WmMouseMove, buttonState);
        e.Handled = true;
    }

    private void ForwardLeftButtonMessage(MouseEventArgs e, int message, int buttonState)
    {
        _process.Refresh();
        if (_process.HasExited || _process.MainWindowHandle == IntPtr.Zero)
        {
            return;
        }

        if (!TryGetCapturePixelPosition(e.GetPosition(CaptureImage), out var x, out var y))
        {
            return;
        }

        PostMessage(_process.MainWindowHandle, message, new IntPtr(buttonState), MakeLParam(x, y));
    }

    private void SetTargetCursorOverride(bool enabled)
    {
        _process.Refresh();
        if (_process.HasExited || _process.MainWindowHandle == IntPtr.Zero || SetCursorOverrideMessage == 0)
        {
            return;
        }

        PostMessage(_process.MainWindowHandle, SetCursorOverrideMessage, enabled ? new IntPtr(1) : IntPtr.Zero, IntPtr.Zero);
    }

    private bool TryGetCapturePixelPosition(Point imagePosition, out int x, out int y)
    {
        x = 0;
        y = 0;

        if (CaptureImage.Source is not BitmapSource source ||
            CaptureImage.ActualWidth <= 0 ||
            CaptureImage.ActualHeight <= 0 ||
            source.PixelWidth <= 0 ||
            source.PixelHeight <= 0)
        {
            return false;
        }

        var scale = Math.Min(CaptureImage.ActualWidth / source.PixelWidth, CaptureImage.ActualHeight / source.PixelHeight);
        var displayedWidth = source.PixelWidth * scale;
        var displayedHeight = source.PixelHeight * scale;
        var offsetX = (CaptureImage.ActualWidth - displayedWidth) / 2;
        var offsetY = (CaptureImage.ActualHeight - displayedHeight) / 2;
        var sourceX = (imagePosition.X - offsetX) / scale;
        var sourceY = (imagePosition.Y - offsetY) / scale;

        if (sourceX < 0 || sourceY < 0 || sourceX >= source.PixelWidth || sourceY >= source.PixelHeight)
        {
            return false;
        }

        x = Math.Clamp((int)Math.Round(sourceX), 0, source.PixelWidth - 1);
        y = Math.Clamp((int)Math.Round(sourceY), 0, source.PixelHeight - 1);
        return true;
    }

    private static IntPtr MakeLParam(int lowWord, int highWord)
    {
        return new IntPtr((highWord << 16) | (lowWord & 0xffff));
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegisterWindowMessage(string lpString);
}

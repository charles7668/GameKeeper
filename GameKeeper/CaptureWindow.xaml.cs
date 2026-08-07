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
    private IntPtr _lastMouseMoveTarget;
    private IntPtr _mouseCaptureTarget;

    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmMouseMove = 0x0200;
    private const int WmMouseLeave = 0x02A3;
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

        if (!TryGetMainClientPosition(e.GetPosition(CaptureImage), _process.MainWindowHandle, out var mainClientX, out var mainClientY))
        {
            ClearLastMouseMoveTarget();
            return;
        }

        var targetHwnd = _mouseCaptureTarget != IntPtr.Zero && message != WmMouseMove
            ? _mouseCaptureTarget
            : GetDeepestChildWindowFromPoint(_process.MainWindowHandle, mainClientX, mainClientY);
        if (targetHwnd == IntPtr.Zero)
        {
            targetHwnd = _process.MainWindowHandle;
        }

        if (message == WmLButtonDown)
        {
            _mouseCaptureTarget = targetHwnd;
        }

        if (!TryConvertMainClientToTargetClient(
                _process.MainWindowHandle,
                targetHwnd,
                mainClientX,
                mainClientY,
                out var x,
                out var y))
        {
            return;
        }

        if (message == WmMouseMove && _lastMouseMoveTarget != IntPtr.Zero && _lastMouseMoveTarget != targetHwnd)
        {
            PostMessage(targetHwnd, message, new IntPtr(buttonState), MakeLParam(x, y));
            PostMessage(_lastMouseMoveTarget, WmMouseLeave, IntPtr.Zero, IntPtr.Zero);
            _lastMouseMoveTarget = targetHwnd;
            return;
        }

        PostMessage(targetHwnd, message, new IntPtr(buttonState), MakeLParam(x, y));

        if (message == WmMouseMove)
        {
            _lastMouseMoveTarget = targetHwnd;
        }

        if (message == WmLButtonUp)
        {
            _mouseCaptureTarget = IntPtr.Zero;
        }
    }

    private void ClearLastMouseMoveTarget()
    {
        if (_lastMouseMoveTarget == IntPtr.Zero)
        {
            return;
        }

        PostMessage(_lastMouseMoveTarget, WmMouseLeave, IntPtr.Zero, IntPtr.Zero);
        _lastMouseMoveTarget = IntPtr.Zero;
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

    private bool TryGetMainClientPosition(Point imagePosition, IntPtr hwnd, out int x, out int y)
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

        var captureX = Math.Clamp(sourceX, 0, source.PixelWidth - 1);
        var captureY = Math.Clamp(sourceY, 0, source.PixelHeight - 1);

        using var _ = new DpiAwarenessScope();

        if (!TryGetCaptureClientArea(hwnd, source.PixelWidth, source.PixelHeight, out var captureClientArea, out var clientRect))
        {
            return false;
        }

        var captureClientWidth = captureClientArea.Right - captureClientArea.Left;
        var captureClientHeight = captureClientArea.Bottom - captureClientArea.Top;
        var clientWidth = clientRect.Right - clientRect.Left;
        var clientHeight = clientRect.Bottom - clientRect.Top;
        if (captureClientWidth <= 0 || captureClientHeight <= 0 || clientWidth <= 0 || clientHeight <= 0)
        {
            return false;
        }

        if (captureX < captureClientArea.Left ||
            captureY < captureClientArea.Top ||
            captureX >= captureClientArea.Right ||
            captureY >= captureClientArea.Bottom)
        {
            return false;
        }

        x = Math.Clamp(
            (int)Math.Round((captureX - captureClientArea.Left) * clientWidth / captureClientWidth),
            0,
            clientWidth - 1);
        y = Math.Clamp(
            (int)Math.Round((captureY - captureClientArea.Top) * clientHeight / captureClientHeight),
            0,
            clientHeight - 1);
        return true;
    }

    private static bool TryConvertMainClientToTargetClient(
        IntPtr mainHwnd,
        IntPtr targetHwnd,
        int mainClientX,
        int mainClientY,
        out int targetClientX,
        out int targetClientY)
    {
        targetClientX = mainClientX;
        targetClientY = mainClientY;

        if (targetHwnd == mainHwnd)
        {
            return true;
        }

        var point = new NativePoint { X = mainClientX, Y = mainClientY };
        if (!ClientToScreen(mainHwnd, ref point) || !ScreenToClient(targetHwnd, ref point))
        {
            return false;
        }

        targetClientX = point.X;
        targetClientY = point.Y;
        return true;
    }

    private static IntPtr GetDeepestChildWindowFromPoint(IntPtr hwnd, int clientX, int clientY)
    {
        var current = hwnd;
        var point = new NativePoint { X = clientX, Y = clientY };

        while (true)
        {
            var child = ChildWindowFromPointEx(current, point, ChildWindowFromPointFlags);
            if (child == IntPtr.Zero || child == current)
            {
                return current;
            }

            var screenPoint = point;
            if (!ClientToScreen(current, ref screenPoint) || !ScreenToClient(child, ref screenPoint))
            {
                return child;
            }

            current = child;
            point = screenPoint;
        }
    }

    private static bool TryGetCaptureClientArea(
        IntPtr hwnd,
        int captureWidth,
        int captureHeight,
        out DoubleRect captureClientArea,
        out NativeRect clientRect)
    {
        captureClientArea = default;
        clientRect = default;

        if (!TryGetCaptureBounds(hwnd, out var captureBounds) || !GetClientRect(hwnd, out clientRect))
        {
            return false;
        }

        var clientTopLeft = new NativePoint { X = clientRect.Left, Y = clientRect.Top };
        var clientBottomRight = new NativePoint { X = clientRect.Right, Y = clientRect.Bottom };
        if (!ClientToScreen(hwnd, ref clientTopLeft) || !ClientToScreen(hwnd, ref clientBottomRight))
        {
            return false;
        }

        var captureBoundsWidth = captureBounds.Right - captureBounds.Left;
        var captureBoundsHeight = captureBounds.Bottom - captureBounds.Top;
        if (captureBoundsWidth <= 0 || captureBoundsHeight <= 0)
        {
            return false;
        }

        captureClientArea = new DoubleRect
        {
            Left = (clientTopLeft.X - captureBounds.Left) * captureWidth / (double)captureBoundsWidth,
            Top = (clientTopLeft.Y - captureBounds.Top) * captureHeight / (double)captureBoundsHeight,
            Right = (clientBottomRight.X - captureBounds.Left) * captureWidth / (double)captureBoundsWidth,
            Bottom = (clientBottomRight.Y - captureBounds.Top) * captureHeight / (double)captureBoundsHeight
        };

        return true;
    }

    private static bool TryGetCaptureBounds(IntPtr hwnd, out NativeRect bounds)
    {
        if (DwmGetWindowAttribute(hwnd, DwmwaExtendedFrameBounds, out bounds, Marshal.SizeOf<NativeRect>()) == 0)
        {
            return true;
        }

        return GetWindowRect(hwnd, out bounds);
    }

    private static IntPtr MakeLParam(int lowWord, int highWord)
    {
        return new IntPtr(unchecked((int)((ushort)lowWord | ((uint)(ushort)highWord << 16))));
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hWnd, out NativeRect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref NativePoint lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ScreenToClient(IntPtr hWnd, ref NativePoint lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr ChildWindowFromPointEx(IntPtr hWndParent, NativePoint pt, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        out NativeRect pvAttribute,
        int cbAttribute);

    private const int DwmwaExtendedFrameBounds = 9;
    private const uint CwpSkipInvisible = 0x0001;
    private const uint CwpSkipDisabled = 0x0002;
    private const uint CwpSkipTransparent = 0x0004;
    private const uint ChildWindowFromPointFlags = CwpSkipInvisible | CwpSkipDisabled | CwpSkipTransparent;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private struct DoubleRect
    {
        public double Left;
        public double Top;
        public double Right;
        public double Bottom;
    }

    private sealed class DpiAwarenessScope : IDisposable
    {
        private static readonly IntPtr PerMonitorAwareV2 = new(-4);

        private readonly IntPtr _previousContext;

        public DpiAwarenessScope()
        {
            _previousContext = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
        }

        public void Dispose()
        {
            if (_previousContext != IntPtr.Zero)
            {
                SetThreadDpiAwarenessContext(_previousContext);
            }
        }
    }
}

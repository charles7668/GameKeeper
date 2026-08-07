using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;

namespace GameKeeper.Services;

internal sealed class WindowCaptureService : IDisposable
{
    public WindowCaptureService(IntPtr hwnd)
    {
        if (!GraphicsCaptureSession.IsSupported())
        {
            throw new NotSupportedException("Windows Graphics Capture is not supported on this system.");
        }

        _item = GraphicsCaptureItemFactory.CreateForWindow(hwnd);
        if (_item.Size.Width <= 0 || _item.Size.Height <= 0)
        {
            throw new InvalidOperationException("The selected window has no capturable content.");
        }

        _device = Direct3D11DeviceFactory.CreateDevice();
        _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            _device,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            2,
            _item.Size);
        _session = _framePool.CreateCaptureSession(_item);
        _session.IsCursorCaptureEnabled = false;

        _item.Closed += (_, _) => Application.Current.Dispatcher.Invoke(() => Closed?.Invoke(this, EventArgs.Empty));
        _framePool.FrameArrived += OnFrameArrived;
    }

    private readonly IDirect3DDevice _device;
    private readonly object _frameLock = new();
    private readonly Direct3D11CaptureFramePool _framePool;
    private readonly GraphicsCaptureItem _item;
    private readonly GraphicsCaptureSession _session;
    private WriteableBitmap? _bitmap;
    private bool _hasPendingFrame;
    private bool _isDisposed;
    private bool _isProcessingFrame;

    public void Dispose()
    {
        lock (_frameLock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
        }

        _session.Dispose();
        _framePool.Dispose();
    }

    public event EventHandler<BitmapSource>? FrameReady;

    public event EventHandler? Closed;

    public event EventHandler<string>? Failed;

    public void Start()
    {
        _session.StartCapture();
    }

    private async void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        lock (_frameLock)
        {
            if (_isDisposed)
            {
                return;
            }

            if (_isProcessingFrame)
            {
                _hasPendingFrame = true;
                return;
            }

            _isProcessingFrame = true;
        }

        while (true)
        {
            lock (_frameLock)
            {
                _hasPendingFrame = false;
            }

            try
            {
                await ProcessLatestFrameAsync(sender);
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!_isDisposed)
                    {
                        Failed?.Invoke(this, ex.Message);
                    }
                });
            }

            lock (_frameLock)
            {
                if (_isDisposed || !_hasPendingFrame)
                {
                    _isProcessingFrame = false;
                    return;
                }
            }
        }
    }

    private async Task ProcessLatestFrameAsync(Direct3D11CaptureFramePool sender)
    {
        Direct3D11CaptureFrame? latestFrame = null;
        try
        {
            while (true)
            {
                var frame = sender.TryGetNextFrame();
                if (frame == null)
                {
                    break;
                }

                latestFrame?.Dispose();
                latestFrame = frame;
            }

            if (latestFrame == null)
            {
                return;
            }

            if (latestFrame.ContentSize.Width != _item.Size.Width || latestFrame.ContentSize.Height != _item.Size.Height)
            {
                sender.Recreate(_device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, latestFrame.ContentSize);
            }

            using var bitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(latestFrame.Surface);
            using var displayBitmap =
                SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var width = displayBitmap.PixelWidth;
            var height = displayBitmap.PixelHeight;
            var stride = width * 4;
            var bytes = new byte[stride * height];
            displayBitmap.CopyToBuffer(bytes.AsBuffer());

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_isDisposed)
                {
                    return;
                }

                if (_bitmap == null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
                {
                    _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
                }

                _bitmap.WritePixels(new Int32Rect(0, 0, width, height), bytes, stride, 0);
                FrameReady?.Invoke(this, _bitmap);
            });
        }
        finally
        {
            latestFrame?.Dispose();
        }
    }
}

using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using WinRT;

namespace GameKeeper.Services;

internal static class GraphicsCaptureItemFactory
{
    private static readonly Guid GraphicsCaptureItemGuid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

    public static GraphicsCaptureItem CreateForWindow(IntPtr hwnd)
    {
        var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
        var itemGuid = GraphicsCaptureItemGuid;
        Marshal.ThrowExceptionForHR(interop.CreateForWindow(hwnd, ref itemGuid, out var itemPointer));
        try
        {
            return MarshalInterface<GraphicsCaptureItem>.FromAbi(itemPointer);
        }
        finally
        {
            Marshal.Release(itemPointer);
        }
    }

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        int CreateForWindow(IntPtr window, [In] ref Guid iid, out IntPtr result);
        int CreateForMonitor(IntPtr monitor, [In] ref Guid iid, out IntPtr result);
    }
}

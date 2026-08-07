using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameKeeper.ViewModels;

public sealed class ProcessItemViewModel(Process process)
{
    public Process Process { get; } = process;

    public int Id => Process.Id;

    public ImageSource? Icon { get; } = LoadIcon(process);

    public string ProcessName => Process.ProcessName;

    private static ImageSource? LoadIcon(Process process)
    {
        try
        {
            var fileName = process.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            var fileInfo = new ShFileInfo();
            var result = SHGetFileInfo(
                fileName,
                0,
                ref fileInfo,
                (uint)Marshal.SizeOf<ShFileInfo>(),
                ShgfiIcon | ShgfiSmallIcon);
            if (result == IntPtr.Zero || fileInfo.Icon == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var image = Imaging.CreateBitmapSourceFromHIcon(
                    fileInfo.Icon,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(16, 16));
                image.Freeze();
                return image;
            }
            finally
            {
                DestroyIcon(fileInfo.Icon);
            }
        }
        catch
        {
            return null;
        }
    }

    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiSmallIcon = 0x000000001;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref ShFileInfo psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileInfo
    {
        public IntPtr Icon;
        public int IconIndex;
        public uint Attributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string TypeName;
    }
}

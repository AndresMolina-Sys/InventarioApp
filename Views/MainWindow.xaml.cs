using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace InventarioAppDesktop.Views;

public partial class MainWindow : Window
{
    private readonly ViewModels.MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new ViewModels.MainViewModel();
        DataContext = _vm;
        Loaded += MainWindow_Loaded;
        SourceInitialized += OnSourceInitialized;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAllAsync();
        ContentScrollViewer.Focus();
        ContentScrollViewer.PreviewMouseWheel += (s, args) =>
        {
            var sv = ContentScrollViewer;
            sv.ScrollToVerticalOffset(sv.VerticalOffset - args.Delta);
            args.Handled = true;
        };
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_GETMINMAXINFO = 0x0024;
        if (msg == WM_GETMINMAXINFO)
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    var rcWork = monitorInfo.rcWork;
                    var rcMonitor = monitorInfo.rcMonitor;
                    mmi.ptMaxPosition.X = Math.Abs(rcWork.Left - rcMonitor.Left);
                    mmi.ptMaxPosition.Y = Math.Abs(rcWork.Top - rcMonitor.Top);
                    mmi.ptMaxSize.X = Math.Abs(rcWork.Right - rcWork.Left);
                    mmi.ptMaxSize.Y = Math.Abs(rcWork.Bottom - rcWork.Top);
                }
            }
            Marshal.StructureToPtr(mmi, lParam, false);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private const int MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

    [DllImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
}

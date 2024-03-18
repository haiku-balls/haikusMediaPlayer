using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.Core;
using Windows.Storage.FileProperties;
using WinRT.Interop;
using Windows.Storage.Streams;
using Windows.Media.Playback;
using Windows.Storage;
using System.Threading.Tasks;
using System.Diagnostics;
using DiscordRPC;
using DiscordRPC.Logging;
using System.IO;
using Windows.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace haikusMediaPlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        private OverlappedPresenter _presenter;

        private AppWindow m_AppWindow;

        MediaPlaybackList _mediaPlaybackList;

        private void CenterToScreen(IntPtr hWnd)
        {
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            if (appWindow is not null)
            {
                Microsoft.UI.Windowing.DisplayArea displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
                if (displayArea is not null)
                {
                    var CenteredPosition = appWindow.Position;
                    CenteredPosition.X = ((displayArea.WorkArea.Width - appWindow.Size.Width) / 2);
                    CenteredPosition.Y = ((displayArea.WorkArea.Height - appWindow.Size.Height) / 2);
                    appWindow.Move(CenteredPosition);
                }
            }
        }

        // SYSTEM FUNCTION, GRABS THE CURRENT WINDOW. SEE MAINWINDOW.
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        public MainWindow()
        {
            this.InitializeComponent();
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            CenterToScreen(hWnd);

            m_AppWindow = GetAppWindowForCurrentWindow();

            // Title bar
            var titleBar = m_AppWindow.TitleBar;
            Title = "Haiku's Media Player";

            // Allows XAML to "clip" into.
            titleBar.ExtendsContentIntoTitleBar = true;

            // App Icon (Alt-Tab)
            m_AppWindow.SetIcon("Assets/windowIcon.ico");

            // This disables the ability to maximize the window.
            // Inherited from winUISysInfo.
            _presenter = m_AppWindow.Presenter as OverlappedPresenter;
            _presenter.IsResizable = true;
            _presenter.IsMaximizable = false;
            _presenter.IsMinimizable = false;

            // Headless navigate
            contentFrame.Navigate(typeof(homePage));
        }
    }
}

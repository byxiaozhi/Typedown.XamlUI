using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Typedown.XamlUI
{
    public partial class XamlWindow
    {
        private bool _firstShow = true;

        public static XamlWindow GetWindow(UIElement element)
        {
            if (element == null)
                return null;
            return AllWindows.Where(x => x._rootLayout == element.XamlRoot.Content).FirstOrDefault();
        }

        public override void Show(ShowWindowCommand? command = null)
        {
            command ??= GetShowWindowCommand();
            InitializeWindow();
            if (_firstShow)
            {
                _firstShow = false;
                if (WindowStartupLocation == WindowStartupLocation.CenterScreen)
                    CenterWindow();
                if (WindowStartupLocation == WindowStartupLocation.CenterOwner)
                    CenterWindow(Owner);
            }
            PInvoke.ShowWindow(new(Handle), (SHOW_WINDOW_CMD)command);
        }

        public Task ShowDialogAsync()
        {
            var owner = new HWND(Owner);
            if (owner == HWND.Null)
                throw new InvalidOperationException("Owner property is not set");
            var taskSource = new TaskCompletionSource<bool>();
            if (PInvoke.EnableWindow(owner, false))
                throw new InvalidOperationException("Another dialog is already open");
            var command = GetShowWindowCommand();
            try
            {
                InitializeWindow();
            }
            catch (Exception ex)
            {
                PInvoke.EnableWindow(owner, true);
                throw ex;
            }
            Closed += OnDialogWindowClosed;
            void OnDialogWindowClosed(object sender, ClosedEventArgs e)
            {
                Closed -= OnDialogWindowClosed;
                PInvoke.EnableWindow(owner, true);
                PInvoke.SetForegroundWindow(owner);
                taskSource.SetResult(true);
            }
            Show(command);
            return taskSource.Task;
        }

        public virtual unsafe void OpenSystemMenu(Point? screenPoint = null)
        {
            if (Handle == 0)
                throw new InvalidOperationException();
            if (!screenPoint.HasValue)
            {
                var point = new System.Drawing.Point();
                if (!PInvoke.GetCursorPos(&point))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                screenPoint = new(point.X, point.Y);
            }
            PInvoke.GetSystemMenu_SafeHandle(new(Handle), true);
            using var hMenu = PInvoke.GetSystemMenu_SafeHandle(new(Handle), false);
            var toEnable = (bool b) => b ? MENU_ITEM_FLAGS.MF_ENABLED : MENU_ITEM_FLAGS.MF_DISABLED;
            PInvoke.EnableMenuItem(hMenu, PInvoke.SC_MAXIMIZE, toEnable(WindowState == WindowState.Normal && ResizeMode == WindowResizeMode.CanResize));
            PInvoke.EnableMenuItem(hMenu, PInvoke.SC_RESTORE, toEnable(WindowState == WindowState.Maximized && ResizeMode == WindowResizeMode.CanResize));
            PInvoke.EnableMenuItem(hMenu, PInvoke.SC_MOVE, toEnable(WindowState == WindowState.Normal));
            PInvoke.EnableMenuItem(hMenu, PInvoke.SC_SIZE, toEnable(WindowState == WindowState.Normal && ResizeMode == WindowResizeMode.CanResize));
            PInvoke.EnableMenuItem(hMenu, PInvoke.SC_MINIMIZE, toEnable(ResizeMode == WindowResizeMode.CanResize || ResizeMode == WindowResizeMode.CanMinimize));
            PInvoke.SetMenuDefaultItem(hMenu, WindowState == WindowState.Maximized ? 0u : 4u, 1);
            var retvalue = PInvoke.TrackPopupMenu(hMenu, TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD, (int)screenPoint.Value.X, (int)screenPoint.Value.Y, 0, new(Handle), (RECT*)0);
            if (retvalue) PInvoke.PostMessage(new(Handle), PInvoke.WM_SYSCOMMAND, (nuint)retvalue.Value, 0);
        }

        public override void SetTopmost(bool value)
        {
            Topmost = value;
            if (Handle != 0)
                base.SetTopmost(value);
        }
    }
}

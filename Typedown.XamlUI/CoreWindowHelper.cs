using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Typedown.XamlUI
{
    internal class CoreWindowHelper
    {
        public static HWND CoreWindowHandle { get; private set; }

        public static void DetachCoreWindow()
        {
            if (CoreWindowHandle != IntPtr.Zero)
            {
                PInvoke.SetParent(CoreWindowHandle, HWND.Null);
                PInvoke.ShowWindow(CoreWindowHandle, SHOW_WINDOW_CMD.SW_HIDE);
            }
        }

        public static void SetCoreWindow(HWND handle)
        {
            if (IsCoreWindow(handle))
            {
                CoreWindowHandle = handle;
                var exStyle = PInvoke.GetWindowLong(CoreWindowHandle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
                PInvoke.SetWindowLong(CoreWindowHandle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle | (int)WindowExStyle.WS_EX_TOOLWINDOW);
            }
        }

        public static unsafe bool IsCoreWindow(HWND handle)
        {
            var classname = new char[255];
            fixed (char* lpClassName = classname)
            {
                PInvoke.GetClassName(handle, lpClassName, classname.Length);
                var str = Marshal.PtrToStringAuto((nint)lpClassName);
                return str == typeof(Windows.UI.Core.CoreWindow).FullName;
            }
        }
    }
}

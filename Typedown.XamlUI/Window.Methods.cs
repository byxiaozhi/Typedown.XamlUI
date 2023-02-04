using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Typedown.XamlUI
{
    public partial class Window
    {

        public unsafe Rect GetRect()
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            var rect = new RECT();
            if (!PInvoke.GetWindowRect(_hWnd, &rect))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return new(rect.X / ScalingFactor, rect.Y / ScalingFactor, rect.Width / ScalingFactor, rect.Height / ScalingFactor);
        }

        public Point GetLocation()
        {
            var rect = GetRect();
            return new(rect.X, rect.Y);
        }

        public Size GetSize()
        {
            var rect = GetRect();
            return new(rect.Width, rect.Height);
        }

        public unsafe Size GetClientSize()
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            var rect = new RECT();
            if (!PInvoke.GetClientRect(_hWnd, &rect))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return new(rect.Width / ScalingFactor, rect.Height / ScalingFactor);
        }

        public virtual void SetRect(Rect rect)
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            var rawX = (int)Math.Round(rect.X * ScalingFactor);
            var rawY = (int)Math.Round(rect.Y * ScalingFactor);
            var rawW = (int)Math.Round(rect.Width * ScalingFactor);
            var rawH = (int)Math.Round(rect.Height * ScalingFactor);
            var flags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
            if (!PInvoke.SetWindowPos(_hWnd, HWND.Null, rawX, rawY, rawW, rawH, flags))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public virtual void SetLocation(Point point)
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            var rawX = (int)Math.Round(point.X * ScalingFactor);
            var rawY = (int)Math.Round(point.Y * ScalingFactor);
            var flags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE;
            if (!PInvoke.SetWindowPos(_hWnd, HWND.Null, rawX, rawY, 0, 0, flags))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public virtual void SetSize(Size size)
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            var rawW = (int)Math.Round(size.Width * ScalingFactor);
            var rawH = (int)Math.Round(size.Height * ScalingFactor);
            var flags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE;
            if (!PInvoke.SetWindowPos(_hWnd, HWND.Null, 0, 0, rawW, rawH, flags))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public virtual void Show(ShowWindowCommand? command = null)
        {
            InitializeWindow();
            command ??= ShowWindowCommand.SW_NORMAL;
            PInvoke.ShowWindow(_hWnd, (SHOW_WINDOW_CMD)command);
        }

        public virtual unsafe bool CenterWindow(nint owner = 0)
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            RECT centerRect;
            if (owner == 0)
            {
                var monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFO>();
                if (!PInvoke.GetMonitorInfo(PInvoke.MonitorFromWindow(_hWnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST), &monitorInfo))
                    return false;
                centerRect = monitorInfo.rcWork;
            }
            else
            {
                if (!PInvoke.GetWindowRect(new(owner), &centerRect))
                    return false;
            }
            var windowRect = new RECT();
            if (!PInvoke.GetWindowRect(_hWnd, &windowRect))
                return false;
            int left = (centerRect.left + centerRect.right - windowRect.Width) / 2;
            int top = (centerRect.top + centerRect.bottom - windowRect.Height) / 2;
            var flags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
            return PInvoke.SetWindowPos(_hWnd, HWND.Null, left, top, windowRect.Width, windowRect.Height, flags);
        }

        public WindowStyle GetStyle()
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            var res = PInvoke.GetWindowLong(_hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            if (res == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return (WindowStyle)res;
        }

        public WindowExStyle GetExStyle()
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            var res = PInvoke.GetWindowLong(_hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            if (res == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return (WindowExStyle)res;
        }

        public virtual WindowStyle SetStyle(WindowStyle style)
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            var res = PInvoke.SetWindowLong(_hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)style);
            if (res == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return (WindowStyle)res;
        }

        public virtual WindowExStyle SetExStyle(WindowExStyle style)
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            var res = PInvoke.SetWindowLong(_hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)style);
            if (res == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return (WindowExStyle)res;
        }

        public virtual WindowStyle AddStyle(WindowStyle style)
        {
            return SetStyle(GetStyle() | style);
        }

        public virtual WindowExStyle AddExStyle(WindowExStyle style)
        {
            return SetExStyle(GetExStyle() | style);
        }

        public virtual WindowStyle RemoveStyle(WindowStyle style)
        {
            return SetStyle(GetStyle() & ~style);
        }

        public virtual WindowExStyle RemoveExStyle(WindowExStyle style)
        {
            return SetExStyle(GetExStyle() & ~style);
        }

        public bool HasStyle(WindowStyle style)
        {
            return GetStyle().HasFlag(style);
        }

        public bool HasExStyle(WindowExStyle style)
        {
            return GetExStyle().HasFlag(style);
        }

        public bool GetIsMaximized()
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            return PInvoke.IsZoomed(_hWnd);
        }

        public bool GetIsMinimized()
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            return PInvoke.IsIconic(_hWnd);
        }

        public nint GetOwner()
        {
            if (_hWnd != HWND.Null)
                return PInvoke.GetWindowLongPtr(_hWnd, WINDOW_LONG_PTR_INDEX.GWLP_HWNDPARENT);
            return Owner;
        }

        public virtual void SetOwner(nint owner)
        {
            if (Owner != owner)
                Owner = owner;
            if (_hWnd != HWND.Null)
                PInvoke.SetWindowLongPtr(_hWnd, WINDOW_LONG_PTR_INDEX.GWLP_HWNDPARENT, owner);
        }

        public virtual void SetTopmost(bool value)
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            var HWND_TOPMOST = -1;
            var HWND_NOTOPMOST = -2;
            var flags = SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
            if (!PInvoke.SetWindowPos(_hWnd, new(new(value ? HWND_TOPMOST : HWND_NOTOPMOST)), 0, 0, 0, 0, flags))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public virtual bool TryActive()
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            return PInvoke.SetForegroundWindow(_hWnd);
        }

        public void PostMessage(uint msg, nuint lParam, nint wParam)
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            if (!PInvoke.PostMessage(_hWnd, msg, lParam, wParam))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public nint SendMessage(uint msg, nuint lParam, nint wParam)
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            return PInvoke.SendMessage(_hWnd, msg, lParam, wParam);
        }

        public void Close()
        {
            if (_hWnd == HWND.Null)
                throw new InvalidOperationException();
            PInvoke.PostMessage(_hWnd, PInvoke.WM_SYSCOMMAND, PInvoke.SC_CLOSE, 0);
        }

        public void Destory()
        {
            if (_hWnd != HWND.Null && !PInvoke.DestroyWindow(_hWnd))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}

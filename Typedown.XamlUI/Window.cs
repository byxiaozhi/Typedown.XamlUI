using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Typedown.XamlUI
{
    public abstract partial class Window : DependencyObject, IDisposable
    {
        private static readonly WNDPROC _wndProc = new(StaticWndProc);

        private static readonly HashSet<Type> _registeredClasses = new();

        private static readonly Dictionary<HWND, Window> _instances = new();

        private bool _isDisposed = false;

        private HWND _hWnd = HWND.Null;

        private readonly object _gate = new();

        private static unsafe void EnsureRegisterClass(Type type)
        {
            if (_registeredClasses.Contains(type))
                return;
            fixed (char* pClassName = type.FullName)
            {
                var hInstance = PInvoke.GetModuleHandle((char*)0);
                var wndClass = new WNDCLASSEXW();
                wndClass.cbSize = (uint)Marshal.SizeOf(wndClass);
                wndClass.style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW;
                wndClass.lpfnWndProc = _wndProc;
                wndClass.cbClsExtra = 0;
                wndClass.cbWndExtra = 0;
                wndClass.hInstance = hInstance;
                wndClass.hIcon = PInvoke.LoadIcon(hInstance, PInvoke.IDI_APPLICATION);
                wndClass.hCursor = PInvoke.LoadCursor(hInstance, PInvoke.IDC_ARROW);
                wndClass.hbrBackground = PInvoke.GetSysColorBrush(SYS_COLOR_INDEX.COLOR_WINDOW);
                wndClass.lpszMenuName = null;
                wndClass.lpszClassName = pClassName;
                var atom = PInvoke.RegisterClassEx(wndClass);
                if (atom == 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                _registeredClasses.Add(type);
            }
        }

        private static LRESULT StaticWndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            if (_instances.TryGetValue(hWnd, out var instance))
            {
                instance.UpdateDependencyProperty(msg, wParam, lParam);
                nint result = 0;
                bool handled = false;
                IReadOnlyList<WndProc> hooks;
                lock (instance._gate)
                    hooks = instance._windowMessagehooks;
                if (hooks != null)
                {
                    foreach (var wndProc in hooks)
                    {
                        result = wndProc(hWnd, msg, wParam, lParam, ref handled);
                        if (handled)
                            break;
                    }
                }
                if (!handled)
                {
                    result = instance.WndProc(hWnd.Value, msg, wParam, lParam);
                }
                if (msg == PInvoke.WM_DESTROY)
                {
                    _instances.Remove(hWnd);
                    instance.Closed?.Invoke(instance, new());
                    instance._hWnd = HWND.Null;
                }
                return new(result);
            }
            return PInvoke.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        protected virtual unsafe void InitializeWindow(WindowStyle? style = null, WindowExStyle? exStyle = null, nint hWndParent = 0)
        {
            CheckDisposed();

            if (_hWnd != HWND.Null)
                return;

            var type = GetType();
            EnsureRegisterClass(type);

            exStyle ??= WindowExStyle.WS_EX_NOREDIRECTIONBITMAP;
            style ??= WindowStyle.WS_OVERLAPPEDWINDOW;
            _hWnd = PInvoke.CreateWindowEx((WINDOW_EX_STYLE)exStyle, type.FullName, Title, (WINDOW_STYLE)style, PInvoke.CW_USEDEFAULT, 0, PInvoke.CW_USEDEFAULT, 0, new(hWndParent), null, null, null);
            if (_hWnd == HWND.Null)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            DisplayDpi = PInvoke.GetDpiForWindow(_hWnd);
            if (Width >= 0 && Height >= 0)
                SetSize(new(Width, Height));
            if (Left >= 0 && Top >= 0)
                SetLocation(new(Left, Top));
            if (Owner != 0)
                SetOwner(Owner);

            _instances.Add(_hWnd, this);
        }

        protected virtual nint WndProc(nint hWnd, uint msg, nuint wParam, nint lParam)
        {
            switch (msg)
            {
                case PInvoke.WM_DPICHANGED:
                    {
                        var rcNew = Marshal.PtrToStructure<RECT>(lParam);
                        var flags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
                        PInvoke.SetWindowPos(new(hWnd), HWND.Null, rcNew.X, rcNew.Y, rcNew.Width, rcNew.Height, flags);
                        break;
                    }
                case PInvoke.WM_CLOSE:
                    {
                        var args = new ClosingEventArgs();
                        Closing?.Invoke(this, args);
                        if (args.Cancel)
                            return 0;
                        break;
                    }
            }
            return PInvoke.DefWindowProc(new(hWnd), msg, wParam, lParam);
        }

        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(string.Empty);
        }

        public virtual void Dispose()
        {
            if (_hWnd != HWND.Null && !PInvoke.DestroyWindow(_hWnd))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            _isDisposed = true;
        }
    }
}

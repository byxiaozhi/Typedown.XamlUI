using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Typedown.XamlUI
{
    internal class DragBarWindow : Window
    {
        private readonly XamlWindow _window;

        private static readonly List<WeakReference> _instances = new();

        private readonly WeakReference _ref;

        public static new List<DragBarWindow> AllWindows
        {
            get
            {
                lock (_instances)
                    return _instances.Select(x => x.Target as DragBarWindow).Where(x => x != null).ToList();
            }
        }

        public DragBarWindow(XamlWindow window)
        {
            _ref = new(this);
            lock (_instances)
                _instances.Add(_ref);
            _window = window;
            InitializeWindow();
        }

        protected override void InitializeWindow(WindowStyle? style = null, WindowExStyle? exStyle = null, nint hWndParent = 0)
        {
            style = WindowStyle.WS_CHILD | WindowStyle.WS_VISIBLE;
            exStyle = WindowExStyle.WS_EX_NOREDIRECTIONBITMAP | WindowExStyle.WS_EX_LAYERED;
            base.InitializeWindow(style, exStyle, _window.Handle);
            PInvoke.SetLayeredWindowAttributes(new(Handle), new(0), 255, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA);
            PInvoke.SetWindowPos(new(Handle), HWND.Null, 0, 0, 0, 0, 0);
        }

        protected override nint WndProc(nint hWnd, uint msg, nuint wParam, nint lParam)
        {
            switch (msg)
            {
                case PInvoke.WM_NCHITTEST:
                case PInvoke.WM_NCLBUTTONDOWN:
                case PInvoke.WM_NCLBUTTONUP:
                case PInvoke.WM_NCLBUTTONDBLCLK:
                case PInvoke.WM_NCRBUTTONDOWN:
                case PInvoke.WM_NCRBUTTONUP:
                case PInvoke.WM_NCRBUTTONDBLCLK:
                case PInvoke.WM_NCMOUSELEAVE:
                    return _window.SendMessage(msg, wParam, lParam);
                default:
                    break;
            }
            return base.WndProc(hWnd, msg, wParam, lParam);
        }

        ~DragBarWindow()
        {
            lock (_instances)
                _instances.Remove(_ref);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Typedown.XamlUI
{
    public partial class XamlWindow : Window
    {
        private static readonly List<WeakReference> _instances = new();

        private readonly WeakReference _ref;

        private DesktopWindowXamlSource _xamlSource;

        private readonly RootLayout _rootLayout = new();

        private readonly UISettings _uiSettings = new();

        private bool _isDisposed = false;

        private static readonly bool _isWin11OrHigher = Environment.OSVersion.Version.Build >= 22000;

        public XamlWindow()
        {
            _ref = new(this);
            lock (_instances)
                _instances.Add(_ref);
            InitializeBinding();
            _rootLayout.Loaded += OnLoaded;
            _rootLayout.Unloaded += OnUnloaded;
            Closed += OnClosed;
        }

        ~XamlWindow()
        {
            lock (_instances)
                _instances.Remove(_ref);
        }

        protected override void InitializeWindow(WindowStyle? style = null, WindowExStyle? exStyle = null, nint hWndParent = 0)
        {
            CheckDisposed();
            if (_xamlSource != null)
                return;

            if (!exStyle.HasValue)
            {
                exStyle = WindowExStyle.WS_EX_NOREDIRECTIONBITMAP;
            }
            if (!style.HasValue)
            {
                style = WindowStyle.WS_OVERLAPPED | WindowStyle.WS_CAPTION | WindowStyle.WS_SYSMENU;
                switch (ResizeMode)
                {
                    case WindowResizeMode.CanMinimize:
                        style |= WindowStyle.WS_MINIMIZEBOX;
                        break;
                    case WindowResizeMode.CanResize:
                        style |= WindowStyle.WS_THICKFRAME | WindowStyle.WS_MINIMIZEBOX | WindowStyle.WS_MAXIMIZEBOX;
                        break;
                }
            }
            else
            {
                if (style.Value.HasFlag(WindowStyle.WS_THICKFRAME))
                    ResizeMode = WindowResizeMode.CanResize;
                else if (style.Value.HasFlag(WindowStyle.WS_MINIMIZEBOX))
                    ResizeMode = WindowResizeMode.CanMinimize;
                else
                    ResizeMode = WindowResizeMode.NoResize;
            }

            base.InitializeWindow(style, exStyle, hWndParent);

            _xamlSource = new() { Content = _rootLayout };
            _xamlSource.TakeFocusRequested += OnXamlSourceTakeFocusRequested;
            _uiSettings.ColorValuesChanged += OnColorValuesChanged;
            var xamlSourceNative = (_xamlSource as IDesktopWindowXamlSourceNative);
            xamlSourceNative.AttachToWindow(Handle);
            XamlSourceHandle = xamlSourceNative.WindowHandle;
            CoreWindowHelper.DetachCoreWindow();

            if (Topmost) SetTopmost(true);
            if (!Frame) UpdateFrame();
            UpdateTheme();
        }

        private static Thickness GetFrameBorderThickness(uint dpi)
        {
            var cxFrame = PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CXFRAME, dpi);
            var cxPaddedBorder = PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CXPADDEDBORDER, dpi);
            return new(cxFrame + cxPaddedBorder);
        }

        protected virtual unsafe bool HitTest(System.Drawing.Point ptScreen, out uint hitTestResult)
        {
            hitTestResult = 0;
            if (!Frame)
            {
                RECT rcClient;
                System.Drawing.Point ptClient = ptScreen;
                PInvoke.GetClientRect(new(Handle), &rcClient);
                PInvoke.ScreenToClient(new(Handle), &ptClient);
                var thickness = GetFrameBorderThickness(DisplayDpi);
                var isTop = ptClient.Y < thickness.Top;
                var isBottom = ptClient.Y > rcClient.Height;
                var isLeft = ptClient.X < 0;
                var isRight = ptClient.X > rcClient.Width;

                if (WindowState == WindowState.Normal && HasStyle(WindowStyle.WS_THICKFRAME))
                {
                    if (isTop)
                    {
                        if (isLeft) hitTestResult = PInvoke.HTTOPLEFT;
                        else if (isRight) hitTestResult = PInvoke.HTTOPRIGHT;
                        else hitTestResult = PInvoke.HTTOP;
                    }
                    else if (isBottom)
                    {
                        if (isLeft) hitTestResult = PInvoke.HTBOTTOMLEFT;
                        else if (isRight) hitTestResult = PInvoke.HTBOTTOMRIGHT;
                        else hitTestResult = PInvoke.HTBOTTOM;
                    }
                    else if (isLeft) hitTestResult = PInvoke.HTLEFT;
                    else if (isRight) hitTestResult = PInvoke.HTRIGHT;
                }
                else if (isLeft || isRight || isBottom)
                {
                    hitTestResult = PInvoke.HTBORDER;
                }

                if (hitTestResult != 0)
                {
                    return true;
                }
            }

            foreach (var dragBarWindow in DragBarWindow.AllWindows)
            {
                RECT rect;
                if (dragBarWindow.Handle != 0 &&
                    PInvoke.GetWindowRect(new(dragBarWindow.Handle), &rect) &&
                    PInvoke.PtInRect(&rect, ptScreen))
                {
                    hitTestResult = PInvoke.HTCAPTION;
                    return true;
                }
            }

            return false;
        }

        protected override unsafe nint WndProc(nint hWnd, uint msg, nuint wParam, nint lParam)
        {
            UpdateDependencyProperty(msg, wParam, lParam);
            ApplyPatch(msg, wParam, lParam);
            switch (msg)
            {
                case PInvoke.WM_SIZE:
                    {
                        var newWidth = lParam & 0xffff;
                        var newHeight = lParam >> 16;
                        var thickness = GetFrameBorderThickness(DisplayDpi);
                        var topBorderThickness = Frame ? 0 : (int)Math.Floor(GetIsMaximized() ? thickness.Top : ScalingFactor);
                        var flags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW;
                        PInvoke.SetWindowPos(new(XamlSourceHandle), HWND.Null, 0, topBorderThickness, (int)newWidth, (int)newHeight - topBorderThickness, flags);
                        break;
                    }
                case PInvoke.WM_DPICHANGED:
                    _ = Dispatcher.RunIdleAsync(_ => _rootLayout.InvalidateMeasure());
                    break;
                case PInvoke.WM_NCCALCSIZE:
                    {
                        if (wParam != 0 && !Frame)
                        {
                            var thickness = GetFrameBorderThickness(DisplayDpi);
                            var p = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(lParam);
                            p.rgrc._0.left += (int)thickness.Left;
                            p.rgrc._0.right -= (int)thickness.Right;
                            p.rgrc._0.bottom -= (int)thickness.Bottom;
                            Marshal.StructureToPtr(p.rgrc, lParam, false);
                            return IntPtr.Zero;
                        }
                        break;
                    }
                case PInvoke.WM_NCRBUTTONUP:
                    if (wParam == PInvoke.HTCAPTION)
                    {
                        OpenSystemMenu(new(lParam & 0xffff, lParam >> 16));
                    }
                    break;
                case PInvoke.WM_NCHITTEST:
                    {
                        LRESULT dwmResult;
                        if (PInvoke.DwmDefWindowProc(new(hWnd), msg, wParam, lParam, &dwmResult))
                        {
                            return dwmResult;
                        }
                        if (HitTest(new((int)(lParam & 0xffff), (int)(lParam >> 16)), out var hitTest))
                        {
                            return (nint)hitTest;
                        }
                    }
                    break;
                case PInvoke.WM_NCMOUSELEAVE:
                    {
                        LRESULT dwmResult;
                        if (PInvoke.DwmDefWindowProc(new(hWnd), msg, wParam, lParam, &dwmResult))
                        {
                            return dwmResult;
                        }
                    }
                    break;
                case PInvoke.WM_GETMINMAXINFO:
                    {
                        var p = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                        p.ptMinTrackSize.X = (int)Math.Round(MinWidth * ScalingFactor);
                        p.ptMinTrackSize.Y = (int)Math.Round(MinHeight * ScalingFactor);
                        Marshal.StructureToPtr(p, lParam, false);
                        break;
                    }
            }
            return base.WndProc(hWnd, msg, wParam, lParam);
        }

        private async void OnColorValuesChanged(UISettings sender, object args)
        {
            await _rootLayout.Dispatcher.RunIdleAsync(_ => UpdateTheme());
        }

        private unsafe void SetDarkMode(bool isDarkMode)
        {
            uint darkModeValue = (!Frame && !_isWin11OrHigher) || isDarkMode ? 1u : 0;
            PInvoke.DwmSetWindowAttribute(new(Handle), DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &darkModeValue, (uint)Marshal.SizeOf(typeof(uint)));
            var actualTheme = isDarkMode ? ElementTheme.Dark : ElementTheme.Light;
            if (ActualTheme != actualTheme)
            {
                ActualTheme = actualTheme;
                _rootLayout.RequestedTheme = ActualTheme;
            }
        }

        private void UpdateTheme()
        {
            var requestedTheme = RequestedTheme;
            if (requestedTheme == ElementTheme.Default)
                requestedTheme = XamlApplication.Current?.RequestedTheme ?? ElementTheme.Default;
            SetDarkMode(requestedTheme switch
            {
                ElementTheme.Light => false,
                ElementTheme.Dark => true,
                _ => _uiSettings.GetColorValue(UIColorType.Foreground).IsColorLight()
            });
        }

        private unsafe void UpdateFrame()
        {
            if (!Frame)
            {
                if (!_rootLayout.Children.OfType<CaptionControlGroup>().Any())
                    _rootLayout.Children.Add(CaptionControlGroup.CreateForXamlWindow(this));
            }
            else
            {
                foreach (var control in _rootLayout.Children.OfType<CaptionControlGroup>())
                    _rootLayout.Children.Remove(control);
            }
            if (!_isWin11OrHigher)
            {
                var margin = new MARGINS() { cyTopHeight = (int)Math.Round(32 * ScalingFactor) };
                PInvoke.DwmExtendFrameIntoClientArea(new(Handle), &margin);
            }
            if (Handle != 0)
            {
                var flags = SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE;
                PInvoke.SetWindowPos(new(Handle), HWND.Null, 0, 0, 0, 0, flags);
            }
            UpdateTheme();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (XamlApplication.Current is not null)
                XamlApplication.Current.RequestedThemeChanged += OnApplicationRequestedThemeChanged;
            Loaded?.Invoke(this, EventArgs.Empty);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (XamlApplication.Current is not null)
                XamlApplication.Current.RequestedThemeChanged -= OnApplicationRequestedThemeChanged;
            Unloaded?.Invoke(this, EventArgs.Empty);
        }

        private void OnApplicationRequestedThemeChanged(object sender, RequestedThemeChangedEventArgs e)
        {
            UpdateTheme();
        }

        private void OnXamlSourceTakeFocusRequested(DesktopWindowXamlSource sender, DesktopWindowXamlSourceTakeFocusRequestedEventArgs args)
        {
            sender.NavigateFocus(args.Request);
        }

        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(string.Empty);
        }

        private void OnClosed(object sender, ClosedEventArgs e)
        {
            _xamlSource?.Dispose();
            _xamlSource = null;
            XamlSourceHandle = IntPtr.Zero;
            _uiSettings.ColorValuesChanged -= OnColorValuesChanged;
        }

        public override void Dispose()
        {
            _xamlSource?.Dispose();
            _xamlSource = null;
            XamlSourceHandle = IntPtr.Zero;
            _uiSettings.ColorValuesChanged -= OnColorValuesChanged;
            _isDisposed = true;
            base.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Typedown.XamlUI
{
    [ContentProperty(Name = nameof(Content))]
    public partial class XamlWindow
    {
        public static DependencyProperty ContentProperty { get; } = DependencyProperty.Register(nameof(Content), typeof(UIElement), typeof(XamlWindow), new(null, OnDependencyPropertyChanged));

        public UIElement Content { get => (UIElement)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

        public static DependencyProperty RequestedThemeProperty { get; } = DependencyProperty.Register(nameof(RequestedTheme), typeof(ElementTheme), typeof(XamlWindow), new(ElementTheme.Default, OnDependencyPropertyChanged));

        public ElementTheme RequestedTheme { get => (ElementTheme)GetValue(RequestedThemeProperty); set => SetValue(RequestedThemeProperty, value); }

        public static DependencyProperty ActualThemeProperty { get; } = DependencyProperty.Register(nameof(ActualTheme), typeof(ElementTheme), typeof(XamlWindow), new(ElementTheme.Default, OnDependencyPropertyChanged));

        public ElementTheme ActualTheme { get => (ElementTheme)GetValue(ActualThemeProperty); private set => SetValue(ActualThemeProperty, value); }

        public static DependencyProperty WindowStateProperty { get; } = DependencyProperty.Register(nameof(WindowState), typeof(WindowState), typeof(XamlWindow), new(WindowState.Normal, OnDependencyPropertyChanged));

        public WindowState WindowState { get => (WindowState)GetValue(WindowStateProperty); set => SetValue(WindowStateProperty, value); }

        public static DependencyProperty ResizeModeProperty { get; } = DependencyProperty.Register(nameof(ResizeMode), typeof(WindowResizeMode), typeof(XamlWindow), new(WindowResizeMode.CanResize, OnDependencyPropertyChanged));

        public WindowResizeMode ResizeMode { get => (WindowResizeMode)GetValue(ResizeModeProperty); set => SetValue(ResizeModeProperty, value); }

        public static DependencyProperty MinWidthProperty { get; } = DependencyProperty.Register(nameof(MinWidth), typeof(double), typeof(XamlWindow), new(0d, OnDependencyPropertyChanged));

        public double MinWidth { get => (double)GetValue(MinWidthProperty); set => SetValue(MinWidthProperty, value); }

        public static DependencyProperty MinHeightProperty { get; } = DependencyProperty.Register(nameof(MinHeight), typeof(double), typeof(XamlWindow), new(0d, OnDependencyPropertyChanged));

        public double MinHeight { get => (double)GetValue(MinHeightProperty); set => SetValue(MinHeightProperty, value); }

        public static DependencyProperty IsActiveProperty { get; } = DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(XamlWindow), new(false, OnDependencyPropertyChanged));

        public bool IsActive { get => (bool)GetValue(IsActiveProperty); private set => SetValue(IsActiveProperty, value); }

        public static DependencyProperty TopmostProperty { get; } = DependencyProperty.Register(nameof(Topmost), typeof(bool), typeof(XamlWindow), new(false, OnDependencyPropertyChanged));

        public bool Topmost { get => (bool)GetValue(TopmostProperty); set => SetValue(TopmostProperty, value); }

        public static DependencyProperty FrameProperty { get; } = DependencyProperty.Register(nameof(Frame), typeof(bool), typeof(XamlWindow), new(true, OnDependencyPropertyChanged));

        public bool Frame { get => (bool)GetValue(FrameProperty); set => SetValue(FrameProperty, value); }

        public static DependencyProperty DragProperty { get; } = DependencyProperty.RegisterAttached(nameof(Drag), typeof(bool), typeof(XamlWindow), new(false, OnAttachedDependencyPropertyChanged));

        public bool Drag { get => (bool)GetValue(DragProperty); set => SetValue(DragProperty, value); }

        public WindowStartupLocation WindowStartupLocation { get; set; }

        public static new List<XamlWindow> AllWindows
        {
            get
            {
                lock (_instances)
                    return _instances.Select(x => x.Target as XamlWindow).Where(x => x != null).ToList();
            }
        }

        public ResourceDictionary Resources { get => _rootLayout.Resources; set => _rootLayout.Resources = value; }

        public object DataContext { get => _rootLayout.DataContext; set => _rootLayout.DataContext = value; }

        public XamlRoot XamlRoot => _rootLayout.XamlRoot;

        public nint XamlSourceHandle { get; private set; }

        private bool _propertiesUpdating = false;

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = d as XamlWindow;

            if (!target._propertiesUpdating && target.Handle != IntPtr.Zero)
            {
                if (e.Property == RequestedThemeProperty)
                {
                    target.UpdateTheme();
                }
                else if (e.Property == WindowStateProperty)
                {
                    var isMinimized = target.GetIsMinimized();
                    var isMaximized = target.GetIsMaximized();
                    if (target.WindowState == WindowState.Minimized && !isMinimized)
                        target.Show(ShowWindowCommand.SW_MINIMIZE);
                    if (target.WindowState == WindowState.Maximized && !isMaximized)
                        target.Show(ShowWindowCommand.SW_MAXIMIZE);
                    if (target.WindowState == WindowState.Normal && (isMaximized || isMinimized))
                        target.Show(ShowWindowCommand.SW_NORMAL);
                }
                else if (e.Property == ResizeModeProperty)
                {
                    var style = target.GetStyle();
                    switch (target.ResizeMode)
                    {
                        case WindowResizeMode.NoResize:
                            style &= ~(WindowStyle.WS_THICKFRAME | WindowStyle.WS_MINIMIZEBOX | WindowStyle.WS_MAXIMIZEBOX);
                            break;
                        case WindowResizeMode.CanMinimize:
                            style &= ~(WindowStyle.WS_THICKFRAME | WindowStyle.WS_MAXIMIZEBOX);
                            style |= WindowStyle.WS_MINIMIZEBOX;
                            break;
                        case WindowResizeMode.CanResize:
                            style |= WindowStyle.WS_THICKFRAME | WindowStyle.WS_MINIMIZEBOX | WindowStyle.WS_MAXIMIZEBOX;
                            break;
                    }
                    target.SetStyle(style);
                }
                else if (e.Property == TopmostProperty)
                {
                    target.SetTopmost(target.Topmost);
                }
                else if (e.Property == FrameProperty)
                {
                    target.UpdateFrame();
                }

            }

            if (e.Property == ContentProperty)
            {
                target._rootLayout.Content = target.Content;
            }
            else if (e.Property == WindowStateProperty)
            {
                target.StateChanged?.Invoke(target, new(target.WindowState));
            }
            else if (e.Property == ActualThemeProperty)
            {
                target.ActualThemeChanged?.Invoke(target, new(target.ActualTheme));
            }
            else if (e.Property == ResizeModeProperty)
            {
                target.ResizeModeChanged?.Invoke(target, new(target.ResizeMode));
            }
        }

        private static void OnAttachedDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == DragProperty && d is FrameworkElement element)
            {
                if ((bool)e.NewValue)
                    DragBar.AttachToFrameworkElement(element);
                else
                    DragBar.UnattachToFrameworkElement(element);
            }
        }

        private void InitializeBinding()
        {
            _rootLayout.SetBinding(
                FrameworkElement.RequestedThemeProperty,
                new Binding()
                {
                    Source = this,
                    Path = new(nameof(RequestedThemeProperty)),
                    Mode = BindingMode.TwoWay
                });
        }

        private void UpdateDependencyProperty(uint msg, nuint wParam, nint lParam)
        {
            _propertiesUpdating = true;
            switch (msg)
            {
                case PInvoke.WM_NCACTIVATE:
                    {
                        IsActive = wParam != 0;
                        IsActiveChanged?.Invoke(this, new(IsActive));
                        break;
                    }
                case PInvoke.WM_SIZE:
                    {
                        WindowState windowState = wParam switch
                        {
                            PInvoke.SIZE_RESTORED => WindowState.Normal,
                            PInvoke.SIZE_MINIMIZED => WindowState.Minimized,
                            PInvoke.SIZE_MAXIMIZED => WindowState.Maximized,
                            _ => WindowState
                        };
                        if (WindowState != windowState)
                            WindowState = windowState;
                        break;
                    }
                case PInvoke.WM_STYLECHANGED:
                    {
                        if ((WINDOW_LONG_PTR_INDEX)wParam == WINDOW_LONG_PTR_INDEX.GWL_STYLE)
                        {
                            var styleStruct = Marshal.PtrToStructure<STYLESTRUCT>(lParam);
                            var newStyle = (WindowStyle)styleStruct.styleNew;
                            WindowResizeMode resizeMode;
                            if (newStyle.HasFlag(WindowStyle.WS_MINIMIZEBOX) && newStyle.HasFlag(WindowStyle.WS_MAXIMIZEBOX))
                                resizeMode = WindowResizeMode.CanResize;
                            else if (newStyle.HasFlag(WindowStyle.WS_MINIMIZEBOX))
                                resizeMode = WindowResizeMode.CanMinimize;
                            else
                                resizeMode = WindowResizeMode.NoResize;
                            if (ResizeMode != resizeMode)
                                ResizeMode = resizeMode;
                        }
                        break;
                    }
            }
            _propertiesUpdating = false;
        }

        public static void SetDrag(FrameworkElement target, bool value)
        {
            target.SetValue(DragProperty, value);
        }

        public static bool GetDrag(FrameworkElement target)
        {
            return (bool)target.GetValue(DragProperty);
        }

        private ShowWindowCommand GetShowWindowCommand()
        {
            var canMinimized = ResizeMode == WindowResizeMode.CanMinimize || ResizeMode == WindowResizeMode.CanResize;
            var canMaximized = ResizeMode == WindowResizeMode.CanResize;
            if (WindowState == WindowState.Minimized && canMinimized)
                return ShowWindowCommand.SW_SHOWMINIMIZED;
            if (WindowState == WindowState.Maximized && canMaximized)
                return ShowWindowCommand.SW_SHOWMAXIMIZED;
            return ShowWindowCommand.SW_SHOWNORMAL;
        }
    }
}

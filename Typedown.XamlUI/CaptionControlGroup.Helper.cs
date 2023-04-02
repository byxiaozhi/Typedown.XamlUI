using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Win32;

namespace Typedown.XamlUI
{
    public partial class CaptionControlGroup
    {
        internal static unsafe CaptionControlGroup CreateForXamlWindow(XamlWindow window)
        {
            var control = new CaptionControlGroup()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
            };

            control.MinimizeClick += (s, e) => window.WindowState = WindowState.Minimized;
            control.MaximizeOrRestoreClick += (s, e) => window.WindowState = window.GetIsMaximized() ? WindowState.Normal : WindowState.Maximized;
            control.CloseClick += (s, e) => window.Close();
            DragBar.AttachToFrameworkElement(control);

            string stateMinButton = "Normal";
            string stateMaxButton = "Normal";
            string stateCloseButton = "Normal";

            control.Loaded += (s, e) =>
            {
                window.StateChanged += OnWindowStateChanged;
                window.IsActiveChanged += OnWindowIsActiveChanged;
                window.ResizeModeChanged += OnWindowResizeModeChanged;
                window.AddWindowMessageHook(WndProcHook);
                OnWindowStateChanged(control, new(window.WindowState));
                OnWindowIsActiveChanged(control, new(window.IsActive));
                OnWindowResizeModeChanged(control, new(window.ResizeMode));
            };

            control.Unloaded += (s, e) =>
            {
                window.StateChanged -= OnWindowStateChanged;
                window.IsActiveChanged -= OnWindowIsActiveChanged;
                window.ResizeModeChanged -= OnWindowResizeModeChanged;
                window.RemoveWindowMessageHook(WndProcHook);
            };

            void OnWindowStateChanged(object sender, StateChangedEventArgs e)
            {
                control.IsMaximized = window.WindowState == WindowState.Maximized;
            };

            void OnWindowIsActiveChanged(object sender, IsActiveChangedEventArgs e)
            {
                control.IsActive = window.IsActive;
            };

            void OnWindowResizeModeChanged(object sender, ResizeModeChangedEventArgs e)
            {
                switch (e.NewWindowResizeMode)
                {
                    case WindowResizeMode.NoResize:
                        control.MinButton.Visibility = Visibility.Collapsed;
                        control.MaxButton.Visibility = Visibility.Collapsed;
                        break;
                    case WindowResizeMode.CanMinimize:
                        control.MinButton.Visibility = Visibility.Visible;
                        control.MaxButton.Visibility = Visibility.Visible;
                        control.MaxButton.IsEnabled = false;
                        break;
                    case WindowResizeMode.CanResize:
                        control.MinButton.Visibility = Visibility.Visible;
                        control.MaxButton.Visibility = Visibility.Visible;
                        control.MaxButton.IsEnabled = true;
                        break;
                }
            }

            string GetButtonState(nuint hitTestResult)
            {
                return hitTestResult switch
                {
                    PInvoke.HTMINBUTTON => stateMinButton,
                    PInvoke.HTMAXBUTTON => stateMaxButton,
                    PInvoke.HTCLOSE => stateCloseButton,
                    _ => null
                };
            }

            void SetButtonState(nuint hitTestResult, string state)
            {
                switch (hitTestResult)
                {
                    case PInvoke.HTMINBUTTON:
                        stateMinButton = control.MinButton.IsEnabled ? state : "Disabled";
                        stateMaxButton = control.MaxButton.IsEnabled ? "Normal" : "Disabled";
                        stateCloseButton = control.CloseButton.IsEnabled ? "Normal" : "Disabled";
                        break;
                    case PInvoke.HTMAXBUTTON:
                        stateMinButton = control.MinButton.IsEnabled ? "Normal" : "Disabled";
                        stateMaxButton = control.MaxButton.IsEnabled ? state : "Disabled";
                        stateCloseButton = control.CloseButton.IsEnabled ? "Normal" : "Disabled";
                        break;
                    case PInvoke.HTCLOSE:
                        stateMinButton = control.MinButton.IsEnabled ? "Normal" : "Disabled";
                        stateMaxButton = control.MaxButton.IsEnabled ? "Normal" : "Disabled";
                        stateCloseButton = control.CloseButton.IsEnabled ? state : "Disabled";
                        break;
                }
                VisualStateManager.GoToState(control.MinButton, stateMinButton, true);
                VisualStateManager.GoToState(control.MaxButton, stateMaxButton, true);
                VisualStateManager.GoToState(control.CloseButton, stateCloseButton, true);
            }

            void ResetButtonState()
            {
                stateMinButton = control.MinButton.IsEnabled ? "Normal" : "Disabled";
                stateMaxButton = control.MaxButton.IsEnabled ? "Normal" : "Disabled";
                stateCloseButton = control.CloseButton.IsEnabled ? "Normal" : "Disabled";
                VisualStateManager.GoToState(control.MinButton, stateMinButton, true);
                VisualStateManager.GoToState(control.MaxButton, stateMaxButton, true);
                VisualStateManager.GoToState(control.CloseButton, stateCloseButton, true);
            }

            nint WndProcHook(nint hWnd, uint msg, nuint wParam, nint lParam, ref bool handled)
            {
                switch (msg)
                {
                    case PInvoke.WM_NCHITTEST:
                        if (VisualTreeHelper.GetParent(window.XamlRoot?.Content) is not FrameworkElement border)
                            break;
                        var ptScreenBytes = BitConverter.GetBytes(lParam);
                        var ptScreen = new System.Drawing.Point(BitConverter.ToInt16(ptScreenBytes, 0), BitConverter.ToInt16(ptScreenBytes, 2));
                        PInvoke.ScreenToClient(new(window.XamlSourceHandle), &ptScreen);
                        var pointer = new Windows.Foundation.Point(ptScreen.X / window.ScalingFactor, ptScreen.Y / window.ScalingFactor);

                        var getBtnRect = (Button btn) => btn.TransformToVisual(border).TransformBounds(new(0, 0, btn.ActualWidth, btn.ActualHeight));
                        var rcMinBtn = getBtnRect(control.MinButton);
                        var rcMaxBtn = getBtnRect(control.MaxButton);
                        var rcCloseBtn = getBtnRect(control.CloseButton);

                        uint htResult = 0;
                        if (control.MinButton.IsEnabled && rcMinBtn.Contains(pointer))
                        {
                            htResult = PInvoke.HTMINBUTTON;
                        }
                        else if (control.MaxButton.IsEnabled && rcMaxBtn.Contains(pointer))
                        {
                            htResult = PInvoke.HTMAXBUTTON;
                        }
                        else if (control.CloseButton.IsEnabled && rcCloseBtn.Contains(pointer))
                        {
                            htResult = PInvoke.HTCLOSE;
                        }
                        if (htResult != 0)
                        {
                            if (GetButtonState(htResult) != "Pressed")
                                SetButtonState(htResult, "PointerOver");
                            handled = true;
                            return (nint)htResult;
                        }
                        else
                        {
                            ResetButtonState();
                        }
                        break;
                    case PInvoke.WM_NCLBUTTONDOWN:
                        switch (wParam)
                        {
                            case PInvoke.HTMINBUTTON:
                            case PInvoke.HTMAXBUTTON:
                            case PInvoke.HTCLOSE:
                                SetButtonState(wParam, "Pressed");
                                handled = true;
                                break;
                        }
                        break;
                    case PInvoke.WM_NCLBUTTONUP:
                        switch (wParam)
                        {
                            case PInvoke.HTMINBUTTON:
                                SetButtonState(wParam, "Normal");
                                control.OnMinButtonClick(control, null);
                                handled = true;
                                break;
                            case PInvoke.HTMAXBUTTON:
                                SetButtonState(wParam, "Normal");
                                control.OnMaxButtonClick(control, null);
                                handled = true;
                                break;
                            case PInvoke.HTCLOSE:
                                SetButtonState(wParam, "Normal");
                                control.OnCloseButtonClick(control, null);
                                handled = true;
                                break;
                        }
                        break;
                    case PInvoke.WM_NCMOUSELEAVE:
                        ResetButtonState();
                        break;
                }
                return 0;
            }

            return control;
        }

    }
}

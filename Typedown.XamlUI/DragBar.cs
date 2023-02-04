using System;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Typedown.XamlUI
{
    public class DragBar : IDisposable
    {
        private DragBarWindow _dragBarWindow = null;

        private readonly WeakReference<FrameworkElement> _frameworkElementRef;

        private static readonly ConditionalWeakTable<FrameworkElement, DragBar> _instances = new();

        private XamlWindow _xamlWindow;

        private RootLayout _rootLayout;

        private DragBar(FrameworkElement frameworkElement)
        {
            _frameworkElementRef = new(frameworkElement);
            frameworkElement.Loaded += OnLoaded;
            frameworkElement.Unloaded += OnUnloaded;
            frameworkElement.SizeChanged += OnSizeChanged;
            frameworkElement.LayoutUpdated += OnLayoutUpdated;
            if (frameworkElement.IsLoaded)
                CreateDragBarWindow(frameworkElement);
        }

        public static DragBar AttachToFrameworkElement(FrameworkElement frameworkElement)
        {
            lock (_instances)
            {
                if (_instances.TryGetValue(frameworkElement, out var exist))
                    return exist;
                var dragBar = new DragBar(frameworkElement);
                _instances.Add(frameworkElement, dragBar);
                return dragBar;
            }
        }

        public static DragBar UnattachToFrameworkElement(FrameworkElement frameworkElement)
        {
            lock (_instances)
            {
                if (_instances.TryGetValue(frameworkElement, out var exist))
                {
                    _instances.Remove(frameworkElement);
                    exist.Dispose();
                    return exist;
                }
                return null;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CreateDragBarWindow(sender as FrameworkElement);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DestoryDragBarWindow();
        }

        private void OnSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            UpdateDragBarWindowPos(sender as FrameworkElement);
        }

        private void OnLayoutUpdated(object sender, object e)
        {
            UpdateDragBarWindowPos(sender as FrameworkElement);
        }

        private void OnWindowSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            if (_frameworkElementRef.TryGetTarget(out var frameworkElement))
                UpdateDragBarWindowPos(frameworkElement);
        }

        private void CreateDragBarWindow(FrameworkElement frameworkElement)
        {
            if (_dragBarWindow != null)
                return;
            _xamlWindow = XamlWindow.GetWindow(frameworkElement);
            if (_xamlWindow == null)
                return;
            if (_xamlWindow.XamlRoot?.Content is RootLayout rootLayout)
            {
                _rootLayout = rootLayout;
                _rootLayout.SizeChanged += OnWindowSizeChanged;
            }
            _dragBarWindow = new DragBarWindow(_xamlWindow);
            UpdateDragBarWindowPos(frameworkElement);
        }

        private void DestoryDragBarWindow()
        {
            if (_dragBarWindow != null)
            {
                var handle = new HWND(_dragBarWindow.Handle);
                _ = _dragBarWindow.Dispatcher.RunIdleAsync(_ => PInvoke.DestroyWindow(handle));
                _dragBarWindow = null;
            }
            if (_rootLayout != null)
            {
                var rootLayout = _rootLayout;
                _ = _rootLayout.Dispatcher.RunIdleAsync(_ => rootLayout.SizeChanged -= OnWindowSizeChanged);
                _rootLayout = null;
            }
            _xamlWindow = null;
        }

        private unsafe void UpdateDragBarWindowPos(FrameworkElement frameworkElement)
        {
            if (frameworkElement != null && _rootLayout != null && VisualTreeHelper.GetParent(_rootLayout) is FrameworkElement border)
            {
                var ptXamlSource = new System.Drawing.Point(0, 0);
                PInvoke.ClientToScreen(new(_xamlWindow.XamlSourceHandle), &ptXamlSource);
                PInvoke.ScreenToClient(new(_xamlWindow.Handle), &ptXamlSource);

                var offsetX = ptXamlSource.X / _xamlWindow.ScalingFactor;
                var offsetY = ptXamlSource.Y / _xamlWindow.ScalingFactor;
                var size = frameworkElement.ActualSize;
                var rect = frameworkElement.TransformToVisual(border).TransformBounds(new(offsetX, offsetY, size.X, size.Y));

                _dragBarWindow?.SetRect(rect);
            }
        }

        public void Dispose()
        {
            DestoryDragBarWindow();
            if (_frameworkElementRef.TryGetTarget(out var frameworkElement))
            {
                frameworkElement.Loaded -= OnLoaded;
                frameworkElement.Unloaded -= OnUnloaded;
                frameworkElement.SizeChanged -= OnSizeChanged;
                frameworkElement.LayoutUpdated -= OnLayoutUpdated;
            }
        }
    }
}

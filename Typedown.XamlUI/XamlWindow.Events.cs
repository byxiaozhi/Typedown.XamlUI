using System;
using Windows.UI.Xaml;

namespace Typedown.XamlUI
{
    public partial class XamlWindow
    {
        public event EventHandler<ActualThemeChangedEventArgs> ActualThemeChanged;

        public event EventHandler<IsActiveChangedEventArgs> IsActiveChanged;

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public event EventHandler<ResizeModeChangedEventArgs> ResizeModeChanged;

        public event EventHandler Loaded;

        public event EventHandler Unloaded;
    }

    public class ActualThemeChangedEventArgs : EventArgs
    {
        public ElementTheme NewActualTheme { get; }

        public ActualThemeChangedEventArgs(ElementTheme newActualTheme)
        {
            NewActualTheme = newActualTheme;
        }
    }

    public class IsActiveChangedEventArgs : EventArgs
    {
        public bool NewIsActive { get; }

        public IsActiveChangedEventArgs(bool newIsActive)
        {
            NewIsActive = newIsActive;
        }
    }

    public class StateChangedEventArgs : EventArgs
    {
        public WindowState NewWindowState { get; }

        public StateChangedEventArgs(WindowState newWindowState)
        {
            NewWindowState = newWindowState;
        }
    }

    public class ResizeModeChangedEventArgs : EventArgs
    {
        public WindowResizeMode NewWindowResizeMode { get; }

        public ResizeModeChangedEventArgs(WindowResizeMode newWindowResizeMode)
        {
            NewWindowResizeMode = newWindowResizeMode;
        }
    }
}

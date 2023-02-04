using System;
using Windows.UI.Xaml;

namespace Typedown.XamlUI
{
    public partial class XamlApplication
    {
        internal event EventHandler<RequestedThemeChangedEventArgs> RequestedThemeChanged;
    }

    public class RequestedThemeChangedEventArgs : EventArgs
    {
        public ElementTheme NewRequestedTheme { get; }

        public RequestedThemeChangedEventArgs(ElementTheme newRequestedTheme)
        {
            NewRequestedTheme = newRequestedTheme;
        }
    }
}

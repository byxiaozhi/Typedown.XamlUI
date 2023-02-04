using Windows.UI.Xaml;

namespace Typedown.XamlUI
{
    public partial class XamlApplication
    {
        private ElementTheme _requestedTheme;

        public ElementTheme RequestedTheme
        {
            get
            {
                return _requestedTheme;
            }
            set
            {
                if (_requestedTheme != value)
                {
                    _requestedTheme = value;
                    RequestedThemeChanged?.Invoke(this, new(_requestedTheme));
                }
            }
        }
    }
}

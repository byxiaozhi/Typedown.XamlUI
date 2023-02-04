using Windows.UI;

namespace Typedown.XamlUI
{
    internal static class ColorExtensions
    {
        public static bool IsColorLight(this Color color)
        {
            return ((5 * color.G) + (2 * color.R) + color.B) > (8 * 128);
        }
    }
}

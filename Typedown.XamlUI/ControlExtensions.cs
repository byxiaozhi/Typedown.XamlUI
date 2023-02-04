using System.Reflection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Typedown.XamlUI
{
    internal static class ControlExtensions
    {
        public static DependencyObject GetTemplateChild(this Control control, string childName)
        {
            var method = typeof(Control).GetMethod("GetTemplateChild", BindingFlags.NonPublic | BindingFlags.Instance);
            return method.Invoke(control, new object[] { childName }) as DependencyObject;
        }
    }
}

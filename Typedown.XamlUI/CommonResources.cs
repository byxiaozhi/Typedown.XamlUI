using Windows.UI.Xaml;

namespace Typedown.XamlUI
{
    internal class CommonResources : ResourceDictionary
    {
        public CommonResources()
        {
            var assemblyName = typeof(CommonResources).Assembly.GetName().Name;
            MergedDictionaries.Add(new() { Source = new($"ms-appx:///{assemblyName}/Resources/RootLayout_themeresources.xaml") });
            MergedDictionaries.Add(new() { Source = new($"ms-appx:///{assemblyName}/Resources/ComboBox_themeresources.xaml") });
            MergedDictionaries.Add(new() { Source = new($"ms-appx:///{assemblyName}/Resources/ContentDialog_themeresources.xaml") });
            MergedDictionaries.Add(new() { Source = new($"ms-appx:///{assemblyName}/Resources/CaptionControl_themeresources.xaml") });
        }
    }
}

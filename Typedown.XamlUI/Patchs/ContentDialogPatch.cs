using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Typedown.XamlUI.Patchs
{
    internal class ContentDialogPatch
    {
        public static readonly DependencyProperty ApplyProperty = DependencyProperty.RegisterAttached("Apply", typeof(bool), typeof(ContentDialogPatch), new(false, OnDependencyPropertyChanged));
        public static bool GetApply(ContentDialog target) => (bool)target.GetValue(ApplyProperty);
        public static void SetApply(ContentDialog target, bool value) => target.SetValue(ApplyProperty, value);

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = d as ContentDialog;
            UpdateTheme(target);
            if ((bool)e.NewValue && !(bool)e.OldValue)
                target.Opened += OnContentDialogOpened;
            else
                target.Opened -= OnContentDialogOpened;
        }

        private static void OnContentDialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            UpdateTheme(sender);
        }

        private static void UpdateTheme(ContentDialog dialog)
        {
            if (dialog?.XamlRoot?.Content is FrameworkElement rootLayout)
                dialog.RequestedTheme = rootLayout.ActualTheme;
        }
    }
}

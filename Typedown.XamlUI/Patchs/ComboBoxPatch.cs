using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Typedown.XamlUI.Patchs
{
    internal class ComboBoxPatch
    {
        public static readonly DependencyProperty ApplyProperty = DependencyProperty.RegisterAttached("Apply", typeof(bool), typeof(ComboBoxPatch), new(false, OnDependencyPropertyChanged));
        public static bool GetApply(ComboBox target) => (bool)target.GetValue(ApplyProperty);
        public static void SetApply(ComboBox target, bool value) => target.SetValue(ApplyProperty, value);

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = d as ComboBox;
            if ((bool)e.NewValue && !(bool)e.OldValue)
                target.DropDownOpened += OnDropDownOpened;
            else
                target.DropDownOpened -= OnDropDownOpened;
        }

        private static void OnDropDownOpened(object sender, object e)
        {
            var target = sender as ComboBox;
            if (target.GetTemplateChild("PopupBorder") is Border popupBorder)
                popupBorder.RequestedTheme = target.ActualTheme;
        }
    }
}

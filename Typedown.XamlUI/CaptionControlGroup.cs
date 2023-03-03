using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Typedown.XamlUI
{
    public partial class CaptionControlGroup : Control
    {
        public static DependencyProperty IsMaximizedProperty { get; } = DependencyProperty.Register(nameof(IsMaximized), typeof(bool), typeof(CaptionControlGroup), null);

        public bool IsMaximized { get => (bool)GetValue(IsMaximizedProperty); set => SetValue(IsMaximizedProperty, value); }

        public static DependencyProperty IsActiveProperty { get; } = DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(CaptionControlGroup), new(true, OnDependencyPropertyChanged));

        public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }

        public Button MinButton { get; private set; }

        public Button MaxButton { get; private set; }

        public Button CloseButton { get; private set; }

        public event EventHandler MinimizeClick;

        public event EventHandler MaximizeOrRestoreClick;

        public event EventHandler CloseClick;

        internal CaptionControlGroup()
        {
            IsTabStop = false;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            MinButton = GetTemplateChild("MinButton") as Button;
            MaxButton = GetTemplateChild("MaxButton") as Button;
            CloseButton = GetTemplateChild("CloseButton") as Button;
            RegisterXamlEventHandlers();
        }

        private void RegisterXamlEventHandlers()
        {
            if (MinButton != null)
                MinButton.Click += OnMinButtonClick;
            if (MaxButton != null)
                MaxButton.Click += OnMaxButtonClick;
            if (CloseButton != null)
                CloseButton.Click += OnCloseButtonClick;
        }

        private void OnMinButtonClick(object sender, RoutedEventArgs e)
        {
            MinimizeClick?.Invoke(this, EventArgs.Empty);
        }

        private void OnMaxButtonClick(object sender, RoutedEventArgs e)
        {
            MaximizeOrRestoreClick?.Invoke(this, EventArgs.Empty);
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            CloseClick?.Invoke(this, EventArgs.Empty);
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = d as CaptionControlGroup;
            if (e.Property == IsActiveProperty)
            {
                VisualStateManager.GoToState(target, target.IsActive ? "Active" : "Deactive", true);
            }
        }
    }
}

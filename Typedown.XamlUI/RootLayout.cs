﻿using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Typedown.XamlUI
{
    [ContentProperty(Name = nameof(Content))]
    public class RootLayout : Grid
    {
        public static DependencyProperty ContentProperty { get; } = DependencyProperty.Register(nameof(Content), typeof(object), typeof(RootLayout), new(null, OnDependencyPropertyChanged));

        public object Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

        private readonly ContentPresenter _contentPresenter = new();

        public RootLayout()
        {
            Name = nameof(RootLayout);
            _contentPresenter.SizeChanged += OnContentSizeChanged;
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = d as RootLayout;
            if (e.Property == ContentProperty)
            {
                target._contentPresenter.Content = e.NewValue;
                if (!target.Children.Contains(target._contentPresenter))
                    target.Children.Insert(0, target._contentPresenter);
            }
        }

        private void OnContentSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            _contentPresenter.Clip ??= new();
            _contentPresenter.Clip.Rect = new(new(0, 0), e.NewSize);
        }

    }
}
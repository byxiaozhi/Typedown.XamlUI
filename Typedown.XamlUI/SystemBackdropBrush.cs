using Microsoft.Graphics.Canvas.Effects;
using System;
using Windows.Foundation.Metadata;
using Windows.System.Power;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Typedown.XamlUI
{
    public class SystemBackdropBrush : XamlCompositionBrushBase
    {
        private readonly XamlWindow _window;

        private static readonly bool _isSupported = ApiInformation.IsMethodPresent("Windows.UI.Composition.Compositor", "TryCreateBlurredWallpaperBackdropBrush");

        public SystemBackdropBrush(XamlWindow window)
        {
            _window = window;
        }

        private Color GetTintColor()
        {
            return _window.ActualTheme == ElementTheme.Dark ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);
        }

        private float GetTintOpacity()
        {
            return _window.ActualTheme == ElementTheme.Dark ? 0.8f : 0.5f;
        }

        private float GetLuminosityOpacity()
        {
            return 1;
        }

        private static CompositionBrush BuildMicaEffectBrush(Color tintColor, float tintOpacity, float luminosityOpacity)
        {
            if (!_isSupported)
            {
                return Windows.UI.Xaml.Window.Current.Compositor.CreateColorBrush(tintColor);
            }

            ColorSourceEffect tintColorEffect = new()
            {
                Name = "TintColor",
                Color = tintColor
            };

            OpacityEffect tintOpacityEffect = new()
            {
                Name = "TintOpacity",
                Opacity = tintOpacity,
                Source = tintColorEffect
            };

            ColorSourceEffect luminosityColorEffect = new()
            {
                Name = "LuminosityColor",
                Color = tintColor,
            };

            OpacityEffect luminosityOpacityEffect = new()
            {
                Name = "LuminosityOpacity",
                Opacity = luminosityOpacity,
                Source = luminosityColorEffect
            };

            BlendEffect luminosityBlendEffect = new()
            {
                Mode = BlendEffectMode.Color,
                Background = new CompositionEffectSourceParameter("BlurredWallpaperBackdrop"),
                Foreground = luminosityOpacityEffect,
            };

            BlendEffect colorBlendEffect = new()
            {
                Mode = BlendEffectMode.Luminosity,
                Background = luminosityBlendEffect,
                Foreground = tintOpacityEffect
            };

            CompositionEffectBrush micaEffectBrush = Windows.UI.Xaml.Window.Current.Compositor.CreateEffectFactory(colorBlendEffect, new string[] { }).CreateBrush();

            CompositionBackdropBrush blurredWallpaperBackdropBrush = Windows.UI.Xaml.Window.Current.Compositor.TryCreateBlurredWallpaperBackdropBrush();
            micaEffectBrush.SetSourceParameter("BlurredWallpaperBackdrop", blurredWallpaperBackdropBrush);

            return micaEffectBrush;
        }

        private static CompositionBrush CreateCrossFadeEffectBrush(CompositionBrush from, CompositionBrush to)
        {
            CrossFadeEffect crossFadeEffect = new()
            {
                Name = "Crossfade",
                Source1 = new CompositionEffectSourceParameter("Source1"),
                Source2 = new CompositionEffectSourceParameter("Source2"),
                CrossFade = 0
            };
            var crossFadeEffectBrush = Windows.UI.Xaml.Window.Current.Compositor.CreateEffectFactory(crossFadeEffect, new[] { "Crossfade.CrossFade" }).CreateBrush();
            crossFadeEffectBrush.Comment = "Crossfade";
            crossFadeEffectBrush.SetSourceParameter("Source1", to);
            crossFadeEffectBrush.SetSourceParameter("Source2", from);
            return crossFadeEffectBrush;
        }

        private static ScalarKeyFrameAnimation CreateCrossFadeAnimation()
        {
            var animation = Windows.UI.Xaml.Window.Current.Compositor.CreateScalarKeyFrameAnimation();
            var linearEasing = Windows.UI.Xaml.Window.Current.Compositor.CreateLinearEasingFunction();
            animation.InsertKeyFrame(0.0f, 0.0f, linearEasing);
            animation.InsertKeyFrame(1.0f, 1.0f, linearEasing);
            animation.Duration = TimeSpan.FromMilliseconds(250);
            return animation;
        }

        protected override void OnConnected()
        {
            if (CompositionBrush != null)
                return;

            _window.IsActiveChanged += OnWindowIsActiveChanged;
            _window.ActualThemeChanged += OnWindowActualThemeChanged;
            PowerManager.EnergySaverStatusChanged += OnEnergySaverStatusChanged;

            UpdateState();
        }

        protected override void OnDisconnected()
        {
            if (CompositionBrush != null)
            {
                CompositionBrush.Dispose();
                CompositionBrush = null;
            }

            _window.IsActiveChanged -= OnWindowIsActiveChanged;
            _window.ActualThemeChanged -= OnWindowActualThemeChanged;
            PowerManager.EnergySaverStatusChanged -= OnEnergySaverStatusChanged;
        }

        private void OnWindowIsActiveChanged(object sender, IsActiveChangedEventArgs e)
        {
            UpdateState();
        }

        private void OnWindowActualThemeChanged(object sender, ActualThemeChangedEventArgs e)
        {
            UpdateState();
        }

        private async void OnEnergySaverStatusChanged(object sender, object e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateState());
        }

        private bool prevIsActive = false;

        private ElementTheme prevTheme = ElementTheme.Default;

        private void UpdateState()
        {
            var active = _isSupported && _window.IsActive && PowerManager.EnergySaverStatus != EnergySaverStatus.On;
            var theme = _window.ActualTheme;
            if (CompositionBrush != null && prevIsActive == active && prevTheme == theme)
                return;
            prevIsActive = active;
            prevTheme = theme;

            var nextEffectBrush = active ? BuildMicaEffectBrush(GetTintColor(), GetTintOpacity(), GetLuminosityOpacity()) : Windows.UI.Xaml.Window.Current.Compositor.CreateColorBrush(GetTintColor());

            if (CompositionBrush == null)
            {
                CompositionBrush = nextEffectBrush;
                return;
            }

            if (CompositionBrush.Comment == "Crossfade")
                CompositionBrush.StopAnimation("Crossfade.CrossFade");
            var crossFadeEffectBrush = CreateCrossFadeEffectBrush(CompositionBrush, nextEffectBrush);
            CompositionBrush = crossFadeEffectBrush;

            CompositionScopedBatch scopedBatch = Windows.UI.Xaml.Window.Current.Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            var crossFadeAnimation = CreateCrossFadeAnimation();
            crossFadeEffectBrush.StartAnimation("Crossfade.CrossFade", crossFadeAnimation);
            scopedBatch.Completed += (s, a) =>
            {
                if (CompositionBrush == crossFadeEffectBrush)
                    CompositionBrush = nextEffectBrush;
            };
            scopedBatch.End();
        }
    }
}

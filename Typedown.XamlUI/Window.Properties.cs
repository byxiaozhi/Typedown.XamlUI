using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Typedown.XamlUI
{
    public partial class Window
    {
        public static DependencyProperty TitleProperty { get; } = DependencyProperty.Register(nameof(Title), typeof(string), typeof(Window), new(string.Empty, OnDependencyPropertyChanged));

        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        public static DependencyProperty LeftProperty { get; } = DependencyProperty.Register(nameof(Left), typeof(double), typeof(Window), new(double.NaN, OnDependencyPropertyChanged));

        public double Left { get => (double)GetValue(LeftProperty); set => SetValue(LeftProperty, value); }

        public static DependencyProperty TopProperty { get; } = DependencyProperty.Register(nameof(Top), typeof(double), typeof(Window), new(double.NaN, OnDependencyPropertyChanged));

        public double Top { get => (double)GetValue(TopProperty); set => SetValue(TopProperty, value); }

        public static DependencyProperty WidthProperty { get; } = DependencyProperty.Register(nameof(Width), typeof(double), typeof(Window), new(double.NaN, OnDependencyPropertyChanged));

        public double Width { get => (double)GetValue(WidthProperty); set => SetValue(WidthProperty, value); }

        public static DependencyProperty HeightProperty { get; } = DependencyProperty.Register(nameof(Height), typeof(double), typeof(Window), new(double.NaN, OnDependencyPropertyChanged));

        public double Height { get => (double)GetValue(HeightProperty); set => SetValue(HeightProperty, value); }

        public static DependencyProperty OwnerProperty { get; } = DependencyProperty.Register(nameof(Owner), typeof(nint), typeof(Window), new(default(nint), OnDependencyPropertyChanged));

        public nint Owner { get => (nint)GetValue(OwnerProperty); set => SetValue(OwnerProperty, value); }

        public static List<Window> AllWindows => _instances.Values.ToList();

        public nint Handle => _hWnd.Value;

        public uint DisplayDpi { get; private set; }

        public double ScalingFactor => DisplayDpi / 96d;

        private bool _propertiesUpdating = false;

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = d as Window;

            if (!target._propertiesUpdating && target._hWnd != HWND.Null)
            {
                if (e.Property == TitleProperty)
                {
                    PInvoke.SetWindowText(target._hWnd, target.Title);
                }
                else if (e.Property == LeftProperty || e.Property == TopProperty)
                {
                    target.SetLocation(new(target.Left, target.Top));
                }
                else if (e.Property == WidthProperty || e.Property == HeightProperty)
                {
                    target.SetSize(new(target.Width, target.Height));
                }
                else if (e.Property == OwnerProperty)
                {
                    target.SetOwner(target.Owner);
                }
            }

            if (!target._propertiesUpdating && target._hWnd == HWND.Null)
            {
                if (e.Property == LeftProperty || e.Property == TopProperty)
                {
                    target.LocationChanged?.Invoke(target, new(new(target.Left, target.Top)));
                }
                else if (e.Property == WidthProperty || e.Property == HeightProperty)
                {
                    target.SizeChanged?.Invoke(target, new(new(target.Width, target.Height)));
                }
            }

        }

        private void UpdateDependencyProperty(uint msg, nuint wParam, nint lParam)
        {
            _propertiesUpdating = true;
            switch (msg)
            {
                case PInvoke.WM_DPICHANGED:
                    {
                        static ushort HighWord(uint value) => (ushort)((value >> 16) & 0xFFFF);
                        DisplayDpi = HighWord((uint)wParam);
                        PInvoke.EnumChildWindows(new(_hWnd), (child, _) =>
                        {
                            if (_instances.TryGetValue(child, out var instance))
                                instance.DisplayDpi = DisplayDpi;
                            return true;
                        }, 0);
                        DpiChanged?.Invoke(this, new(DisplayDpi));
                        break;
                    }
                case PInvoke.WM_SETTEXT:
                    {
                        Title = Marshal.PtrToStringAuto(lParam);
                        break;
                    }
                case PInvoke.WM_WINDOWPOSCHANGED:
                    {
                        var pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                        if ((int)Math.Round(Left * ScalingFactor) != pos.x)
                            Left = pos.x / ScalingFactor;
                        if ((int)Math.Round(Top * ScalingFactor) != pos.y)
                            Top = pos.y / ScalingFactor;
                        if ((int)Math.Round(Width * ScalingFactor) != pos.cx)
                            Width = pos.cx / ScalingFactor;
                        if ((int)Math.Round(Height * ScalingFactor) != pos.cy)
                            Height = pos.cy / ScalingFactor;
                    }
                    break;
                case PInvoke.WM_MOVE:
                    LocationChanged?.Invoke(this, new(new(Left, Top)));
                    break;
                case PInvoke.WM_SIZE:
                    SizeChanged?.Invoke(this, new(new(Width, Height)));
                    break;
            }
            _propertiesUpdating = false;
        }
    }
}

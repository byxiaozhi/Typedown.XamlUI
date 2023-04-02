using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.Win32;

namespace Typedown.XamlUI
{
    public partial class XamlWindow
    {
        private unsafe void ApplyPatch(uint msg, nuint wParam, nint lParam)
        {
            switch (msg)
            {
                case PInvoke.WM_MOVE:
                    UpdatePopupLocation();
                    break;
                case PInvoke.WM_SIZE:
                    var sizeBytes = BitConverter.GetBytes(lParam);
                    var newWidth = BitConverter.ToUInt16(sizeBytes, 0) / ScalingFactor;
                    var newHeight = BitConverter.ToUInt16(sizeBytes, 2) / ScalingFactor;
                    UpdateContentDialogSize(newWidth, newHeight);
                    break;
                case PInvoke.WM_NCLBUTTONDOWN:
                    CloseMenuFlyout();
                    break;
            }
        }

        private void UpdatePopupLocation()
        {
            foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(_rootLayout.XamlRoot))
            {
                popup.InvalidateMeasure();
            }
        }

        private void UpdateContentDialogSize(double width, double height)
        {
            foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(_rootLayout.XamlRoot))
            {
                if (popup.Child is ContentDialog dialog)
                {
                    var smoke = dialog.GetTemplateChild("SmokeLayerBackground");
                    if (smoke is Rectangle rectangle)
                    {
                        rectangle.Width = width;
                        rectangle.Height = height;
                    }
                    dialog.Width = width;
                    dialog.Height = height;
                }
            }
        }

        private void CloseMenuFlyout()
        {
            foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(_rootLayout.XamlRoot))
                if (popup.Child is MenuFlyoutPresenter)
                    popup.IsOpen = false;
        }

    }
}

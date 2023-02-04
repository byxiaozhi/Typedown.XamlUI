using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace Typedown.XamlUI
{
    public partial class Window
    {
        public event EventHandler<ClosingEventArgs> Closing;

        public event EventHandler<ClosedEventArgs> Closed;

        public event EventHandler<LocationChangedEventArgs> LocationChanged;

        public event EventHandler<SizeChangedEventArgs> SizeChanged;

        public event EventHandler<DpiChangedEventArgs> DpiChanged;

        private IReadOnlyList<WndProc> _windowMessagehooks;

        public IDisposable AddWindowMessageHook(WndProc wndProc)
        {
            lock (_gate)
            {
                _windowMessagehooks ??= new List<WndProc>();
                _windowMessagehooks = _windowMessagehooks.Append(wndProc).ToList();
                return new ActionDisposable(() => RemoveWindowMessageHook(wndProc));
            }
        }

        public void RemoveWindowMessageHook(WndProc wndProc)
        {
            lock (_gate)
            {
                _windowMessagehooks = _windowMessagehooks.Where(x => !Equals(x, wndProc)).ToList();
            }
        }
    }

    public delegate nint WndProc(nint hWnd, uint msg, nuint lParam, nint wParam, ref bool handled);

    public class ClosingEventArgs : EventArgs
    {
        public bool Cancel { get; set; }
    }

    public class ClosedEventArgs : EventArgs
    {

    }

    public class LocationChangedEventArgs : EventArgs
    {
        public Point NewLocation { get; }

        public LocationChangedEventArgs(Point newLocation)
        {
            NewLocation = newLocation;
        }
    }

    public class SizeChangedEventArgs : EventArgs
    {
        public Size NewSize { get; }

        public SizeChangedEventArgs(Size newSize)
        {
            NewSize = newSize;
        }
    }

    public class DpiChangedEventArgs : EventArgs
    {
        public double NewDpi { get; }

        public DpiChangedEventArgs(double newDpi)
        {
            NewDpi = newDpi;
        }
    }
}

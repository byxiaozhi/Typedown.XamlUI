using System;

namespace Typedown.XamlUI
{
    internal class ActionDisposable : IDisposable
    {
        private Action _dispose;

        private readonly object _gate = new();

        public ActionDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_dispose != null)
                {
                    _dispose();
                    _dispose = null;
                }
            }
        }
    }
}

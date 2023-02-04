using System;
using System.Runtime.InteropServices;

namespace Typedown.XamlUI
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("3cbcf1bf-2f76-4e9c-96ab-e84b37972554")]
    partial interface IDesktopWindowXamlSourceNative
    {
        void AttachToWindow(IntPtr parentWnd);

        IntPtr WindowHandle { get; }
    }
}

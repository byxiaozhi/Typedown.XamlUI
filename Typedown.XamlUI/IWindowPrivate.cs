using System;
using System.Runtime.InteropServices;

namespace Typedown.XamlUI
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
    [Guid("06636C29-5A17-458D-8EA2-2422D997A922")]
    internal interface IWindowPrivate
    {
        bool TransparentBackground { get; set; }
    }
}

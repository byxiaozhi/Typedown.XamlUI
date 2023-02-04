using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32
{
    internal partial class PInvoke
    {
        [DllImport("User32", ExactSpelling = true, EntryPoint = "GetWindowLongW", SetLastError = true)]
        private static extern int GetWindowLong_x86(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex);

        [DllImport("User32", ExactSpelling = true, EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static extern nint GetWindowLongPtr_x64(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex);

        internal static nint GetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex)
        {
            return IntPtr.Size == 4 ? GetWindowLong_x86(hWnd, nIndex) : GetWindowLongPtr_x64(hWnd, nIndex);
        }

        [DllImport("User32", ExactSpelling = true, EntryPoint = "SetWindowLongW", SetLastError = true)]
        private static extern int SetWindowLong_x86(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, int dwNewLong);

        [DllImport("User32", ExactSpelling = true, EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr_x64(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, IntPtr dwNewLong);

        internal static nint SetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 4 ? SetWindowLong_x86(hWnd, nIndex, (int)dwNewLong) : SetWindowLongPtr_x64(hWnd, nIndex, dwNewLong);
        }
    }
}

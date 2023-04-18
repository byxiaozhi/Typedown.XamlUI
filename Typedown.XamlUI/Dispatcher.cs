using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Typedown.XamlUI
{
    public class Dispatcher
    {
        public static Dispatcher Current
        {
            get
            {
                var threadId = PInvoke.GetCurrentThreadId();
                if (_threadDispatchers.TryGetValue(threadId, out var value))
                    return value;
                return null;
            }
        }

        private static readonly ConcurrentDictionary<uint, Dispatcher> _threadDispatchers = new();

        private static readonly ConcurrentDictionary<HWND, Dispatcher> _hwndDispatchers = new();

        private static readonly WNDPROC _wndProc = new(StaticWndProc);

        private readonly ConcurrentDictionary<uint, Action> _tasks = new();

        private readonly SynchronizationContext _synchronizationContext;

        private DesktopWindowXamlSource _xamlSource;

        private uint _threadId = 0;

        private HWND _hWnd = HWND.Null;

        private Exception _exception;

        private const uint WM_TASK = PInvoke.WM_USER + 1;

        private uint curTaskId = 0;

        static unsafe Dispatcher()
        {
            fixed (char* pClassName = typeof(Dispatcher).FullName)
            {
                var wndClass = new WNDCLASSEXW();
                wndClass.cbSize = (uint)Marshal.SizeOf(wndClass);
                wndClass.lpfnWndProc = _wndProc;
                wndClass.hInstance = PInvoke.GetModuleHandle((char*)0);
                wndClass.lpszClassName = pClassName;
                PInvoke.RegisterClassEx(wndClass);
            }
        }

        internal Dispatcher()
        {
            _synchronizationContext = new DispatcherSynchronizationContext(this);
        }

        private unsafe void CreateMessageWindow()
        {
            if (!_hWnd.IsNull) return;
            _hWnd = PInvoke.CreateWindowEx(0, typeof(Dispatcher).FullName, typeof(Dispatcher).FullName, 0, 0, 0, 0, 0, HWND.Null, null, null, null);
            _hwndDispatchers.TryAdd(_hWnd, this);
            _xamlSource = new();
            var xamlSourceNative = (_xamlSource as IDesktopWindowXamlSourceNative);
            xamlSourceNative.AttachToWindow(_hWnd);
            PostTaskProcessMessage(null);
        }

        public void Start()
        {
            var threadId = PInvoke.GetCurrentThreadId();
            if (!_threadDispatchers.TryAdd(threadId, this))
                throw new InvalidOperationException("Dispatcher is already running");
            _threadId = threadId;
            try
            {
                CreateMessageWindow();
                MessageLoop();
            }
            finally
            {
                if (!_hWnd.IsNull) PInvoke.DestroyWindow(_hWnd);
                _threadDispatchers.TryRemove(_threadId, out _);
                _threadId = 0;
                if (_exception != null)
                    throw _exception;
            }
        }

        private unsafe void MessageLoop()
        {
            MSG msg;
            while (!_hWnd.IsNull && PInvoke.GetMessage(&msg, HWND.Null, 0, 0))
            {
                PInvoke.TranslateMessage(&msg);
                PInvoke.DispatchMessage(&msg);
            }
        }

        private void TaskProcess(uint taskId)
        {
            if (_tasks.TryRemove(taskId, out var action))
            {
                SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
                action();
            }
        }

        private unsafe void ClearTaskQueue()
        {
            MSG msg;
            while (PInvoke.PeekMessage(&msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
            {
                PInvoke.TranslateMessage(&msg);
                PInvoke.DispatchMessage(&msg);
                foreach (var taskId in _tasks.Keys)
                    TaskProcess(taskId);
            }
        }

        private static LRESULT StaticWndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            if (_hwndDispatchers.TryGetValue(hWnd, out var instance))
            {
                var result = instance.WndProc(hWnd, msg, wParam, lParam);
                if (msg == PInvoke.WM_DESTROY)
                {
                    _hwndDispatchers.TryRemove(hWnd, out var _);
                    instance._hWnd = HWND.Null;
                }
                return result;
            }
            return PInvoke.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private LRESULT WndProc(HWND hWnd, uint msg, nuint wParam, nint lParam)
        {
            if (msg == WM_TASK)
            {
                if (lParam != 0)
                {
                    foreach (var taskId in _tasks.Keys)
                        TaskProcess(taskId);
                }
                else
                {
                    TaskProcess((uint)wParam);
                }
            }
            return PInvoke.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private bool PostTaskProcessMessage(uint? taskId)
        {
            while (!_hWnd.IsNull && !PInvoke.PostMessage(_hWnd, WM_TASK, taskId ?? 0, taskId.HasValue ? 0 : 1))
                Thread.Sleep(10);
            return !_hWnd.IsNull;
        }

        internal void PostTask(Action action)
        {
            var taskId = curTaskId++;
            _tasks.TryAdd(taskId, action);
            PostTaskProcessMessage(taskId);
        }

        public Task<TResult> RunAsync<TResult>(Func<TResult> action)
        {
            var source = new TaskCompletionSource<TResult>();
            PostTask(() =>
            {
                try
                {
                    var result = action();
                    source.SetResult(result);
                }
                catch (Exception ex)
                {
                    source.SetException(ex);
                }
            });
            return source.Task;
        }

        public Task RunAsync(Action action)
        {
            var source = new TaskCompletionSource<object>();
            PostTask(() =>
            {
                try
                {
                    action();
                    source.SetResult(null);
                }
                catch (Exception ex)
                {
                    source.SetException(ex);
                }
            });
            return source.Task;
        }

        public Task ShutdownAsync(Exception exception = null)
        {
            if (!_hWnd.IsNull)
            {
                _exception = exception;
                var source = new TaskCompletionSource<bool>();
                PostTask(() =>
                {
                    ClearTaskQueue();
                    if (!_hWnd.IsNull)
                        PInvoke.DestroyWindow(_hWnd);
                    source.SetResult(true);
                });
                return source.Task;
            }
            return Task.CompletedTask;
        }
    }
}

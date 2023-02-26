using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
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
                if (_dispatchers.TryGetValue(threadId, out var value))
                    return value;
                return null;
            }
        }

        private static readonly ConcurrentDictionary<uint, Dispatcher> _dispatchers = new();

        private readonly BlockingCollection<Action> _taskQueue = new();

        private readonly SynchronizationContext _synchronizationContext;

        private uint _threadId = 0;

        private Exception _exception;

        internal Dispatcher()
        {
            _synchronizationContext = new DispatcherSynchronizationContext(this);
        }

        public unsafe void Start()
        {
            var threadId = PInvoke.GetCurrentThreadId();
            if (!_dispatchers.TryAdd(threadId, this))
                throw new InvalidOperationException("Dispatcher is already running");
            _threadId = threadId;
            try
            {
                PInvoke.PostThreadMessage(_threadId, 0, 0, 0);
                MessageLoop();
            }
            finally
            {
                _dispatchers.TryRemove(_threadId, out _);
                _threadId = 0;
                if (_exception != null)
                    throw _exception;
            }
        }

        private unsafe void MessageLoop()
        {
            MSG msg;
            while (PInvoke.GetMessage(&msg, HWND.Null, 0, 0))
            {
                MessageProcess(&msg);
            }
        }

        private unsafe void ClearTaskQueue()
        {
            MSG msg;
            while (PInvoke.PeekMessage(&msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
            {
                MessageProcess(&msg);
            }
        }

        private unsafe void MessageProcess(MSG* msg)
        {
            PInvoke.TranslateMessage(msg);
            PInvoke.DispatchMessage(msg);
            while (_taskQueue.TryTake(out var action))
            {
                SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
                action();
            }
        }

        internal async void PostTask(Action action)
        {
            if (GetWpfCurrentApplication() is object app)
            {
                await WpfInvokeAsync(app, action);
            }
            else
            {
                _taskQueue.Add(action);
                if (_threadId != 0)
                {
                    PInvoke.PostThreadMessage(_threadId, 0, 0, 0);
                }
            }
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
            if (_threadId != 0)
            {
                _exception = exception;
                var source = new TaskCompletionSource<bool>();
                PostTask(() =>
                {
                    ClearTaskQueue();
                    PInvoke.PostThreadMessage(_threadId, PInvoke.WM_QUIT, 0, 0);
                    source.SetResult(true);
                });
                return source.Task;
            }
            return Task.CompletedTask;
        }

        private object GetWpfCurrentApplication()
        {
            var type = Type.GetType("System.Windows.Application, PresentationFramework");
            var current = type?.GetProperty("Current")?.GetValue(null);
            return current;
        }

        private Task WpfInvokeAsync(object app, Action action)
        {
            var dispatcher = app?.GetType().GetProperty("Dispatcher")?.GetValue(app);
            var invokeAsync = dispatcher?.GetType().GetMethod("InvokeAsync", new Type[] { typeof(Action) });
            var operation = invokeAsync?.Invoke(dispatcher, new object[] { action });
            var task = operation?.GetType().GetProperty("Task")?.GetValue(operation) as Task;
            return task;
        }
    }
}

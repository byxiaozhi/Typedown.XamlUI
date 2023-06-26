using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.XamlTypeInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Typedown.XamlUI
{
    public partial class XamlApplication
    {
        private readonly WindowsApplication _application;

        private readonly HOOKPROC _hookProc;

        private readonly HHOOK _hHook;

        private readonly Dispatcher _dispatcher = new();

        private ResourceDictionary _resources;

        public ResourceDictionary Resources
        {
            get
            {
                return _resources;
            }
            set
            {
                _application.Resources.MergedDictionaries.Remove(_resources);
                _application.Resources.MergedDictionaries.Add(value);
                _resources = value;
            }
        }

        public static XamlApplication Current { get; private set; }

        public XamlApplication() : this(null) { }

        public XamlApplication(IEnumerable<IXamlMetadataProvider> providers = null)
        {
            if (Current != null)
                throw new InvalidOperationException();
            Current = this;
            _hookProc = new(CBTHookProc);
            _hHook = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_CBT, _hookProc, HINSTANCE.Null, PInvoke.GetCurrentThreadId());
            _application = new(providers);
            _resources = new();
            _application.Resources.MergedDictionaries.Add(_resources);
        }

        protected virtual void OnLaunched() { }

        public unsafe void Run()
        {
            if (Current != this)
                throw new InvalidOperationException();
            _ = _dispatcher.RunAsync(OnLaunched).ContinueWith(x =>
            {
                if (x.IsFaulted)
                    Exit(x.Exception.InnerException);
            });
            _dispatcher.Start();
        }

        public async void Exit(Exception exception = null)
        {
            if (Current != this)
                throw new InvalidOperationException();
            foreach (var window in Window.AllWindows.Where(x => x.Handle != 0))
                window.Destory();
            PInvoke.UnhookWindowsHookEx(_hHook);
            await _dispatcher.ShutdownAsync(exception);
            Current = null;
        }

        private LRESULT CBTHookProc(int code, WPARAM wParam, LPARAM lParam)
        {
            if (code == PInvoke.HCBT_CREATEWND)
            {
                var hWnd = new HWND((nint)wParam.Value);
                if (CoreWindowHelper.IsCoreWindow(hWnd))
                    CoreWindowHelper.SetCoreWindow(hWnd);
            }
            return PInvoke.CallNextHookEx(_hHook, code, wParam, lParam);
        }

        internal class WindowsApplication : Application, IXamlMetadataProvider
        {
            private readonly IReadOnlyList<IXamlMetadataProvider> _providers;

            public WindowsApplication(IEnumerable<IXamlMetadataProvider> providers)
            {
                providers ??= new List<IXamlMetadataProvider>();
                var presetProviders = new List<IXamlMetadataProvider> {
                    new XamlControlsXamlMetaDataProvider()
                };
                _providers = providers.Union(presetProviders).ToList();

                if (Windows.System.DispatcherQueue.GetForCurrentThread() is null)
                    Windows.UI.Xaml.Hosting.WindowsXamlManager.InitializeForCurrentThread();
                ((Windows.UI.Xaml.Window.Current as object) as IWindowPrivate).TransparentBackground = true;

                Resources.MergedDictionaries.Add(new XamlControlsResources());
                Resources.MergedDictionaries.Add(new CommonResources());
            }

            public IXamlType GetXamlType(Type type) => _providers.Select(x => x.GetXamlType(type)).Where(x => x != null).FirstOrDefault();

            public IXamlType GetXamlType(string fullName) => _providers.Select(x => x.GetXamlType(fullName)).Where(x => x != null).FirstOrDefault();

            public XmlnsDefinition[] GetXmlnsDefinitions() => _providers.SelectMany(x => x.GetXmlnsDefinitions()).ToArray();
        }
    }
}

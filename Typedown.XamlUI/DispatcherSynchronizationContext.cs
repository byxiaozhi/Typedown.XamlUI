using System.Threading;

namespace Typedown.XamlUI
{
    public class DispatcherSynchronizationContext : SynchronizationContext
    {
        private readonly Dispatcher dispatcher;

        public DispatcherSynchronizationContext(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            dispatcher.PostTask(() => d.Invoke(state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            dispatcher.RunAsync(() => d.Invoke(state)).Wait();
        }
    }
}

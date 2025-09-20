using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RnSArchipelago.Data
{
    internal class SubscriptionHandle: IDisposable
    {
        private readonly List<Action<DataContext, DataStateKey>> _subscribers;
        private readonly Action<DataContext, DataStateKey> _handler;
        private bool _isDisposed;

        internal SubscriptionHandle(List<Action<DataContext, DataStateKey>> subscribers, Action<DataContext, DataStateKey> handler)
        {
            _subscribers = subscribers;
            _handler = handler;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _subscribers.Remove(_handler);
                _isDisposed = true;
            }
        }
    }
}

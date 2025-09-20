using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RnSArchipelago.Data
{
    internal interface ISharedData
    {
        internal void SetValue<T>(DataContext context, DataStateKey key, T value);
        internal T? GetValue<T>(DataContext context, DataStateKey key);

        internal event Action<DataContext, DataStateKey>? OnChanged;

        internal IDisposable Subscribe(Action<DataContext, DataStateKey> handler);
        internal IDisposable SubscribeToContext(DataContext context, Action<DataContext, DataStateKey> handler);
        internal IDisposable SubscribeToContextAndKey(DataContext context, DataStateKey key, Action<DataContext, DataStateKey> handler);
    }
}

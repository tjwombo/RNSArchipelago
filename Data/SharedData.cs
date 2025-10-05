namespace RnSArchipelago.Data
{
    internal class SharedData: ISharedData
    {
        private readonly Dictionary<DataContext, Dictionary<DataStateKey, object>> _contextStates = new();
        private readonly object _lock = new();

        public event Action<DataContext, DataStateKey>? OnChanged;

        private readonly List<Action<DataContext, DataStateKey>> _subscribers = new();

        public void SetValue<T>(DataContext context, DataStateKey key, T value)
        {
            lock (_lock)
            {
                if (!_contextStates.TryGetValue(context, out var state))
                {
                    state = new Dictionary<DataStateKey, object>();
                    _contextStates[context] = state;
                }
                state[key] = value!;
            }

            OnChanged?.Invoke(context, key);

            foreach( var sub in _subscribers.ToArray())
            {
                sub(context, key);
            }
        }

        public T? GetValue<T>(DataContext context, DataStateKey key)
        {
            lock (_lock)
            {
                if (_contextStates.TryGetValue(context, out var state) &&
                    state.TryGetValue(key, out var value) && value is T typedValue)
                {
                    return typedValue;
                }
                return default;
            }
        }

        public IDisposable Subscribe(Action<DataContext, DataStateKey> handler)
        {
            _subscribers.Add(handler);
            return new SubscriptionHandle(_subscribers, handler);
        }

        public IDisposable SubscribeToContext(DataContext context, Action<DataContext, DataStateKey> handler)
        {
            Action<DataContext, DataStateKey> wrapped = (ctx, key) =>
            {
                if (ctx.Equals(context))
                {
                    handler(ctx, key);
                }
            };
            _subscribers.Add(wrapped);
            return new SubscriptionHandle(_subscribers, wrapped);
        }

        public IDisposable SubscribeToContextAndKey(DataContext context, DataStateKey key, Action<DataContext, DataStateKey> handler)
        {
            Action<DataContext, DataStateKey> wrapped = (ctx, k) =>
            {
                if (ctx.Equals(context) && k.Equals(key))
                {
                    handler(ctx, k);
                }
            };
            _subscribers.Add(wrapped);
            return new SubscriptionHandle(_subscribers, wrapped);
        }
    }
}

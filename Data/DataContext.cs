using System.Collections.Concurrent;

namespace RnSArchipelago.Data
{
    internal class DataContext
    {
        private readonly ConcurrentDictionary<DataStateKey, object> _entries;
        
        private readonly ConcurrentDictionary<DataStateKey, HashSet<IObserver<object>>> _observers;

        public DataContext()
        {
            _entries = new ConcurrentDictionary<DataStateKey, object>();    
            _observers = new ConcurrentDictionary<DataStateKey, HashSet<IObserver<object>>>();
        }

        public T? Get<T>(DataStateKey key)
        {
            if (this._entries.TryGetValue(key, out var value))
            {
                if (value is T t)
                    return t;
                else
                    throw new ArgumentException($"expected type ${typeof(T)}, found ${value.GetType()} for key ${key}");
            }
            else
                return default;
        }

        public void Set<T>(DataStateKey key, T value)
        {
            if (value is null)
            {
                this._entries.TryRemove(key, out _);
            }
            else
            {
                this._entries[key] = value;
            }

            if (this._observers.TryGetValue(key, out var set))
            {
                lock (set)
                {
                    foreach (var observer in set)
                    {
                        observer.OnNext((key, value));
                    }
                }
            }
        }

        public IObservable<T> Observe<T>(DataStateKey key)
        {
            return new KeyObservable<T>(this, key);
        }

        private record KeyObservable<T>(DataContext Context, DataStateKey Key) : IObservable<T>
        {
            public IDisposable Subscribe(IObserver<T> observer)
            {
                IObserver<object> fake =
                    new TypeTestObserver<object, T>(observer);
                HashSet<IObserver<object>> set = Context._observers.GetOrAdd(
                    Key,
                    (k) => []
                );
                lock (set)
                {
                    set.Add(fake);
                }
                return new KeySubscription(Context, Key, fake);
            }
        }

        private record TypeTestObserver<T, U>(IObserver<U> Inner) : IObserver<T>
        {
            public void OnCompleted()
            {
                Inner.OnCompleted();
            }

            public void OnError(Exception error)
            {
                Inner.OnError(error);
            }

            public void OnNext(T value)
            {
                if (value is U u)
                {
                    Inner.OnNext(u);
                }
                else if (value is null)
                {
                    throw new ArgumentException($"expected type ${typeof(T)}, found null");
                }
                else
                {
                    throw new ArgumentException($"expected type ${typeof(T)}, found ${value.GetType()}");
                }
            }
        }

        private record KeySubscription(DataContext Context, DataStateKey Key, IObserver<object> Fake)
            : IDisposable
        {
            public void Dispose()
            {
                var set = Context._observers[Key];
                lock (set)
                {
                    set.Remove(Fake);
                }
            }
        }
    }
}

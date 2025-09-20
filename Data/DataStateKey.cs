using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RnSArchipelago.Data
{
    internal readonly struct DataStateKey: IEquatable<DataStateKey>
    {
        private readonly string? _stringValue;
        private readonly long? _longValue;
        private readonly KeyType _type;

        private enum KeyType
        {
            None,
            String,
            Long
        }

        internal DataStateKey(string value)
        {
            _stringValue = value ?? throw new ArgumentNullException(nameof(value));
            _longValue = null;
            _type = KeyType.String;
        }

        internal DataStateKey(long value)
        {
            _stringValue = null;
            _longValue = value;
            _type = KeyType.Long;
        }

        public bool Equals(DataStateKey other)
        {
            if (_type != other._type)
            {
                return false;
            }

            switch(_type)
            {
                case KeyType.String:
                    return _stringValue == other._stringValue;
                case KeyType.Long:
                    return _longValue == other._longValue;
            }
            return false;
        }

        public override bool Equals(object? obj) => obj is DataStateKey other && Equals(other);

        public override int GetHashCode()
        {
            switch (_type)
            {
                case KeyType.String:
                    return _stringValue!.GetHashCode();
                case KeyType.Long:
                    return _longValue!.GetHashCode();
            }
            return 0;
        }

        public override string ToString()
        {
            switch (_type)
            {
                case KeyType.String:
                    return _stringValue!;
                case KeyType.Long:
                    return _longValue!.ToString()!;
            }
            return string.Empty;
        }

        public static implicit operator DataStateKey(string value) => new DataStateKey(value);
        public static implicit operator DataStateKey(long value) => new DataStateKey(value);
    }
}

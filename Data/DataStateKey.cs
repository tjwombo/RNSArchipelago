using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RnSArchipelago.Data
{
    internal abstract record DataStateKey
    {
        private DataStateKey() {}

        internal sealed record NoKey : DataStateKey;

        internal sealed record StringKey(string StringValue) : DataStateKey;

        internal sealed record LongKey(long LongValue) : DataStateKey;

        public static implicit operator DataStateKey(string value) => new StringKey(value);
        public static implicit operator DataStateKey(long value) => new LongKey(value);
    }
}

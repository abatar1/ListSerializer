using System.Runtime.CompilerServices;
using System.Threading;

namespace ListSerializer.Core
{
    public class UniqueIdGenerator
    {
        private long _counter;
        private readonly ConditionalWeakTable<ListNode, object> _ids = new ConditionalWeakTable<ListNode, object>();

        public long GetId(ListNode obj)
        {
            if (obj == null) return 0;
            return (long) _ids.GetValue(obj, _ => Interlocked.Increment(ref _counter));
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MyLib
{
    public class ActorSynchronizationContext : SynchronizationContext
    {
        private readonly SynchronizationContext _subContext;
        private readonly ConcurrentQueue<Action> _pending = new ConcurrentQueue<Action>();
        private int _pendingCount;

        public ActorSynchronizationContext(SynchronizationContext context = null)
        {
            this._subContext = context ?? new SynchronizationContext();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null) {
                throw new ArgumentNullException("SendOrPostCallback");
            }
            _pending.Enqueue(() => d(state));
            if (Interlocked.Increment(ref _pendingCount) == 1)
            {
                _subContext.Post(Consume, null); 
            }
        }

        private void Consume(object state)
        {
            var surroundContext = Current;
            try
            {
                SetSynchronizationContext(this);
                do
                {
                    Action a;
                    _pending.TryDequeue(out a);
                    a.Invoke();
                } while(Interlocked.Decrement(ref _pendingCount) > 0);
            } finally
            {
                SetSynchronizationContext(surroundContext);
            }
        }
        public override void Send(SendOrPostCallback d, object state)
        {
            throw new NotSupportedException();
        }
        public override SynchronizationContext CreateCopy()
        {
            return this;
        }
    }
}


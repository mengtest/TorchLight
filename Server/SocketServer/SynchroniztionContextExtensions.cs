using System;
using System.Threading;

namespace MyLib 
{
    public static class SynchroniztionContextExtensions
    {
        public static SynchronizationContextAwaiter GetAwaiter (this SynchronizationContext context) 
        {
            if(context == null) throw new ArgumentNullException("context");
            return new SynchronizationContextAwaiter(context);
        }
    }
}


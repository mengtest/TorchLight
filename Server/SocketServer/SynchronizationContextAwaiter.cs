using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MyLib 
{
    public sealed class SynchronizationContextAwaiter : INotifyCompletion
    {
        private readonly SynchronizationContext _context;
        public SynchronizationContextAwaiter(SynchronizationContext context)
        {
            if(context == null ) throw new ArgumentNullException("context");
            _context = context;
        }
        public bool IsCompleted {
            get {
                return false;
            }
        }

        /// <summary>
        /// 将Action 任务调度到 _context 控制的线程里面去执行
        /// 
        /// var temp = e.GetAwaiter();
        /// </summary>
        /// <param name="action">Action.</param>
        public void OnCompleted(Action action) {
            _context.Post(x=>action(), null);
        }
        public void GetResult(){}
    }
}


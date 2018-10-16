using System.Threading;

#if NET_2_0
namespace Core.Utils
{
    public struct SpinWait
    {
        internal const int YieldThreshold = 10; 
        private int _count;
        public void SpinOnce()
        {
            if (_count++ > YieldThreshold)
                Thread.Sleep(0);
            else
            {
                Thread.SpinWait(4 << _count);
            }
        }
    }
}

#endif
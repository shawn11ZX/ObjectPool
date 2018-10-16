using System.Diagnostics;
using System.Text;
using System.Threading;


namespace Core.ObjectPool
{

	public class RingBufferObjectAllocator : IObjectAllocator
	{
		

		public void PrintDebugInfo(StringBuilder sb)
		{
			sb.Append("<tr>");
			sb.Append("<td>");
			sb.Append(_factory);
			sb.Append("</td>");

			sb.Append("<td>");
			sb.Append(AllocCount);
			sb.Append("</td>");

			sb.Append("<td>");
			sb.Append(FreeCount);
			sb.Append("</td>");

			sb.Append("<td>");
			sb.Append(AllocCount - FreeCount);
			sb.Append("</td>");

			sb.Append("<td>");
			sb.Append(NewCount);
			sb.Append("</td>");

			sb.Append("<td>");
			sb.Append(_pool.Count);
			sb.Append("</td>");

			sb.Append("<td>");
			sb.Append(_pool.Capacity);
			sb.Append("</td>");



			sb.Append("</tr>");
		}

		private int _allocatorNumber;
		private int _freeNumber;
		private volatile RingBuffer<object> _pool = new RingBuffer<object>(100);

		private IObjectFactory _factory;

		public long NewCount;

		public long AllocCount;

		public long FreeCount;


		public RingBufferObjectAllocator(IObjectFactory factory)
		{
			_factory = factory;
		}

		public object Allocate()
		{
            Interlocked.Increment(ref AllocCount);

#if (NET_2_0)
            Core.Utils.SpinWait spin = new Core.Utils.SpinWait();
#else
            System.Threading.SpinWait spin = new System.Threading.SpinWait();
#endif
            while (Interlocked.Increment(ref _allocatorNumber) != 1)
			{
				Interlocked.Decrement(ref _allocatorNumber);
				spin.SpinOnce();
			}
		    object obj;
            var succ = _pool.TryDequeue(out obj);
			Interlocked.Decrement(ref _allocatorNumber);
			if (!succ || obj == null)
			{
				Interlocked.Increment(ref NewCount);
				obj = _factory.MakeObject();
			}

			_factory.ActivateObject(obj);
			return obj;
		}



		// there may be some leak while increasing cap
		// but it's ok...
		public void Free(object t)
		{
			if (t == null)
			{
				return;
			}


			_factory.DestroyObject(t);
			Interlocked.Increment(ref FreeCount);
			if (_pool.Count < _pool.Capacity - 5)
			{
#if NET_2_0
                Core.Utils.SpinWait spin = new Core.Utils.SpinWait();
#else
                System.Threading.SpinWait spin = new System.Threading.SpinWait();
                
#endif
                while (Interlocked.Increment(ref _freeNumber) != 1)
				{
					Interlocked.Decrement(ref _freeNumber);
					spin.SpinOnce();
				}
				_pool.Enqueue(t);
				Interlocked.Decrement(ref _freeNumber);

			}
			else
			{
				RingBuffer<object> old = _pool;
				_pool = new RingBuffer<object>(old.Capacity * 2);
				Debug.Print("ring buffer not big enough for object pool of type {0}", _factory.MakeObject().GetType());
			}
		}

	    public IObjectFactory Factory { get { return _factory; } set { _factory = value; } }
	}
}


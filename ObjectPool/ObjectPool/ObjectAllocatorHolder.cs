
namespace Core.ObjectPool
{
    public class ObjectAllocatorHolder<T> 
    {
        private static IObjectAllocator _allocator;
        
        static ObjectAllocatorHolder()
        {
            
            if (_allocator == null)
            {
                _allocator = ObjectAllocators.GetAllocator(typeof(T));
            }
            
        }

        public static T Allocate()
        {
            var rc = _allocator.Allocate();
            return (T)rc;
        }
        public static void Free(T t)
        {
            _allocator.Free(t);
        }

        
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Core.ObjectPool
{
    
    public class ObjectAllocators
    {
        static Dictionary<Type, IObjectAllocator> _allocatorDic = new Dictionary<Type, IObjectAllocator>();

        static ObjectAllocators()
        {
            GetAllocator(typeof(MemoryStream)).Factory = new MemoryStreamObjectFactory();
		}
        public static IObjectAllocator GetAllocator(Type type)
        {
            IObjectAllocator rc;
            if (!_allocatorDic.TryGetValue(type, out rc))
            {
                rc = new RingBufferObjectAllocator(new DefaultObjectFactory(type));
                _allocatorDic[type] = rc;
            }
            return rc;
        }

        public static void SetAllocator(Type type, IObjectAllocator allocator)
        {
            _allocatorDic[type] = allocator;
        }

        public static string PrintAllDebugInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<p>Object Pools</p>");
            sb.Append("<table width='400px' border='1' align='center' cellpadding='2' cellspacing='1'>");
            sb.Append("<thead>");
            sb.Append("<td>name</td>");
            sb.Append("<td>allocCount</td>");
            sb.Append("<td>freeCount</td>");
            sb.Append("<td>activeCount</td>");
            sb.Append("<td>newCount</td>");
            sb.Append("<td>poolSize</td>");
            sb.Append("<td>PoolCap</td>");
            sb.Append("</thead>");

            foreach (var allocator in _allocatorDic.Values)
            {
                allocator.PrintDebugInfo(sb);
            }
           
            sb.Append("</table>");
            return sb.ToString();
        }
    }
}
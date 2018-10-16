using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core.ObjectPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.ObjectPool.Tests
{
    [TestClass()]
    public class ObjectAllocatorTests
    {
        class Apple
        {
            
        }

        

        [TestMethod()]
        public void TestSimple()
        {
            Apple apple = ObjectAllocatorHolder<Apple>.Allocate();
            ObjectAllocatorHolder<Apple>.Free(apple);
        }




        class ReusableApple : IReusableObject
        {
            public bool eaten;
            public void ReInit()
            {
                eaten = false;
            }
        }

        [TestMethod()]
        public void TestReuseable()
        {
            List<ReusableApple> list = new List<ReusableApple>();
            for (int count = 0; count < 100; count++)
            {
                for (int i = 0; i < 100; i++)
                {
                    ReusableApple apple = ObjectAllocatorHolder<ReusableApple>.Allocate();
                    apple.eaten = true;
                    list.Add(apple);
                }

                foreach (var s in list)
                {
                    ObjectAllocatorHolder<ReusableApple>.Free(s);
                }
            }

            var o = ObjectAllocatorHolder<ReusableApple>.Allocate();
            Assert.IsFalse(o.eaten);
        }
    }
}
using Core.ObjectPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleProjectWithAnalyzer
{
    class Program
    {
        static int Main(string[] args)
        {
            TestSimpleObjectPool();
            TestReferenceCounteredObjectPool();
            return 0;
        }

        /*****************************************************************/

        class Apple
        {

        }
        
        static void TestSimpleObjectPool()
        {
            Apple apple = ObjectAllocatorHolder<Apple>.Allocate();
            ObjectAllocatorHolder<Apple>.Free(apple);
        }

        /*****************************************************************/

        class ReusableApple : IReusableObject
        {
            public bool eaten;
            public void ReInit() // will be called upon Free
            {
                eaten = false; 
            }
        }

        static void TestSimpleReusableObjectPool()
        {
            Apple apple = ObjectAllocatorHolder<Apple>.Allocate();
            ObjectAllocatorHolder<Apple>.Free(apple);
        }

        /*****************************************************************/
        class AppleWithRefCounter : BaseRefCounter
        {
            public static AppleWithRefCounter Alloc()
            {
                AppleWithRefCounter apple = ObjectAllocatorHolder<AppleWithRefCounter>.Allocate();
                return apple;
            }

            public bool eaten;
            protected override void OnCleanUp()
            {
                eaten = false;

                ObjectAllocatorHolder<AppleWithRefCounter>.Free(this);
            }
        }

        static void TestReferenceCounteredObjectPool()
        {
            AppleWithRefCounter apple = AppleWithRefCounter.Alloc();
            apple.ReleaseReference(); // compile will fail, if not release thanks to code analysis
        }


        /*****************************************************************/
        class AppleWithRefCounter2 : IRefCounter
        {
            RefCounter _refCounter;
            public static AppleWithRefCounter2 Alloc()
            {
                AppleWithRefCounter2 apple = ObjectAllocatorHolder<AppleWithRefCounter2>.Allocate();
                return apple;
            }

            public int RefCount {
                get
                {
                    return _refCounter.RefCount;
                }
            }

            protected void OnCleanUp()
            {
                eaten = false;

                ObjectAllocatorHolder<AppleWithRefCounter2>.Free(this);
            }

            public void AcquireReference()
            {
                _refCounter.AcquireReference();
            }

            public void ReleaseReference()
            {
                _refCounter.ReleaseReference();
            }

            public void ReInit()
            {
                _refCounter.ReInit();
            }

            public bool eaten;
        }

        static void TestReferenceCounteredObjectPool2()
        {
            AppleWithRefCounter2 apple = AppleWithRefCounter2.Alloc();
            apple.ReleaseReference(); // compile will fail, if not release thanks to code analysis
        }



    }
}

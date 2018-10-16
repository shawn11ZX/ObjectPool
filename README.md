## Preparation
- Add Reference to ObjectPool.dll
- (Optioanl) Add Reference to RefCounterAnalyzer.dll
- (Optioanl) Enable code analysis , see [How to: Enable and disable automatic code analysis for managed code](https://docs.microsoft.com/en-us/visualstudio/code-quality/how-to-enable-and-disable-automatic-code-analysis-for-managed-code?view=vs-2017)

## Usage

### 1. As simple ObjectPool
```csharp
        class Apple
        {

        }
        
        static void Main(string[] args)
        {
            Apple apple = ObjectAllocatorHolder<Apple>.Allocate();
            ObjectAllocatorHolder<Apple>.Free(apple);
        }
```

Alternatively, you may want do some cleanup when Free

```csharp
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
```

### 2. As Reference Countered ObjectPool

Extend ```BaseRefCounter``` and implement ```OnCleanUp```
```csharp
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
```

Alternatively, if you want to avoid class extension, implement ```IRefCounter``` and delegate to ```RefCounter``` instead.

```csharp
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
```
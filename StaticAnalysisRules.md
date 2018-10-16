
## Principles

If a reference counted (RCed) object a field member of another object, or it is contained in some collection filed of another object, we can that object the ‘**owner**‘.

In the following code snippets, A is owner of RcObj (if RcObj is reference counted)

```csharp
class A {
    RcObj _b;
}

class A {
    List<RcObj> _list;
}
```

With the  **owner**  defined, we can define the following rules:

-   Rule 1: The initial reference counter is 1 for newly allocated objects.
-   Rule 2: When owning a reference counted object, we should increase it’s reference counter.
-   Rule 3: When losing the ownership, we should decrease the reference counter.

We further define that:

-   Rule 4: When an owner’s  **method**  (not get property) returns RCed objects, it should increase RCs before returning them. In another words, it shared the ownership.
-   Rule 5: When an owner’s  **get property**  returns RCed objects, it should not increase RCs.

From the above 5 rules, we can infer that:

-   Inference 1: After getting a RCed object from a method, we should release it unless we return it to the caller or own it.

```csharp
class A {
    C c;
    List<RcObj> list;

    void Foo1() {
        RcObj obj = c.GetRcObj();
        obj.ReleaseReference(); // decrease reference counter
    }
    RcObj Foo2() {
        RcObj obj = c.GetRcObj();
        return obj;
        // no need to decrease reference counter
    }

    void Foo3() {
        RcObj obj = c.GetRcObj();
        list.Add(obj);
        // no need to decrease reference counter
    }

    RcObj Foo4() {
        RcObj obj = c.GetRcObj();
        list.Add(obj);
        obj.AcquireReference(); //increase reference counter
        return obj;
    }

}

class C {
    RcObj GetRcObj() {
        return new RcObj();
    }
    // or
    RcObj _obj;
    RcObj GetRcObj() {
        _obj.AcquireReference(); // increase RC before method return
        return _obj;
    }
}
```

-   Inference 2: : After getting a RCed object from a get property, we should not change its RC, unless we return it to the caller or own it.

```csharp
class A {
    C c;
    void Foo1() {
        RcObj obj = c.RcObj;
        Bar(obj);
        // don't change is RC in this method, (Bar may change RC)
    }

    RcObj Foo2() {
        RcObj obj = c.RcObj;
        obj.AcquireReference();
        // increase RC 
        return obj;
    }
}

class C {
    RcObj _obj;
    RcObj RcObj {
        get {
            return _obj;
        }
    }
}
```

-   Inference 3: We should not change RC of a RCed object passed in as method parameter , unless we want to own it.

```csharp
class A {
    void Foo1(RcObj obj) {
        obj.xxx();
        obj.yyy();
        // don't change RC
    }

    RcObj _obj;
    void Foo2(RcObj obj) {
        obj.xxx();
        obj.yyy();

        this._obj = obj;
        obj.AcquireReference(); // increase RC according to Rule 2
    }

    RcObj Foo3(RcObj obj) { // bad practice
        obj.xxx();
        obj.yyy();
        obj.AcquireReference(); // increase RC according to Rule 4
        return obj; 
    }
}
```

With these rules and inferences, we can write a static code analysis tool to help us reduce RC related bug.

----------

Basic Reference Counter interface:

```csharp
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount {get;}
    }
```

## Static Analysis Rules



### Method Invoking

| action | counter |
|--|--|
| calling new operator  | +1 |
|invoking methods that return RCobj (method name **NOT** start with Get, CreateAndGet  | +1 |
|invoking methods that return RCobj (method name **IS** start with Get, CreateAndGet  | 0 |
| pass as method argument | 0 |

```csharp
void F() {
    var s = new Snapshot(); // new operator
    s.ReleaseReference();
}
```

```csharp
void F() {
    var s = Snapshot.Allocate();// invoking non-Get method 
    s.ReleaseReference();
}
```


```csharp
void F() {
    var s = SnapshotPool.GetLatest();  // invoking Get method
}
```

```csharp
void F(ISnapshot s) { // <---- as method argument
    int a = s.Seq;
}
```

### Assignment 
| action | counter |
|--|--|
|invoking get property  | +0 |
|assigning to a field  | -1 |	
	
```csharp
void F() {
    var s = SnapshotPool.Latest;
}

```

```csharp

class A {
    ISnapshot _snapshot;
    void SetSnapshot(ISnapshot s)
    {
        _snapshot = s; // 复制给类字段
        s.AcquireReference();
    }
}
``` 

## return statement

| action | counter |
|--|--|
| return the RCobj (inside methods that **IS** start with Get, CreateAndGet) | 0 |
| return the RCobj (inside methods that is **NOT** start with Get, CreateAndGet) | -1 |

```csharp
class A {
    ISnapshot _snapshot;
    void GetSnapshot() // Method start with Get
    {
        return _snapshot; // <----
    }
}
```


```csharp
class A {
    ISnapshot _snapshot;
    void ReturnSnapshot() // <----
    {
        _snapshot.AcquireReference();
        return _snapshot; // <----
    }
}
```


### Collection

| action | counter |
|--|--|
|adding to a collection, and the collection is a local field | -1 | 


```csharp
System.Collections.IDictionary.Add
System.Collections.ICollection.Add
System.Collections.Generic.ICollection<T>.Add
System.Collections.Generic.Queue<T>.Enqueue
System.Collections.Queue.Enqueue
System.Collections.Generic.List<T>.Insert
Core.ObjectPool.IRefCounterContainer<T>.Add
Collection<T>.Add
LinkedList<T>.AddLast
LinkedList<T>.AddFirst
```


```csharp
class A {
    List<ISnapshot> _snapshotList;
    void SetSnapshot(ISnapshot s)
    {
        _snapshotList.Add(s); // <----
        s.AcquireReference();
    }
}
```

### Others 

| action | counter |
|--|--|
|passing as argument to any function whose name is ReleaseReference | -1 | 

```csharp
void F() {
    ISnapshot s = Snapshot.Allocate();
    ReleaseThread.Instance.ReleaseReference(s); // <----
}
ReleaseReference	-1	
void F() {
    var s = Snapshot.Allocate();
    s.ReleaseReference(); // <----
}
AcquireReference	+1	
void F() {
    var s = Snapshot.Allocate();
    s.AcquireReference(); // <----
    s.ReleaseReference();
    s.ReleaseReference();
}
```

### Suppress Error/Warning

```csharp
#pragma warning disable RefCounter001
var comp = GetGameEntity(entity);
#pragma warning restore RefCounter001
```

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using IDisposableAnalyzers;
using TestHelper;
using RefCounterAnalyzer;

namespace RefCounterAnalyzer.Test
{
	[TestClass]
	public class RefCounter001AnalyzerTest : CodeFixVerifier
	{

		//No diagnostics expected to show up
		[TestMethod]
		public void TestEmpty()
		{
			var test = @"";

			VerifyCSharpDiagnostic(test);
		}

	    //No diagnostics expected to show up
	    [TestMethod]
	    public void TestEmpty2()
	    {
	        var test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public class Allocator
	{
		public void Allocate()
		{
			Action<int> a = (data) => {};
		}
	}


}";

            VerifyCSharpDiagnostic(test);
	    }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
		public void TestInitialByNew()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public class Allocator
	{
		public void Allocate()
		{
			var a = new RefCounter();
		}
	}


}";
			var expected = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterErrorDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of a is 1",
				Locations =
					new[] {
							new DiagnosticResultLocation("Test0.cs", 28, 8)
						}
			};

			VerifyCSharpDiagnostic(test, expected);

			
			
		}


	    [TestMethod]
	    public void TestInitialByCast()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public class Allocator
	{
		public void Allocate(object o)
		{
			var a = (RefCounter) o;
		}

        public void Allocate2(object o)
		{
			var a = (RefCounter) o;
            object b = (object)o;
		}
	}


}";
	        var expected = new DiagnosticResult
	        {
	            Id = RefCounter001.RefCounterSkipDiagnosticId,
	            Severity = DiagnosticSeverity.Error,
	            Message = "Reference Counter of a not checked",
	            Locations =
	                new[] {
	                    new DiagnosticResultLocation("Test0.cs", 33, 8)
	                }
	        };

	        VerifyCSharpDiagnostic(test, expected);



	    }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
	        public void TestInitialByNew2()
	        {
	            string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public class Allocator
	{
		public void Allocate()
		{
			RefCounter a;
            int i;
            a = new RefCounter();
		}
	}


}";
	            var expected = new DiagnosticResult
	            {
	                Id = RefCounter001.RefCounterErrorDiagnosticId,
	                Severity = DiagnosticSeverity.Error,
	                Message = "Reference Counter of a is 1",
	                Locations =
	                    new[] {
	                        new DiagnosticResultLocation("Test0.cs", 28, 15)
	                    }
	            };

	            VerifyCSharpDiagnostic(test, expected);



	        }

	    //Diagnostic and CodeFix both triggered and checked for
	    [TestMethod]
	    public void TestInitialByOutKeyWorkd()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public class Allocator
	{
		public void Allocate()
		{
			RefCounter a;
            Foo(out a);
		}
        public void Allocate2()
		{
			RefCounter b = new RefCounter ();
            Foo(out b);
		}
        private System.Collections.Generic.Dictionary<int, RefCounter> _dic;
        public void Allocate3()
		{
			IRefCounter b;
            if (_dic.TryGetValue(1, out b) == false)
            {
            }
		}
        abstract void Foo(out RefCounter c);
	}


}";
	        var expected = new DiagnosticResult
	        {
	            Id = RefCounter001.RefCounterSkipDiagnosticId,
	            Severity = DiagnosticSeverity.Error,
	            Message = "Reference Counter of b not checked, ",
	            Locations =
	                new[] {
	                    new DiagnosticResultLocation("Test0.cs", 33, 15)
	                }
	        };

	        VerifyCSharpDiagnostic(test, expected);



	    }

        [TestMethod]
		public void TestAssignmentBetweenLocalVars()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public class Allocator
	{
		public void Allocate()
		{
			var a = new RefCounter();
			var b = a;
		}
	}


}";
			var expected1 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterErrorDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of a is 1",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 28, 8)
					}
			};

			var expected2 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterSkipDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of b not checked",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 29, 8)
					}
			};

			VerifyCSharpDiagnostic(test, expected1, expected2);



		}


		[TestMethod]
		public void TestInitialByPropertyGet()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		public void Allocate()
		{
			var a = Member;
		}

		public void Allocate()
		{
			var a = this.Member;
		}

		public abstract RefCounter Member {get;}
	}


}";
			
			VerifyCSharpDiagnostic(test);
		}

		[TestMethod]
		public void TestInitialByMethodInvoke()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		public void Allocate()
		{
			var a = Method();
		}

		public abstract RefCounter Method();
	}

    public abstract class Allocator2
	{
        Allocator allocate;
		public void Allocate()
		{
			var b = allocate.Method();
		}

	}



}";
			var expected1 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterErrorDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of a is 1",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 28, 8)
					}
			};

		    var expected2 = new DiagnosticResult
		    {
		        Id = RefCounter001.RefCounterErrorDiagnosticId,
		        Severity = DiagnosticSeverity.Error,
		        Message = "Reference Counter of b is 1",
		        Locations =
		            new[] {
		                new DiagnosticResultLocation("Test0.cs", 39, 8)
		            }
		    };

            VerifyCSharpDiagnostic(test, expected1, expected2);



		}

	    [TestMethod]
	    public void TestInitialByGetMethodInvoke()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		public void Allocate()
		{
			var a = GetMethod();
		}

		public abstract RefCounter GetMethod();
	}

 



}";
	       
	        VerifyCSharpDiagnostic(test);



	    }

	    [TestMethod]
	    public void TestInitialByGetMethodInvokeOtherClass()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		public abstract RefCounter GetMethod();
	}

    public abstract class Allocator2
	{
        Allocator _a;
		public void Allocate()
		{
			var a = _a.GetMethod();
		}

		
	}

 



}";

	        VerifyCSharpDiagnostic(test);



	    }

        [TestMethod]
	    public void TestInitialByDequeue()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	

    
    public abstract class Allocator2
	{
        private Queue _deserializeQueue = Queue.Synchronized(new Queue());    
		public void Allocate()
		{
			var b = (IRefCounter)_deserializeQueue.Dequeue();
		}

	}

}";
	        var expected1 = new DiagnosticResult
	        {
	            Id = RefCounter001.RefCounterErrorDiagnosticId,
	            Severity = DiagnosticSeverity.Error,
	            Message = "Reference Counter of b is 1",
	            Locations =
	                new[] {
	                    new DiagnosticResultLocation("Test0.cs", 32, 8)
	                }
	        };



	        VerifyCSharpDiagnostic(test, expected1);



	    }


        [TestMethod]
		public void TestSaveToField()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		private RefCounter _field;
		public void Allocate()
		{
			_field = Method();
		}

		public abstract RefCounter Method();
	}


}";
			

			VerifyCSharpDiagnostic(test);



		}





		[TestMethod]
		public void TestSaveToSystemCollection()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		private System.Collections.Generic.Dictionary<int, RefCounter> _dic;
		private System.Collections.Generic.Queue<RefCounter> _queue;
		private System.Collections.Queue _queue2;
		private System.Collections.Generic.List<RefCounter> _list;
		private RefCounter[] _array;

		

		public void AddToQueue()
		{
			//var a = new RefCounter();
			//_queue.Enqueue(a);
		}

		public void AddToQueue2()
		{
			var a = new RefCounter();
			_queue2.Enqueue(a);
		}

        public void AddToDic()
		{
			var a = new RefCounter();
			_dic.Add(1, a);
		}

		public void AddToDic2(RefCounter a)
		{
			a.AcquireReference();
			_dic.Add(1, a);
		}

		public void AddToList()
		{
			var a = new RefCounter();
			_list.Add(a);
		}

		public void AddToArray()
		{
			var a = new RefCounter();
			_array[0] = a;
		}
        
        public void AddToArray()
		{
			var a = new RefCounter();
			System.Collections.Generic.List<RefCounter> list;
            list.Add(a)
		}
	}


}";


			VerifyCSharpDiagnostic(test);


		}

		[TestMethod]
		public void TestSaveToSystemCollectionInLoop()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		private System.Collections.Generic.Dictionary<int, RefCounter> _dic;
		
		private System.Collections.Generic.List<RefCounter> _list;

	
        public void AddToDic()
		{
			var a = new RefCounter();
			foreach(var l in _list) {
				_dic.Add(1, a);
			}
		}

		public void AddToDic2()
		{
			
			foreach(var l in _list) {
				var a = new RefCounter();
				_dic.Add(1, a);
			}
		}

	}


}";

			var expected1 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterSkipDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of a not checked, for loop",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 33, 8)
					}
			};
			VerifyCSharpDiagnostic(test, expected1);

		}

		[TestMethod]
		public void TestSaveToSystemCollection2()
		{
			string test = @"
using System.Collections.Generic;
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public interface ISnapshot : IRefCounter {
		int SnapshotSeq { get; set; }
	}

	public abstract class Allocator
	{
		private Dictionary<int, ISnapshot> _sentSnapshot;
		
		
		public void SerializeSnapshot(ISnapshot snap, Stream stream)
        {
            snap.AcquireReference();
            _sentSnapshot.Add(snap.SnapshotSeq, snap);
		}
		public void AddToQueue(ISnapshot b)
		{
			_sentSnapshot.Add(1, b);
		}
	}

}";

			var expected1 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterErrorDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of b is -1",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 26, 36)
					}
			};
			VerifyCSharpDiagnostic(test, expected1);


		}


		[TestMethod]
		public void TestReturn()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		private RefCounter _field;
		public RefCounter Allocate()
		{
			var a = Method();
			return a;
		}

		public abstract RefCounter Method();
	}


}";

			VerifyCSharpDiagnostic(test);
		}

	    [TestMethod]
	    public void TestReturnFromGetMethod()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		private RefCounter _field;
		public RefCounter GetAllocate
		{
            get {
			    var a = Method();
			    return _field;
            }
		}

		public abstract RefCounter Method();
	}


}";
	        var expected1 = new DiagnosticResult
	        {
	            Id = RefCounter001.RefCounterErrorDiagnosticId,
	            Severity = DiagnosticSeverity.Error,
	            Message = "Reference Counter of a is 1",
	            Locations =
	                new[] {
	                    new DiagnosticResultLocation("Test0.cs", 30, 12)
	                }
	        };
            VerifyCSharpDiagnostic(test, expected1);
	    }

	    [TestMethod]
	    public void TestReturnFromGetProperty()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		private RefCounter _field;
		public RefCounter GetAllocate()
		{
			var a = Method();
			return a;
		}

		public abstract RefCounter Method();
	}


}";
	        var expected1 = new DiagnosticResult
	        {
	            Id = RefCounter001.RefCounterErrorDiagnosticId,
	            Severity = DiagnosticSeverity.Error,
	            Message = "Reference Counter of a is 1",
	            Locations =
	                new[] {
	                    new DiagnosticResultLocation("Test0.cs", 29, 8)
	                }
	        };
	        VerifyCSharpDiagnostic(test, expected1);
	    }


        [TestMethod]
		public void TestReturnPropertyGet()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		public void Allocate()
		{
			var a = Member;
		}

		public RefCounter Allocate()
		{
			var a = this.Member;
			return a;
		}

		public abstract RefCounter Member {get;}
	}


}";
			var expected1 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterErrorDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of a is -1",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 33, 8)
					}
			};
			VerifyCSharpDiagnostic(test, expected1);
		}


		[TestMethod]
		public void TestCallIncRef()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		public RefCounter Allocate()
		{
			var a = this.Member;
			a.AcquireReference();
			return a;
		}

		
		public abstract RefCounter Member {get;}
	}


}";
			VerifyCSharpDiagnostic(test);
		}

	    [TestMethod]
	    public void TestCallReleaseReference()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		public RefCounter Allocate()
		{
			var a = this.AllMember();
			C.ReleaseReference(a);
		}

		
		public abstract RefCounter AllMember();
	}

    public class C {
        public static ReleaseReference(IRefCounter r)
        {
            r.ReleaseReference();
        }
    }


}";
	        VerifyCSharpDiagnostic(test);
	    }


        [TestMethod]
		public void TestCallIncRefInLoop()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		public RefCounter Allocate()
		{
			var a = this.Member;
			while(true) {
				a.AcquireReference();
			}
			return a;
		}

		
		public abstract RefCounter Member {get;}
	}


}";
			var expected1 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterSkipDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of a not checked, for loop",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 28, 8)
					}
			};
			VerifyCSharpDiagnostic(test, expected1);
		}

		[TestMethod]
		public void TestCallDelRefInLoop()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		public RefCounter Allocate()
		{
			var a = this.Member;
			do {
				a.ReleaseReference();
			} while(true);
			return a;
		}

		
		public abstract RefCounter Member {get;}
	}


}";
			var expected1 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterSkipDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of a not checked, for loop",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 28, 8)
					}
			};
			VerifyCSharpDiagnostic(test, expected1);
		}

		[TestMethod]
		public void TestCallIncRefWithWrongId()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		public RefCounter Allocate()
		{
			var a = this.Member;
			var b = this.Member;
			b.AcquireReference();
			return b;
		}

		public RefCounter Allocate2()
		{
			var a = this.Member;
			var b = this.Member;
			a.AcquireReference();
			return b;
		}

        public void Allocate2()
		{
			var a = this.Member;
			a.AcquireReference();
            a.ReleaseReference();
		}

		public abstract RefCounter Member {get;}
	}


}";
			var expected1 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterErrorDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of a is 1",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 36, 8)
					}
			};
		    var expected2 = new DiagnosticResult
		    {
		        Id = RefCounter001.RefCounterErrorDiagnosticId,
		        Severity = DiagnosticSeverity.Error,
		        Message = "Reference Counter of b is -1",
		        Locations =
		            new[] {
		                new DiagnosticResultLocation("Test0.cs", 37, 8)
		            }
		    };
            VerifyCSharpDiagnostic(test, expected1, expected2);
		}

	    [TestMethod]
	    public void TestParameterNoUsage()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		private RefCounter _field;
		public void Allocate(RefCounter rc)
		{
		}

	}


}";

	        VerifyCSharpDiagnostic(test);
	    }

	    [TestMethod]
	    public void TestParameterAssignToField()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		private RefCounter _field;
		public void Allocate(RefCounter rc)
		{
            _field = rc;
		}

	}


}";
	        var expected2 = new DiagnosticResult
	        {
	            Id = RefCounter001.RefCounterErrorDiagnosticId,
	            Severity = DiagnosticSeverity.Error,
	            Message = "Reference Counter of rc is -1",
	            Locations =
	                new[] {
	                    new DiagnosticResultLocation("Test0.cs", 27, 35)
	                }
	        };
            VerifyCSharpDiagnostic(test, expected2);
	    }

	    [TestMethod]
	    public void TestParameterOfReleaseReference()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		public void ReleaseReference(RefCounter rc)
		{
            
		}

	}


}";
	        var expected2 = new DiagnosticResult
	        {
	            Id = RefCounter001.RefCounterErrorDiagnosticId,
	            Severity = DiagnosticSeverity.Error,
	            Message = "Reference Counter of rc is 1",
	            Locations =
	                new[] {
	                    new DiagnosticResultLocation("Test0.cs", 26, 43)
	                }
	        };
	        VerifyCSharpDiagnostic(test, expected2);
	    }


        [TestMethod]
		public void TestParameterAssignToFieldInLoop()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		private RefCounter _field;
		private System.Collections.Generic.List<RefCounter> _list;
		public void Allocate(RefCounter rc)
		{
			foreach(var l in _list) {
				_field = rc;
			}
		}

	}


}";
			var expected2 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterSkipDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of rc not checked, for loop",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 28, 35)
					}
			};
			VerifyCSharpDiagnostic(test, expected2);
		}

		[TestMethod]
	    public void TestParameterAssignToOtherField()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		private Allocator2 _allocat2;
		public void Allocate(RefCounter rc)
		{
            _allocat2._field2 = rc;
		}

	}

    public abstract class Allocator2
	{
		public RefCounter _field2;

	}


}";
			var expected2 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterSkipDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of rc not checked",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 27, 35)
					}
			};
			VerifyCSharpDiagnostic(test, expected2);
	    }

        [TestMethod]
	    public void TestParameterAssignToFieldWithIncRef()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		private RefCounter _field;
		public void Allocate(RefCounter rc)
		{
            _field = rc;
            rc.AcquireReference();
		}

        public void Allocate(RefCounter rc)
		{
            _field = rc;
            _field.AcquireReference();
		}
	}


}";
	        
	        VerifyCSharpDiagnostic(test);
	    }

	    [TestMethod]
	    public void TestParameterAssignToFieldWithIncRefByField()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		private RefCounter _field;
        private System.Collections.Generic.List<RefCounter> _list;


        public void Allocate3(RefCounter rc)
		{
            _list.Add(rc);
		}

		public void Allocate(RefCounter rc)
		{
            _field = rc;
            rc.AcquireReference();
		}
        public void Allocate2(RefCounter rc)
		{
            _list.Add(rc);
            rc.AcquireReference();
		}
	}


}";
	        var expected2 = new DiagnosticResult
	        {
	            Id = RefCounter001.RefCounterErrorDiagnosticId,
	            Severity = DiagnosticSeverity.Error,
	            Message = "Reference Counter of rc is -1",
	            Locations =
	                new[] {
	                    new DiagnosticResultLocation("Test0.cs", 30, 42)
	                }
	        };
            VerifyCSharpDiagnostic(test, expected2);
	    }

		[TestMethod]
		public void TestFieldAssignedAfterAssigned()
		{
			string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		public RefCounter _field2;
		public void Allocate(RefCounter rc)
		{
            _field2 = rc;
            var a = _field2;
		}

		public void Allocate(RefCounter rc)
		{
            _field2 = rc;
			RefCounter b = null;
            b = _field2;
		}
	}

  


}";
			var expected1 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterSkipDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of rc not checked",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 27, 35)
					}
			};

			var expected2 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterSkipDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of rc not checked",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 33, 35)
					}
			};

			var expected3 = new DiagnosticResult
			{
				Id = RefCounter001.RefCounterSkipDiagnosticId,
				Severity = DiagnosticSeverity.Error,
				Message = "Reference Counter of b not checked",
				Locations =
					new[] {
						new DiagnosticResultLocation("Test0.cs", 36, 15)
					}
			};
			VerifyCSharpDiagnostic(test, expected1, expected2, expected3);
		}

	    [TestMethod]
	    public void TestNotAssigned()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public abstract class Allocator
	{
		public void Allocate(RefCounter rc)
		{
           new RefCounter();
		}
        public abstract RefCounter Create();
		public void Allocate(RefCounter rc)
		{
           Create();
		}
	}

  


}";
	        var expected1 = new DiagnosticResult
	        {
	            Id = RefCounter001.RefCounterNotAssignedErrorId,
	            Severity = DiagnosticSeverity.Error,
	            Message = "return type of IRefCounter not used",
	            Locations =
	                new[] {
	                    new DiagnosticResultLocation("Test0.cs", 28, 12)
	                }
	        };

	        var expected2 = new DiagnosticResult
	        {
	            Id = RefCounter001.RefCounterNotAssignedErrorId,
	            Severity = DiagnosticSeverity.Error,
	            Message = "return type of IRefCounter not used",
	            Locations =
	                new[] {
	                    new DiagnosticResultLocation("Test0.cs", 33, 12)
	                }
	        };


            VerifyCSharpDiagnostic(test, expected1, expected2);
	    }

	    [TestMethod]
	    public void TestNotAssignedFromGet()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}
    public delegate void EntityRemoved(IRefCounter entity);
	public abstract class Allocator
	{
        public event EntityRemoved EntityAdded;
		private abstract IRefCounter GetGameEntity(int a);
        public abstract RefCounter GetBaseSnapshot();
		public void Allocate()
		{
            GetBaseSnapshot();
            this.GetBaseSnapshot();
            var b = 1;
            EntityRemoved(GetGameEntity((int)b));
		}
	}

  


}";
	       


	        VerifyCSharpDiagnostic(test);
	    }

        [TestMethod]
	    public void TestElementAccess()
	    {
	        string test = @"
    namespace Core.ObjectPool
{
    public interface IRefCounter
    {
        void AcquireReference();
        void ReleaseReference();
        int RefCount { get; }
    }

	public class RefCounter : IRefCounter
	{
		public void AcquireReference()
		{
		}

		public void ReleaseReference()
		{
		}

		public int RefCount { get; }
	}

	public class Allocator
	{
        private System.Collections.Generic.List<RefCounter> _list;
		public void Allocate()
		{
			var a = _list[0];
		}
	}


}";
	       

	        VerifyCSharpDiagnostic(test);



	    }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new RefCounter001();
		}
	}
}
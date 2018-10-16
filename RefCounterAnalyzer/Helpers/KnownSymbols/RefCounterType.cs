namespace IDisposableAnalyzers
{
    internal class RefCounterType : QualifiedType
    {
        internal readonly QualifiedMethod Dispose;

        internal RefCounterType()
            : base("Core.ObjectPool.IRefCounter")
        {
            this.Dispose = new QualifiedMethod(this, nameof(this.Dispose));
        }
    }
}
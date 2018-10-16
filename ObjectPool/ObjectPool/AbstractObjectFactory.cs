using System;

namespace Core.ObjectPool
{
    public abstract class AbstractObjectFactory : IObjectFactory
    {
        protected AbstractObjectFactory(Type type)
        {
            _isResetable = typeof(IReusableObject).IsAssignableFrom(type);
        }
        private bool _isResetable;

        public abstract object MakeObject();

        public virtual void ActivateObject(object arg0)
        {
            if (_isResetable)
            {
                (arg0 as IReusableObject).ReInit();
            }
         
        }

        public void DestroyObject(object arg0)
        {
        }
    }
}
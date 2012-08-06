using System;
using System.Reflection;

namespace Cwp.ObjectVisitor.TypeHandlers
{
    public class GenericTypeHandler<T, TContext> : ITypeHandler<TContext>
    {
        private readonly Action<TContext, MemberInfo, T> _writeFunction;
        private readonly Func<TContext, MemberInfo, T> _readerFunction;

        public GenericTypeHandler(Action<TContext, MemberInfo, T> writeFunction, Func<TContext, MemberInfo, T> readerFunction)
        {
            _writeFunction = writeFunction;
            _readerFunction = readerFunction;
            _readerFunction = readerFunction;
        }

        protected Action<TContext, MemberInfo, T> WriteFunction
        {
            get { return _writeFunction; }
        }

        protected Func<TContext, MemberInfo, T> ReaderFunction
        {
            get { return _readerFunction; }
        }

        public virtual void Write(object obj, TContext context, MemberInfo member, Type destinationType)
        {
            if (destinationType.IsNullable() && obj == null)
            {
                return;
            }
            WriteFunction(context, member, (T)obj);
        }

        public virtual object Read(TContext context,MemberInfo member, Type sourceType)
        {
            return ReaderFunction(context, member);
        }
    }
}
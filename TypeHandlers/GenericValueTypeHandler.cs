using System;
using System.Reflection;

namespace Cwp.ObjectVisitor.TypeHandlers
{
    public class GenericValueTypeHandler<T, TContext> : GenericTypeHandler<T, TContext>
    {
        private readonly Func<T, T, bool> _comparer;
        private readonly T _nullValue;

        public GenericValueTypeHandler(
            Action<TContext, MemberInfo, T> writeFunction,
            Func<TContext, MemberInfo, T> readerFunction,
            T nullValue)
            : this(writeFunction, readerFunction, nullValue, (v1, v2) => v1.Equals(v2))
        {
        }

        public GenericValueTypeHandler(
            Action<TContext, MemberInfo, T> writeFunction,
            Func<TContext, MemberInfo, T> readerFunction,
            T nullValue, 
            Func<T, T, bool> comparer)
            : base(writeFunction, readerFunction)
        {
            _nullValue = nullValue;
            _comparer = comparer;
        }

        public override void Write(object value, TContext context,MemberInfo member, Type destinationType)
        {
            if (destinationType.IsNullable() && value == null)
            {
                WriteFunction(context, member, _nullValue);
                return;
            }
            WriteFunction(context, member, (T)value);
        }

        public override object Read(TContext context, MemberInfo member, Type sourceType)
        {
            var result = ReaderFunction(context, member);
            if (sourceType.IsNullable() && _comparer.Invoke(result, _nullValue))
            {
                return null;
            }
            return result;
        }
    }
}
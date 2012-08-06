using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cwp.ObjectVisitor.TypeHandlers
{
    public interface IComplexAwareContext
    {
        void StartChild(MemberInfo member, Type destinationType);
        void EndChild(MemberInfo member, Type destinationType);
    }

    public class ComplexTypeHandler<T, TContext> : IComplexTypeHandler<TContext> where T : new() where TContext : IComplexAwareContext
    {
        private ObjectVisitor<T, TContext> _visitor;

        public ComplexTypeHandler(IDictionary<Type, ITypeHandler<TContext>> typeHandlers)
        {
            _visitor = new ObjectVisitor<T, TContext>(typeHandlers);
        }

        public void Write(object obj, TContext context, MemberInfo member, Type destinationType)
        {
            context.StartChild(member, destinationType);
            _visitor.Write(context, (T)obj);
            context.EndChild(member, destinationType);
        }

        public object Read(TContext context, MemberInfo member, Type sourceType)
        {
            return default(T);
        }
    }
}
using System;
using System.Reflection;

namespace Cwp.ObjectVisitor.TypeHandlers
{
    public interface ITypeHandler<TContext>
    {
        void Write(object obj, TContext context, MemberInfo member, Type destinationType);
        object Read(TContext context, MemberInfo member, Type sourceType);
    }

    public interface IComplexTypeHandler<TContext> : ITypeHandler<TContext>
    {
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Cwp.ObjectVisitor.TypeHandlers;

namespace Cwp.ObjectVisitor
{
    public class IgnoreMemberAttribute : Attribute
    {
    }

    /// <summary>
    /// Generic POF serializer to serialize and deserialize any object.
    /// </summary>
    /// <typeparam name="T">Type to serialize/deserialize.</typeparam>
    /// <remarks>
    /// The serialisation is based on public properties.
    /// For speed reasons, we dynamicly create (with Expressions) functions 
    /// to serialize and deserialize an object. This is done upon instantioation 
    /// of the serializer.  
    /// </remarks>
    public class ObjectVisitor<T, TContext> where T : new() 
    {
        private readonly Type _elementType;
        private Func<TContext, ITypeHandler<TContext>[], T> _readerFunc;
        private Action<TContext, T, ITypeHandler<TContext>[]> _writerFunc;
        private IDictionary<MemberInfo, ITypeHandler<TContext>> _propertySerializers;
        private readonly IDictionary<Type, ITypeHandler<TContext>> _typeHandlers =
            new Dictionary<Type, ITypeHandler<TContext>>();

        static ObjectVisitor()
        {
        }

        public ObjectVisitor()
        {
            _elementType = typeof(T);
        }

        public ObjectVisitor<T, TContext> WithTypeHandler<P>(ITypeHandler<TContext> handler)
        {
            _typeHandlers.Add(typeof(P), handler);
            return this;
        }

        protected Func<TContext, ITypeHandler<TContext>[], T> ReaderFunc
        {
            get
            {
                if (_readerFunc.IsNull())
                {
                    _readerFunc = CreateReader();
                }
                return _readerFunc;
            }
        }

        protected Action<TContext, T, ITypeHandler<TContext>[]> WriterFunc
        {
            get
            {
                if (_writerFunc.IsNull())
                {
                    _writerFunc = CreateWriter();
                }
                return _writerFunc;
            }
        }

        public IDictionary<MemberInfo, ITypeHandler<TContext>> PropertySerializers
        {
            get
            {
                if(_propertySerializers.IsNull())
                {
                    _propertySerializers = GetPropertySerializers();
                }
                return _propertySerializers;
            }
        }

        public virtual void Write(TContext writer, T o)
        {
            // call the dynamicly generated function to serialize the object into the cache
            WriterFunc(writer, o, PropertySerializers.Values.ToArray());
        }

        public virtual T Read(TContext reader)
        {
            // call the dynamicly generated function to deserialize the object from the cache
            var result = ReaderFunc(reader, PropertySerializers.Values.ToArray());
            return result;
        }

        /// <remarks>
        /// Based on the properties of the type, we create a dictionary to map 
        /// the properties to the required <see cref="ITypeHandler"/>s.
        /// </remarks>
        private IDictionary<MemberInfo, ITypeHandler<TContext>> GetPropertySerializers()
        {
            var serializedMembers = GetPropertiesToSerialize();
            return serializedMembers.ToDictionary(m => m, GetTypeHandler);;
        }

        /// <remarks>
        /// Get the properties we want to serialize.
        /// These are all the public read/write properties.
        /// </remarks>
        private IEnumerable<MemberInfo> GetPropertiesToSerialize()
        {
            var members = new List<MemberInfo>();
            members.AddRange(_elementType.GetProperties()
                                 .Where(prop => prop.CanRead && prop.CanWrite).ToArray());
            
            return (from mi in members
                    let ignoreAttribute = mi.GetCustomAttribute<IgnoreMemberAttribute>()
                    where ignoreAttribute == null
                    orderby mi.Name
                    select mi)
                .ToArray();
        }

        private ITypeHandler<TContext> GetTypeHandler(MemberInfo member)
        {
            var memberType = member.GetMemberType();
            if (memberType.IsNullable())
            {
                memberType = Nullable.GetUnderlyingType(memberType);
            }
            var handler = _typeHandlers.FirstOrDefault(h => h.Key == memberType);
            if (handler.IsNull())
            {
                throw new InvalidOperationException(
                    "Could not find a type handler that can handle type '{0}' for property '{1}'"
                        .FormatWith(memberType.Name, member.Name));
            }
            return handler.Value;
        }

        /// <remarks>
        /// This dynamilcy creates a delegate through expressions.
        /// What it creates is basically:
        /// (ITypeReader reader, ITypeHandler[] handlers) => 
        /// {
        ///     return new MyClass 
        ///     {
        ///         Property1 = handlers[0].Read(reader, 0, typeof(string)),
        ///         Property2 = handlers[1].Read(reader, 1, typeof(string)),
        ///         Property3 = handlers[2].Read(reader, 2, typeof(int)),
        ///         Property4 = handlers[3].Read(reader, 3, typeof(double))
        ///     };
        /// }
        /// 
        /// For the class:
        /// public class MyClass
        /// {
        ///     public string Property1 { get; set; }
        ///     public string Property2 { get; set; }
        ///     public int Property3 { get; set; }
        ///     public double Property4 { get; set; }
        /// }
        /// . 
        /// </remarks>
        private Func<TContext, ITypeHandler<TContext>[], T> CreateReader()
        {
            var context = Expression.Parameter(typeof(TContext), "context");
            var typeHandlers = Expression.Parameter(typeof(ITypeHandler<TContext>[]), "handlers");
            var initExpressions = new List<MemberBinding>();
            var readFunction = GetMethodInfo<ITypeHandler<TContext>>(x => x.Read);

            var counter = 0;
            foreach (var propertySerializer in PropertySerializers)
            {
                var call = Expression.Call(
                    Expression.ArrayIndex(typeHandlers, Expression.Constant(counter)),
                    readFunction,
                    context,
                    Expression.Constant(propertySerializer.Key),
                    Expression.Constant(propertySerializer.Key.GetMemberType()));

                var bind = Expression.Bind(
                    propertySerializer.Key,
                    Expression.Convert(call, propertySerializer.Key.GetMemberType()));
                initExpressions.Add(bind);

                counter++;
            }

            var readerFunction = Expression.Lambda<Func<TContext, ITypeHandler<TContext>[], T>>(
                Expression.MemberInit(Expression.New(typeof(T)), initExpressions),
                new[] { context, typeHandlers });
            
            return readerFunction.Compile();
        }

        /// <remarks>
        /// This dynamilcy creates a delegate through expressions.
        /// What it creates is basically:
        /// (ITypeWriter writer, ITypeHandler[] handlers, MyClass source) => 
        /// {
        ///     handlers[0].Write(source.Property1, writer, 0, typeof(string));
        ///     handlers[1].Write(source.Property2, writer, 1, typeof(string));
        ///     handlers[2].Write(source.Property3, writer, 2, typeof(single));
        ///     handlers[3].Write(source.Property4, writer, 3, typeof(double));
        /// }
        /// 
        /// For the class as described in the <see cref="CreateReader"/>.
        /// </remarks>
        private Action<TContext, T, ITypeHandler<TContext>[]> CreateWriter()
        {
            var context = Expression.Parameter(typeof(TContext), "context");
            var typeHandlers = Expression.Parameter(typeof(ITypeHandler<TContext>[]), "handlers");
            var obj = Expression.Parameter(typeof(T), "source");
            var writeExpressions = new List<Expression>();
            var writeFunction = GetMethodInfo<ITypeHandler<TContext>>(x => x.Write);

            int counter = 0;
            foreach (var propertySerializer in PropertySerializers)
            {
                var call = Expression.Call(
                    Expression.ArrayIndex(typeHandlers, Expression.Constant(counter)), 
                    writeFunction,
                    Expression.Convert(Expression.MakeMemberAccess(obj, propertySerializer.Key), typeof(object)),
                    context,
                    Expression.Constant(propertySerializer.Key),
                    Expression.Constant(propertySerializer.Key.GetMemberType()));

                writeExpressions.Add(call);
                counter++;
            }

            var parameters = new[] { context, obj, typeHandlers };
            var writerFuction = Expression.Lambda<Action<TContext, T, ITypeHandler<TContext>[]>>(
                Expression.Block(
                    writeExpressions),
                    parameters);

            return writerFuction.Compile();
        }

        private static MethodInfo GetMethodInfo<TIn>(Expression<Func<TIn, Func<TContext, MemberInfo, Type, object>>> function)
        {
            return ((ConstantExpression)
                    ((MethodCallExpression)
                     ((UnaryExpression)function.Body)
                         .Operand)
                        .Arguments[2]).Value as MethodInfo;
        }

        private static MethodInfo GetMethodInfo<TIn>(Expression<Func<TIn, Action<object, TContext, MemberInfo, Type>>> function)
        {
            return ((ConstantExpression)
                    ((MethodCallExpression)
                     ((UnaryExpression)function.Body)
                         .Operand)
                        .Arguments[2]).Value as MethodInfo;
        }
    }
}
using System;
using System.Reflection;

namespace Cwp.ObjectVisitor
{
    public static class ClrExtensions
    {
        public static bool IsNull(this object item)
        {
            return item == null;
        }

        public static string FormatWith(this string format, params object[] parameters)
        {
            return string.Format(format, parameters);
        }
    }
    
    internal static class TypeExtension
    {
        public static bool IsNullable(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        public static T GetCustomAttribute<T>(this Type type) where T : Attribute
        {
            return (T)Attribute.GetCustomAttribute(type, typeof(T));
        }

        public static bool HasCustomAttribute<T>(this Type type) where T : Attribute
        {
            return !GetCustomAttribute<T>(type).IsNull();
        }
    }

    internal static class MemberInfoExtension
    {
        public static Type GetMemberType(this MemberInfo info)
        {
            if (info is PropertyInfo)
            {
                return ((PropertyInfo)info).PropertyType;
            }
            return ((FieldInfo)info).FieldType;
        }

        public static T GetCustomAttribute<T>(this MemberInfo info) where T : Attribute
        {
            var attribs = info.GetCustomAttributes(typeof(T), true);
            if (attribs.Length == 0)
            {
                return null;
            }
            return (T)attribs[0];
        }
    }
}
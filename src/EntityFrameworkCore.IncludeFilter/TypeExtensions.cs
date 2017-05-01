using System;
using System.Reflection;

namespace EntityFrameworkCore.IncludeFilter
{
    public static class TypeExtensions
    {
        internal static bool IsNullableType(this Type type)
        {
            return type != null && type.GenericTypeArguments.Length > 0 && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        internal static Type GetNonNullableType(this Type type)
        {
            if (IsNullableType(type))
            {
                return type.GetGenericArguments()[0];
            }
            return type;
        }
    }
}

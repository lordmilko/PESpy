using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace PESpy
{
    internal static class ReflectionExtensions
    {
        private static BindingFlags allFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        public static PropertyInfo GetPropertyInfo(this Type type, string name)
        {
            var propertyInfo = type.GetProperty(name, allFlags);

            if (propertyInfo == null)
                throw new MissingMemberException(type.Name, name);

            return propertyInfo;
        }

        public static string GetDescription(this Enum element, bool toStringFallback = true)
        {
            if (TryGetDescription(element, out var description))
                return description;

            if (toStringFallback)
                return element.ToString();

            throw new InvalidOperationException($"{element} is missing a {nameof(DescriptionAttribute)}");
        }

        public static bool TryGetDescription(this Enum element, out string description)
        {
            var memberInfo = element.GetType().GetMember(element.ToString());

            if (memberInfo.Length > 0)
            {
                var attributes = memberInfo.First().GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attributes.Length > 0)
                {
                    description = ((DescriptionAttribute)attributes.First()).Description;
                    return true;
                }
            }

            description = null;
            return false;
        }
    }
}
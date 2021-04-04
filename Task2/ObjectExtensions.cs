using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Task2
{
    public static class ObjectExtensions
    {
        public static void SetReadOnlyProperty(this object obj, string propertyName, object newValue)
        {
            var property = obj.GetType().GetProperty(propertyName);
            var type = property.DeclaringType;
            var field = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                            .FirstOrDefault(field =>
                              field.Attributes.HasFlag(FieldAttributes.Private) &&
                              field.Attributes.HasFlag(FieldAttributes.InitOnly) &&
                              field.CustomAttributes.Any(attr => attr.AttributeType == typeof(CompilerGeneratedAttribute)) &&
                              (field.DeclaringType == property.DeclaringType) &&
                              field.FieldType.IsAssignableFrom(property.PropertyType) &&
                              field.Name.StartsWith("<" + property.Name + ">")
                            );
            field.SetValue(obj, newValue);
        }

        public static void SetReadOnlyField(this object obj, string filedName, object newValue)
        {
            FieldInfo fieldInfo = obj.GetType().GetField(filedName);
            fieldInfo.SetValue(obj, newValue);
        }
    }
}

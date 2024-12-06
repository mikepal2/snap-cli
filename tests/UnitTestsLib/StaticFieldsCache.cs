using System.Reflection;

namespace Tests
{
    public class StaticFieldsCache
    {
        private static bool IsInitialized = false;
        private static Dictionary<FieldInfo, object?> _fields = [];
        private static Dictionary<PropertyInfo, object?> _properties = [];
        public StaticFieldsCache()
        {
            if (IsInitialized)
                return;
            InitCache(GetType());
            foreach (var t in GetType().GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                InitCache(t);
            IsInitialized = true;
        }

        private void InitCache(Type type)
        {
            if (!type.IsClass)
                return;
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(f => f.IsStatic && !f.IsLiteral && !f.IsInitOnly))
                _fields.Add(field, field.GetValue(null));
            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(f => f.SetMethod?.IsStatic == true))
                _properties.Add(prop, prop.GetValue(null));
        }

        public void ResetValuesFromCache()
        {
            foreach (var field in _fields)
                field.Key.SetValue(null, field.Value);
            foreach (var prop in _properties)
                prop.Key.SetValue(null, prop.Value);
        }
    }


}

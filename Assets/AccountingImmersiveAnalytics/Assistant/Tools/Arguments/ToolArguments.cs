using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using AccountingImmersiveAnalytics.Assistant.Live;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Arguments
{
    public static class ToolArguments
    {
        private sealed class Field
        {
            public FieldInfo Info;
            public Type Type;
            public bool Optional;
            public string[] Allowed;
            public object Fallback;
        }

        private static readonly Dictionary<Type, Schema> _schemas = new Dictionary<Type, Schema>();
        private static readonly Dictionary<Type, Field[]> _fields = new Dictionary<Type, Field[]>();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCache()
        {
            _schemas.Clear();
            _fields.Clear();
        }

        public static Schema Schema(Type args)
        {
            if (args == null) return null;
            if (_schemas.TryGetValue(args, out var cached)) return cached;
            var built = Build(args);
            _schemas[args] = built;
            return built;
        }

        private static Field[] FieldsOf(Type args)
        {
            if (_fields.TryGetValue(args, out var cached)) return cached;
            FieldInfo[] infos = args.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var fields = new Field[infos.Length];
            for (int i = 0; i < infos.Length; i++)
            {
                FieldInfo f = infos[i];
                Type t = Nullable.GetUnderlyingType(f.FieldType) ?? f.FieldType;
                var values = f.GetCustomAttribute<ValuesAttribute>();
                var fallback = f.GetCustomAttribute<DefaultsToAttribute>();
                fields[i] = new Field
                {
                    Info = f,
                    Type = t,
                    Optional = f.GetCustomAttribute<OptionalAttribute>() != null,
                    Allowed = values != null ? values.Allowed : t.IsEnum ? EnumNames(t) : null,
                    Fallback = fallback != null ? Convert(fallback.Value, t) : null
                };
            }
            _fields[args] = fields;
            return fields;
        }

        private static Schema Build(Type args)
        {
            var properties = new Dictionary<string, Schema>();
            var required = new List<string>();

            foreach (Field field in FieldsOf(args))
            {
                FieldInfo f = field.Info;
                var schema = new Schema { Type = JsonType(field.Type) };
                if (IsList(field.Type)) schema.Items = new Schema { Type = JsonType(ElementType(field.Type)) };
                if (field.Allowed != null) schema.Enum = new List<string>(field.Allowed);

                var limits = f.GetCustomAttribute<LimitsAttribute>();
                if (limits != null) { schema.Minimum = limits.Min; schema.Maximum = limits.Max; }

                var fallback = f.GetCustomAttribute<DefaultsToAttribute>();
                if (fallback != null) schema.Default = fallback.Value;

                var doc = f.GetCustomAttribute<DocAttribute>();
                if (doc != null) schema.Description = doc.Text;
                properties[f.Name] = schema;
                if (!field.Optional) required.Add(f.Name);
            }

            return new Schema
            {
                Type = SchemaType.Object,
                Properties = properties,
                Required = required.Count > 0 ? required : null
            };
        }

        public static object Bind(Type args, Dictionary<string, object> values, out string error)
        {
            error = null;
            object instance = Activator.CreateInstance(args);

            foreach (Field field in FieldsOf(args))
            {
                object raw = null;
                bool present = values != null && values.TryGetValue(field.Info.Name, out raw) && raw != null;

                if (!present)
                {
                    if (!field.Optional)
                    {
                        error = $"Provide '{field.Info.Name}'.";
                        return null;
                    }
                    if (field.Fallback != null) field.Info.SetValue(instance, field.Fallback);
                    continue;
                }

                object bound;
                try
                {
                    bound = field.Allowed != null
                        ? Convert(Canonical(field.Allowed, LiveWire.Text(raw)), field.Type)
                        : Convert(raw, field.Type);
                }
                catch (Exception)
                {
                    error = Rejected(field);
                    return null;
                }
                field.Info.SetValue(instance, bound);
            }
            return instance;
        }

        private static string Canonical(string[] allowed, string bound)
        {
            string trimmed = bound.Trim();
            foreach (string value in allowed)
                if (string.Equals(value, trimmed, StringComparison.OrdinalIgnoreCase)) return value;
            throw new FormatException("not an allowed value");
        }

        private static string Rejected(Field field)
        {
            if (field.Allowed != null)
                return $"'{field.Info.Name}' must be one of: {string.Join(", ", field.Allowed)}.";
            return $"'{field.Info.Name}' is not a valid {JsonType(field.Type).ToString().ToLowerInvariant()}.";
        }

        private static bool IsList(Type t) =>
            t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>);

        private static Type ElementType(Type t) => t.GetGenericArguments()[0];

        private static string[] EnumNames(Type t)
        {
            string[] names = Enum.GetNames(t);
            for (int i = 0; i < names.Length; i++) names[i] = names[i].ToLowerInvariant();
            return names;
        }

        private static SchemaType JsonType(Type t)
        {
            if (t == typeof(int) || t == typeof(long)) return SchemaType.Integer;
            if (t == typeof(float) || t == typeof(double)) return SchemaType.Number;
            if (t == typeof(bool)) return SchemaType.Boolean;
            if (IsList(t)) return SchemaType.Array;
            return SchemaType.String;
        }

        private static object Convert(object value, Type target)
        {
            if (IsList(target))
            {
                Type element = ElementType(target);
                var list = (System.Collections.IList)Activator.CreateInstance(target);
                if (value is System.Collections.IEnumerable items && !(value is string))
                    foreach (var item in items) list.Add(Convert(item, element));
                else
                    list.Add(Convert(value, element));
                return list;
            }
            if (target.IsEnum) return Enum.Parse(target, LiveWire.Text(value), true);
            if (target == typeof(string)) return LiveWire.Text(value);
            return System.Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        }
    }
}

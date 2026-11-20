using System;
using System.Collections.Generic;
using System.Reflection;
using AccountingImmersiveAnalytics.Assistant.Live;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Arguments
{
    public static class ToolArguments
    {
        private static readonly Dictionary<Type, Schema> _cache = new Dictionary<Type, Schema>();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCache() => _cache.Clear();

        public static Schema Schema(Type args)
        {
            if (args == null) return null;
            if (_cache.TryGetValue(args, out var cached)) return cached;
            var built = Build(args);
            _cache[args] = built;
            return built;
        }

        private static Schema Build(Type args)
        {
            var properties = new Dictionary<string, Schema>();
            var required = new List<string>();

            foreach (FieldInfo f in Fields(args))
            {
                Type t = Underlying(f.FieldType);
                Schema schema;
                if (IsComplex(t)) schema = Build(t);
                else
                {
                    schema = new Schema { Type = JsonType(t) };
                    if (t.IsArray || IsList(t))
                    {
                        Type el = ElementType(t);
                        schema.Items = IsComplex(el) ? Build(el) : new Schema { Type = JsonType(el) };
                    }
                }

                var values = f.GetCustomAttribute<ValuesAttribute>();
                if (values != null) schema.Enum = new List<string>(values.Allowed);
                else if (t.IsEnum) schema.Enum = EnumNames(t);

                var limits = f.GetCustomAttribute<LimitsAttribute>();
                if (limits != null) { schema.Minimum = limits.Min; schema.Maximum = limits.Max; }

                var fallback = f.GetCustomAttribute<DefaultsToAttribute>();
                if (fallback != null) schema.Default = fallback.Value;

                var doc = f.GetCustomAttribute<DocAttribute>();
                if (doc != null) schema.Description = doc.Text;
                properties[f.Name] = schema;
                if (f.GetCustomAttribute<OptionalAttribute>() == null) required.Add(f.Name);
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

            foreach (FieldInfo f in Fields(args))
            {
                object raw = null;
                bool present = values != null && values.TryGetValue(f.Name, out raw) && raw != null;

                if (!present)
                {
                    if (f.GetCustomAttribute<OptionalAttribute>() == null)
                    {
                        error = $"Provide '{f.Name}'.";
                        return null;
                    }
                    continue;
                }

                object bound;
                try
                {
                    bound = Convert(raw, Underlying(f.FieldType));
                }
                catch (Exception)
                {
                    error = Rejected(f);
                    return null;
                }

                var allowed = f.GetCustomAttribute<ValuesAttribute>();
                if (allowed != null)
                {
                    if (!TryCanonical(allowed, bound as string, out string canonical))
                    {
                        error = Rejected(f);
                        return null;
                    }
                    bound = canonical;
                }
                f.SetValue(instance, bound);
            }
            return instance;
        }

        private static bool TryCanonical(ValuesAttribute values, string bound, out string canonical)
        {
            canonical = null;
            if (bound == null) return false;
            string trimmed = bound.Trim();
            for (int i = 0; i < values.Allowed.Length; i++)
                if (string.Equals(values.Allowed[i], trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    canonical = values.Allowed[i];
                    return true;
                }
            return false;
        }

        private static string Rejected(FieldInfo f)
        {
            Type t = Underlying(f.FieldType);
            var values = f.GetCustomAttribute<ValuesAttribute>();
            List<string> allowed = values != null ? new List<string>(values.Allowed)
                                 : t.IsEnum ? EnumNames(t)
                                 : null;
            if (allowed != null)
                return $"'{f.Name}' must be one of: {string.Join(", ", allowed)}.";

            if (IsComplex(t))
            {
                var names = new List<string>();
                foreach (FieldInfo nested in Fields(t)) names.Add(nested.Name);
                return $"'{f.Name}' must be an object with fields: {string.Join(", ", names)}. " +
                       "It is not a string; send it as a nested object.";
            }

            return $"'{f.Name}' is not a valid {JsonType(t).ToString().ToLowerInvariant()}.";
        }

        private static IEnumerable<FieldInfo> Fields(Type t) =>
            t.GetFields(BindingFlags.Public | BindingFlags.Instance);

        private static Type Underlying(Type t) => Nullable.GetUnderlyingType(t) ?? t;

        private static bool IsList(Type t) =>
            t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>);

        private static Type ElementType(Type t) =>
            t.IsArray ? t.GetElementType() : t.GetGenericArguments()[0];

        private static List<string> EnumNames(Type t)
        {
            string[] names = System.Enum.GetNames(t);
            var list = new List<string>(names.Length);
            for (int i = 0; i < names.Length; i++) list.Add(names[i].ToLowerInvariant());
            return list;
        }

        private static SchemaType JsonType(Type t)
        {
            if (t.IsEnum) return SchemaType.String;
            if (t == typeof(int) || t == typeof(long)) return SchemaType.Integer;
            if (t == typeof(float) || t == typeof(double)) return SchemaType.Number;
            if (t == typeof(bool)) return SchemaType.Boolean;
            if (t == typeof(string)) return SchemaType.String;
            if (t.IsArray || IsList(t)) return SchemaType.Array;
            return SchemaType.String;
        }

        private static bool IsComplex(Type t) =>
            t.IsClass && t != typeof(string) && !t.IsArray && !IsList(t);

        private static Dictionary<string, object> AsMap(object value)
        {
            var map = new Dictionary<string, object>();
            if (value is Dictionary<string, object> d)
            {
                foreach (var kv in d) map[kv.Key] = kv.Value;
            }
            return map;
        }

        private static object Convert(object value, Type target)
        {
            if (target.IsArray || IsList(target)) return ConvertList(value, target);

            if (IsComplex(target))
            {
                if (!(value is Dictionary<string, object>)) throw new InvalidOperationException("not an object");
                object bound = Bind(target, AsMap(value), out string error);
                if (bound == null) throw new InvalidOperationException(error ?? "bad object");
                return bound;
            }

            if (target.IsEnum)
            {
                return System.Enum.Parse(target, LiveWire.Text(value).Trim(), true);
            }

            if (target == typeof(string)) return LiveWire.Text(value);
            return System.Convert.ChangeType(value, target, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static object ConvertList(object value, Type target)
        {
            Type element = ElementType(target);
            var items = new List<object>();

            if (value is System.Collections.IEnumerable en && !(value is string))
            {
                foreach (var el in en) items.Add(Convert(el, element));
            }
            else
            {
                items.Add(Convert(value, element));
            }

            if (target.IsArray)
            {
                Array array = Array.CreateInstance(element, items.Count);
                for (int i = 0; i < items.Count; i++) array.SetValue(items[i], i);
                return array;
            }

            var list = (System.Collections.IList)Activator.CreateInstance(target);
            for (int i = 0; i < items.Count; i++) list.Add(items[i]);
            return list;
        }
    }
}

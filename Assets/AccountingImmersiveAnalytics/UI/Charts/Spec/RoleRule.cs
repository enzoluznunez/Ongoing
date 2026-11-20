namespace AccountingImmersiveAnalytics.UI.Charts.Spec
{
    public enum Role { Category, Group, Value, X, Y, Size, Item }

    public enum FieldType { Category, Number }

    public readonly struct RoleRule
    {
        public readonly Role Role;
        public readonly FieldType Type;
        public readonly bool Required;

        public RoleRule(Role role, FieldType type, bool required)
        {
            Role = role;
            Type = type;
            Required = required;
        }
    }
}

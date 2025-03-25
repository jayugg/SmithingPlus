using System;
using System.Reflection;

namespace SmithingPlus.Util;

public static class ReflectionExtensions
{
    public static T GetField<T>(this object obj, string fieldName)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        var fi = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (fi == null) return default;

        return (T)fi.GetValue(obj);
    }

    public static T GetInternalField<T>(this object obj, string fieldName)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        var fi = obj.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (fi == null) return default;

        return (T)fi.GetValue(obj);
    }

    public static void SetInternalField<T>(this object obj, string fieldName, T newValue)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        var fi = obj.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (fi == null) throw new InvalidOperationException($"Field '{fieldName}' not found.");

        fi.SetValue(obj, newValue);
    }
}
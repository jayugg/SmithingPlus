

using System.Reflection;
using Vintagestory.API.Datastructures;

namespace SmithingPlus.Util;

public class TreeAttributeSerializer<T>
{
    public static void ToTreeAttributes(T obj, ITreeAttribute tree)
    {
        foreach (PropertyInfo fi in typeof(T).GetProperties())
        {
            var type = fi.PropertyType;
            var name = fi.Name;
            if (type == typeof(bool)) {
                tree.SetBool(name,(bool)fi.GetValue(obj));
            } else if (type==typeof(int)) {
                tree.SetInt(name,(int)fi.GetValue(obj));
            } else if (type==typeof(float)) {
                tree.SetFloat(name,(float)fi.GetValue(obj));
            } else if (type==typeof(string)) {
                tree.SetString(name,(string)fi.GetValue(obj));
            }
        }
    }

    public static void FromTreeAttributes(T obj, ITreeAttribute tree)
    {
        foreach (PropertyInfo fi in typeof(T).GetProperties())
        {
            var type = fi.PropertyType;
            var name = fi.Name;
            if (type == typeof(bool)) {
                bool? v = tree.TryGetBool(name);
                if (v.HasValue) fi.SetValue(obj,v.Value);
            } else if (type==typeof(int)) {
                int? v = tree.TryGetInt(name);
                if (v.HasValue) fi.SetValue(obj,v.Value);
            } else if (type==typeof(float)) {
                float? v = tree.TryGetFloat(name);
                if (v.HasValue) fi.SetValue(obj,v.Value);
            } else if (type==typeof(string)) {
                if(tree.HasAttribute(name)) fi.SetValue(obj,tree.GetString(name));
            }
        }
    }
}

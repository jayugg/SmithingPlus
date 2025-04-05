using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace SmithingPlus.Util;

public static class TreeAttributeExtensions
{
    public static void SetVec3F(this ITreeAttribute tree, string code, Vec3f value)
    {
        tree.SetFloat(code + "X", value.X);
        tree.SetFloat(code + "Y", value.Y);
        tree.SetFloat(code + "Z", value.Z);
    }
    
    public static Vec3f GetVec3F(this ITreeAttribute tree, string code, Vec3f defaultValue = null)
    {
        IAttribute attribute;
        return !tree.TryGetAttribute(code + "X", out attribute) || attribute is not FloatAttribute floatAttribute ?
            defaultValue :
            new Vec3f(floatAttribute.value, tree.GetFloat(code + "Y"), tree.GetFloat(code + "Z"));
    }
    
    public static bool TryGetAttribute(this ITreeAttribute tree, string code, out IAttribute attribute)
    {
        if (tree == null || !tree.HasAttribute(code))
        {
            attribute = null;
            return false;
        }
        attribute = tree.GetTreeAttribute(code);
        return true;
    }
}
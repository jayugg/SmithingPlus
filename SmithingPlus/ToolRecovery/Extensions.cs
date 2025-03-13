using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus.ToolRecovery;

public static class Extensions
{
    internal static int? GetDurability(this ItemStack itemStack)
    {
        if (itemStack?.Attributes == null)
            return new int?();
        return itemStack.Attributes.HasAttribute("durability") ? itemStack.Attributes.GetInt("durability", itemStack.Item?.Durability ?? 1) : new int?();
    }

    internal static void SetDurability(this ItemStack itemStack, int number)
    {
        if (!itemStack.Attributes.HasAttribute("durability"))
            return;
        itemStack.Attributes.SetInt("durability", number);
    }
    
    internal static void CloneBrokenCount(this ItemStack itemStack, ItemStack fromStack, int extraCount = 0)
    {
        var brokenCount = fromStack.GetBrokenCount();
        itemStack.Attributes.SetInt("brokenCount", brokenCount + extraCount);
    }
    
    internal static void CloneAttributeFrom(this ItemStack itemStack, string attributeKey, ItemStack fromStack)
    {
        IAttribute attributeValue = null;
        itemStack.Attributes?.TryGetAttribute(attributeKey, out attributeValue);
        if (attributeValue == null) return;
        if (itemStack.Attributes != null)
            itemStack.Attributes[attributeKey] = fromStack.Attributes[attributeKey];
    }
    
    internal static void SetRepairedToolStack(this ItemStack itemStack, ItemStack fromStack)
    {
        itemStack.Attributes.SetItemstack("repairedToolStack", fromStack);
    }
    
    // Note: On server itemstack needs to be resolved!
    internal static ItemStack GetRepairedToolStack(this ItemStack itemStack)
    {
        return itemStack.Attributes?.GetItemstack("repairedToolStack");
    }
    
    internal static string GetRepairSmith(this ItemStack itemStack)
    {
        var repairedStack = itemStack.GetRepairedToolStack();
        return repairedStack?.GetRepairSmith() ?? itemStack.Attributes.GetString("repairSmith");
    }
    
    internal static void SetRepairSmith(this ItemStack itemStack, string smith)
    {
        itemStack.Attributes.SetString("repairSmith", smith);
    }
    
    internal static void CloneRepairedToolStack(this ItemStack itemStack, ItemStack fromStack, string[] forgettableAttributes = null)
    {
        var repairedStack = fromStack.GetRepairedToolStack();
        if (forgettableAttributes != null)
            foreach(var attributeKey in forgettableAttributes)
                repairedStack.Attributes?.RemoveAttribute(attributeKey);
        itemStack.SetRepairedToolStack(repairedStack);
            
    }

    internal static int GetBrokenCount(this ItemStack itemStack)
    {
        var repairedStack = itemStack.GetRepairedToolStack();
        return repairedStack?.GetBrokenCount() ?? (itemStack.Attributes?.GetInt("brokenCount") ?? 0);
    }
    
    public static Item ItemWithVariant(this Item item, string key, string value)
    {
        if (Core.Api != null) return Core.Api.World.GetItem(item.CodeWithVariant(key, value));
        Core.Logger.Error("Core.Api is null, call this extension method after the mod has started");
        return item;
    } 
    
    public static bool CodeMatches(this ItemStack stack, ItemStack that)
    {
        return stack.Collectible.Code.Equals(that.Collectible.Code);
    } 
    
    public static ItemStack GetBaseMaterial(this ItemStack stack)
    {
        return stack.Collectible is IAnvilWorkable workable ? workable.GetBaseMaterial(stack) : stack;
    }
    
    public static void AddBehavior<T>(this CollectibleObject collectible) where T : CollectibleBehavior
    {
        var existingBehavior = collectible.CollectibleBehaviors.FirstOrDefault(b => b.GetType() == typeof(T));
        collectible.CollectibleBehaviors.Remove(existingBehavior);
        var behavior = (T) Activator.CreateInstance(typeof(T), collectible);
        collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
    }
    
    public static string GetMetalOrMaterial(this CollectibleObject collObj)
    {
        return collObj.Variant["metal"] ?? collObj.Variant["material"];
    }
    
    public static string GetMetalMaterial(this CollectibleObject collObj, ICoreAPI api = null)
    {
        api ??= Core.Api;
        var ingotItem = api?.World.GetItem(new AssetLocation("game:ingot-" + collObj.GetMetalOrMaterial()));
        return ingotItem?.Variant["metal"] ?? ingotItem?.Variant["material"];
    }

    public static bool HasMetalMaterial(this CollectibleObject collObj, ICoreAPI api = null)
    {
        return collObj.GetMetalMaterial(api) != null;
    }
    
    /*
     Regex matching is slow.
     Only use when first assigning behaviors.
     On runtime, check for CollectibleBehaviorRepairableTool instead.
    */
    public static bool IsRepairableTool(this CollectibleObject collObj, bool verbose = false)
    {
        var repairable = WildcardUtil.Match(Core.Config.RepairableToolSelector, collObj.Code.ToString());
        if (verbose && !repairable) Core.Logger.VerboseDebug("Not a repairable tool: {0}", collObj.Code);
        return repairable;
    }
    
    // Same as above, use CollectibleBehaviorRepairableToolHead instead
    public static bool IsRepairableToolHead(this CollectibleObject collObj, bool verbose = false)
    {
        var repairable = WildcardUtil.Match(Core.Config.ToolHeadSelector, collObj.Code.ToString());
        if (verbose && !repairable) Core.Logger.VerboseDebug("Not a repairable tool head: {0}", collObj.Code);
        return repairable;
    }
    
    public static float GetWorkableTemperature(this ItemStack stack)
    {
        float meltingPoint = stack.Collectible.CombustibleProps?.MeltingPoint ?? 0.0f;
        var defaultTemperature = meltingPoint / 2f;
        return stack.ItemAttributes?["workableTemperature"]?.AsFloat(defaultTemperature) ?? defaultTemperature;
    }
    
    public static SmithingRecipe GetSmithingRecipe(this ItemStack toolHead, IWorldAccessor world)
    {
        var smithingRecipe = world.Api.ModLoader
            .GetModSystem<RecipeRegistrySystem>()
            .SmithingRecipes
            .FirstOrDefault(r => r.Output.ResolvedItemstack.Satisfies(toolHead));
        return smithingRecipe;
    }
    
    public static SmithingRecipe GetSmithingRecipe(this CollectibleObject collectible, IWorldAccessor world)
    {
        var smithingRecipe = world.Api.ModLoader
            .GetModSystem<RecipeRegistrySystem>()
            .SmithingRecipes
            .FirstOrDefault(r => r.Output.ResolvedItemstack.Collectible.Code.Equals(collectible.Code));
        return smithingRecipe;
    }
    
    public static float GetSplitCount(this ItemStack stack)
    {
        var splitCount = stack.TempAttributes.GetFloat("sp:splitCount");
        return splitCount;
    }
    
    public static void SetSplitCount(this ItemStack stack, float count)
    {
        stack.TempAttributes.SetFloat("sp:splitCount", count);
    }
}
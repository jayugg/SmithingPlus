using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus.Util;

public static class CollectibleExtensions
{
    public static void AddBehavior<T>(this CollectibleObject collectible) where T : CollectibleBehavior
    {
        var existingBehavior = collectible.CollectibleBehaviors.FirstOrDefault(b => b.GetType() == typeof(T));
        collectible.CollectibleBehaviors.Remove(existingBehavior);
        var behavior = (T) Activator.CreateInstance(typeof(T), collectible);
        collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
    }
    
    public static void AddBehaviorIf<T>(this CollectibleObject collectible, bool condition) where T : CollectibleBehavior
    {
        if (!condition) return;
        collectible.AddBehavior<T>();
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
    
    public static SmithingRecipe GetSmithingRecipe(this CollectibleObject collectible, IWorldAccessor world)
    {
        var smithingRecipe = world.Api.ModLoader
            .GetModSystem<RecipeRegistrySystem>()
            .SmithingRecipes
            .FirstOrDefault(r => r.Output.ResolvedItemstack.Collectible.Code.Equals(collectible.Code));
        return smithingRecipe;
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus.Util;

#nullable enable
public static class CollectibleExtensions
{
    private static void EnsureAttributesNotNull(this CollectibleObject obj)
    {
        obj.Attributes ??= new JsonObject(new JObject());
    }

    public static void MakeForgeable(this CollectibleObject collObj)
    {
        collObj.EnsureAttributesNotNull();
        var token = collObj.Attributes.Token;
        token["forgable"] = true;
        var transform = new ModelTransform
        {
            Translation = new Vec3f(0, -0.1f, 0.35f),
            Rotation = new Vec3f(0, 90f, 0),
            Scale = 0.7f
        };
        token["inForgeTransform"] = JToken.FromObject(transform);
        collObj.Attributes.Token = token;
    }

    public static void AddBehavior<T>(this CollectibleObject collectible) where T : CollectibleBehavior
    {
        var existingBehavior = collectible.CollectibleBehaviors.FirstOrDefault(b => b.GetType() == typeof(T));
        collectible.CollectibleBehaviors.Remove(existingBehavior);
        if (Activator.CreateInstance(typeof(T), collectible) is not T behavior)
        {
            Core.Logger.Error("[CollectibleExtensions] Failed to create behavior {0} for {1}", typeof(T).Name,
                collectible.Code);
            return;
        }

        collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
    }

    public static void AddBehaviorIf<T>(this CollectibleObject collectible, bool condition)
        where T : CollectibleBehavior
    {
        if (!condition) return;
        collectible.AddBehavior<T>();
    }

    public static bool IsRepairableTool(this CollectibleObject collObj, bool verbose = false)
    {
        var repairable = WildcardUtil.Match(Core.Config.RepairableToolSelector, collObj.Code.ToString());
        if (verbose && !repairable) Core.Logger.VerboseDebug("Not a repairable tool: {0}", collObj.Code);
        return repairable;
    }

    public static bool MatchesToolHeadSelector(this CollectibleObject collObj, bool verbose = false)
    {
        var repairable = WildcardUtil.Match(Core.Config.ToolHeadSelector, collObj.Code.ToString());
        if (verbose && !repairable) Core.Logger.VerboseDebug("Not a tool head: {0}", collObj.Code);
        return repairable;
    }

    public static SmithingRecipe? GetSmithingRecipe(this CollectibleObject collObj, ICoreAPI api)
    {
        var smithingRecipe = api.ModLoader
            .GetModSystem<RecipeRegistrySystem>()
            .SmithingRecipes
            .FirstOrDefault(r => r.Output.ResolvedItemstack.Collectible.Code.Equals(collObj.Code));
        return smithingRecipe;
    }

    public static IEnumerable<GridRecipe> GetGridRecipes(this CollectibleObject collObj, ICoreAPI api)
    {
        var gridRecipes =
            from recipe in api.World.GridRecipes
            where recipe.Output.ResolvedItemstack.Collectible.Code.Equals(collObj.Code)
            select recipe;
        return gridRecipes;
    }

    public static IEnumerable<SmithingRecipe> GetSmithingRecipesAsIngredient(this CollectibleObject collObj,
        ICoreAPI api)
    {
        var smithingRecipes =
            from recipe in api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes
            from ing in recipe.Ingredients
            where ing.ResolvedItemstack is not null &&
                  ing.ResolvedItemstack.Collectible.Code.Equals(collObj.Code)
            select recipe;
        return smithingRecipes;
    }

    public static IEnumerable<GridRecipe> GetGridRecipesAsIngredient(this CollectibleObject collObj, ICoreAPI api)
    {
        var gridRecipes =
            from recipe in api.World.GridRecipes
            where recipe.resolvedIngredients != null
            from ing in recipe.resolvedIngredients
            where ing is { ResolvedItemstack.Collectible: not null } &&
                  ing.ResolvedItemstack.Collectible.Code.Equals(collObj.Code)
            select recipe;
        return gridRecipes;
    }

    /*
     Regex matching is slow.
     Only use when first assigning behaviors.
     At runtime, check for CollectibleBehaviorRepairableTool instead.
    */

    // Same as above, check for CollectibleBehaviorRepairableToolHead or CollectibleBehaviorCastToolHead instead
}
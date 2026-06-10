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
    private static readonly JToken ForgeTransformToken = JToken.FromObject(
        new ModelTransform
        {
            Translation = new Vec3f(0, -0.1f, 0.35f),
            Rotation = new Vec3f(0, 90f, 0),
            Scale = 0.7f
        }
    );

    private static void EnsureAttributesNotNull(this CollectibleObject obj)
    {
        obj.Attributes ??= new JsonObject(new JObject());
    }

    public static void MakeForgeable(this CollectibleObject collObj)
    {
        collObj.EnsureAttributesNotNull();
        var token = collObj.Attributes.Token;
        token["forgable"] = true;
        token["inForgeTransform"] = ForgeTransformToken;
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
        var cache = Core.SmithingRecipesByOutputCode;
        if (cache.Count > 0)
        {
            cache.TryGetValue(collObj.Code, out var cached);
            return cached;
        }
        return api.ModLoader
            .GetModSystem<RecipeRegistrySystem>()
            .SmithingRecipes
            .FirstOrDefault(r => r.Output.ResolvedItemstack.Collectible.Code.Equals(collObj.Code));
    }

    public static IEnumerable<SmithingRecipe> GetSmithingRecipesAsIngredient(this CollectibleObject collObj,
        ICoreAPI api)
    {
        var cache = Core.SmithingRecipesByIngredientCode;
        if (cache.Count > 0)
        {
            return cache.TryGetValue(collObj.Code, out var cached)
                ? cached
                : System.Linq.Enumerable.Empty<SmithingRecipe>();
        }
        return
            from recipe in api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes
            from ing in recipe.Ingredients
            where ing.ResolvedItemStack is not null &&
                  ing.ResolvedItemStack.Collectible.Code.Equals(collObj.Code)
            select recipe;
    }

    public static IEnumerable<GridRecipe> GetGridRecipesAsIngredient(this CollectibleObject collObj, ICoreAPI api)
    {
        var cache = Core.GridRecipesByIngredientCode;
        if (cache.Count > 0)
        {
            return cache.TryGetValue(collObj.Code, out var cached)
                ? cached
                : System.Linq.Enumerable.Empty<GridRecipe>();
        }
        return
            from recipe in api.World.GridRecipes
            where recipe.RecipeIngredients != null
            from ing in recipe.RecipeIngredients
            where ing is { ResolvedItemStack.Collectible: not null } &&
                  ing.ResolvedItemStack.Collectible.Code.Equals(collObj.Code)
            select recipe;
    }

    public static CollectibleObject? CollectibleWithVariant(this CollectibleObject collObj, string type, string value)
    {
        var api = collObj.GetField<ICoreAPI>("api");
        if (api == null)
        {
            Core.Logger.Error("[CollectibleWithVariant] Reflection failed to get collectible object api field");
            return null;
        }

        var codeWithVariant = collObj.CodeWithVariant(type, value);
        switch (collObj.ItemClass)
        {
            case EnumItemClass.Block:
                return api.World.GetBlock(codeWithVariant);
            case EnumItemClass.Item:
                return api.World.GetItem(codeWithVariant);
            default:
                Core.Logger.Error(
                    $"[CollectibleWithVariant] Invalid ItemClass \"{collObj.ItemClass}\" for collectible {collObj.Code}");
                return null;
        }
    }

    public static T GetBehavior<T>(this CollectibleObject collObj, bool withInheritance) where T : CollectibleBehavior
    {
        return (T)collObj.GetCollectibleBehavior(typeof(T), withInheritance);
    }

    /*
     Regex matching is slow.
     Only use when first assigning behaviors.
     At runtime, check for CollectibleBehaviorRepairableTool instead.
    */

    // Same as above, check for CollectibleBehaviorRepairableToolHead or CollectibleBehaviorCastToolHead instead
}
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
        var byOutput = ObjectCacheUtil.GetOrCreate(api, $"{Core.ModId}:smithingRecipesByOutput", () =>
        {
            var dict = new Dictionary<AssetLocation, SmithingRecipe>();
            foreach (var recipe in api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes)
            {
                var code = recipe?.Output?.ResolvedItemstack?.Collectible?.Code;
                if (code != null) dict.TryAdd(code, recipe!);
            }

            return dict;
        });
        return byOutput.TryGetValue(collObj.Code, out var smithingRecipe) ? smithingRecipe : null;
    }

    public static IEnumerable<SmithingRecipe> GetSmithingRecipesAsIngredient(this CollectibleObject collObj,
        ICoreAPI api)
    {
        var byIngredient = ObjectCacheUtil.GetOrCreate(api, $"{Core.ModId}:smithingRecipesByIngredient", () =>
        {
            var dict = new Dictionary<AssetLocation, List<SmithingRecipe>>();
            foreach (var recipe in api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes)
            foreach (var ing in recipe.Ingredients)
            {
                var code = ing?.ResolvedItemStack?.Collectible?.Code;
                if (code == null) continue;
                if (!dict.TryGetValue(code, out var list)) dict[code] = list = [];
                // Prevent duplicate entries when a recipe has the same ingredient multiple times
                if (list.Count == 0 || list[^1] != recipe) list.Add(recipe);
            }

            return dict;
        });
        return byIngredient.TryGetValue(collObj.Code, out var recipes) ? recipes : [];
    }

    public static IEnumerable<GridRecipe> GetGridRecipesAsIngredient(this CollectibleObject collObj, ICoreAPI api)
    {
        var byIngredient = ObjectCacheUtil.GetOrCreate(api, $"{Core.ModId}:gridRecipesByIngredient", () =>
        {
            var dict = new Dictionary<AssetLocation, List<GridRecipe>>();
            foreach (var recipe in api.World.GridRecipes)
            foreach (var ing in recipe.RecipeIngredients)
            {
                var code = ing?.ResolvedItemStack?.Collectible?.Code;
                if (code == null) continue;
                if (!dict.TryGetValue(code, out var list)) dict[code] = list = [];
                if (list.Count == 0 || list[^1] != recipe) list.Add(recipe);
            }

            return dict;
        });
        return byIngredient.TryGetValue(collObj.Code, out var recipes) ? recipes : [];
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

    /// <summary>
    ///     Gets the metal properties variant from a CollectibleBehaviorQuenchable behavior.
    /// </summary>
    public static CollectibleBehaviorQuenchable.MetalPropertyVariant? GetMetalProps(
        this CollectibleBehaviorQuenchable behavior)
    {
        return behavior?.GetField<CollectibleBehaviorQuenchable.MetalPropertyVariant>("metalProps");
    }
}

using System.Collections.Generic;
using System.Linq;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.Metal;

#nullable enable
public static class MetalMaterialExtensions
{
    #region CollectibleObject

    public static MetalMaterial? GetOrCacheMetalMaterial(this CollectibleObject collObj, ICoreAPI api)
    {
        var metalMaterial =
            CacheHelper.GetOrAdd(Core.MetalMaterialCache, collObj.Code, () => collObj.GetMetalMaterial(api));
        return metalMaterial;
    }

    private static MetalMaterial? GetMetalMaterial(this CollectibleObject collObj, ICoreAPI api)
    {
        var metalMaterial = GetMetalMaterialDirect(collObj, api);
        if (metalMaterial != null) return metalMaterial;

        // If that fails (coke oven door), try to get the variant from the smithing recipe
        var smithingRecipe = collObj.GetSmithingRecipe(api);
        if (smithingRecipe is { Ingredient.ResolvedItemstack: var ingredientStack })
        {
            var metalVariant = ingredientStack.Collectible.GetMetalVariant();
            metalMaterial = MetalMaterialLoader.GetMaterial(api, metalVariant);
            if (metalMaterial != null)
                return metalMaterial;
        }

        // If that fails, check if the ingredient can be crafted into metal bits or similar
        var childRecipes = collObj.GetGridRecipesAsIngredient(api);
        Core.Logger.VerboseDebug(
            $"[MetalMaterial] CollectibleObject {collObj.Code} has no metal material defined, trying to resolve from {childRecipes.Count()} recipes (as ingredient).");
        if (TryGetMetalMaterialFromIngredients(api, childRecipes, out metalMaterial))
            return metalMaterial;

        // If that fails, return null
        Core.Logger.VerboseDebug(
            $"[MetalMaterial] Failed to find metal material for collectible {collObj.Code}");
        return null;
    }

    // To get the metal material directly from the CollectibleObject's attributes or its code, if possible
    private static MetalMaterial? GetMetalMaterialDirect(this CollectibleObject collObj, ICoreAPI api)
    {
        Core.Logger.VerboseDebug(
            $"[MetalMaterial] Trying to resolve metal material for CollectibleObject {collObj.Code} directly.");
        MetalMaterial? metalMaterial;
        // First get the material from attributes, if available
        if (collObj.Attributes?["metalMaterial"].Exists ?? false)
        {
            var materialCode = collObj.Attributes["metalMaterial"].AsString();
            metalMaterial = MetalMaterialLoader.GetMaterial(api, materialCode);
            if (metalMaterial != null) return metalMaterial;
            Core.Logger.VerboseDebug(
                $"[MetalMaterial] CollectibleObject {collObj.Code} has metalMaterial attribute with code {materialCode}, but no matching material found.");
        }

        // Try to grab the variant directly
        var metalVariant = collObj.GetMetalVariant();
        metalMaterial = MetalMaterialLoader.GetMaterial(api, metalVariant);
        return metalMaterial;
    }

    private static bool TryGetMetalMaterial(IEnumerable<GridRecipe> gridRecipes,
        Func<CollectibleObject, MetalMaterial?> materialResolver, out MetalMaterial? metalMaterial)
    {
        metalMaterial = null;
        foreach (var gridRecipe in gridRecipes)
        {
            var ingredients =
                from ing in gridRecipe.resolvedIngredients
                where ing is { ResolvedItemstack: not null } &&
                      ing.IsTool != true &&
                      ing.ResolvedItemstack.Collectible != null
                select ing.ResolvedItemstack.Collectible;
            foreach (var ingredient in ingredients)
            {
                if (ingredient == null) continue;
                metalMaterial = materialResolver(ingredient);
                if (metalMaterial != null) return true;
            }
        }

        return metalMaterial != null;
    }

    private static bool TryGetMetalMaterialFromIngredients(ICoreAPI api, IEnumerable<GridRecipe> gridRecipes,
        out MetalMaterial? metalMaterial)
    {
        return TryGetMetalMaterial(gridRecipes, ingredient => ingredient.GetMetalMaterialDirect(api),
            out metalMaterial);
    }

    // Use when what matters is the processed result (e.g., iron bloom > iron, blister steel > steel)
    private static MetalMaterial? GetMetalMaterialProcessed(this CollectibleObject collectibleObject, ICoreAPI api)
    {
        // This instead gets the metal material of the items created by smithing this item
        var smithingRecipes = collectibleObject.GetSmithingRecipesAsIngredient(api);
        MetalMaterial? metalMaterial = null;
        foreach (var recipe in smithingRecipes)
        {
            var ingredient = recipe.Output.ResolvedItemstack?.Collectible;
            if (ingredient == null) continue;
            var variantCode = ingredient.GetMetalVariant();
            metalMaterial = MetalMaterialLoader.GetMaterial(api, variantCode);
            if (metalMaterial != null)
                return metalMaterial;
        }

        return metalMaterial;
    }

    public static string GetMetalVariant(this CollectibleObject collObj)
    {
        return collObj.Variant["metal"] ?? collObj.Variant["material"] ?? collObj.LastCodePart();
    }

    // Simplified check using the basic vanilla convention that uses 'metal' and 'material' variants
    public static bool HasMetalMaterialSimple(this CollectibleObject collObj)
    {
        return (collObj.Variant["metal"] ?? collObj.Variant["material"]) != null;
    }

    #endregion

    #region ItemStack

    public static MetalMaterial? GetOrCacheMetalMaterial(this ItemStack itemStack, ICoreAPI api)
    {
        var collObj = itemStack.Collectible;
        if (collObj is not IAnvilWorkable anvilWorkable) return collObj?.GetMetalMaterial(api);
        var ingotStack = anvilWorkable.GetBaseMaterial(itemStack);
        var metalMaterial = ingotStack.Collectible.GetOrCacheMetalMaterial(api);
        return metalMaterial ?? collObj.GetMetalMaterial(api);
    }

    // Use when what matters is the processed result (e.g., iron bloom > iron, blister steel > steel)
    public static MetalMaterial? GetMetalMaterialProcessed(this ItemStack itemStack, ICoreAPI api)
    {
        var collObj = itemStack.Collectible;
        // Resort to the CollectibleObject method for items that are not anvil workable
        if (collObj is not IAnvilWorkable anvilWorkable) return collObj?.GetOrCacheMetalMaterial(api);
        // Grab from IAnvilWorkable
        var ingotStack = anvilWorkable.GetBaseMaterial(itemStack);
        // Try to grab the processed material from the ingot stack
        return ingotStack.Collectible.GetMetalMaterialProcessed(api);
    }

    #endregion
}
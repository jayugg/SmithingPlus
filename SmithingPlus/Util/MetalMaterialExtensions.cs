using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.Util;

#nullable enable
public static class MetalMaterialExtensions
{
    #region CollectibleObject
    
    public static MetalMaterial? GetMetalMaterial(this CollectibleObject collObj, ICoreAPI api, int recursionDepth = 0)
    {
        // First try to grab the variant directly
        var ingotCode = collObj.GetMetalIngotCode();
        var metalMaterial = new MetalMaterial(api, ingotCode);
        if (metalMaterial.Resolved) return metalMaterial;
        // If that fails (coke oven door), try to get the variant from the smithing recipe
        var smithingRecipe = collObj.GetSmithingRecipe(api);
        if (smithingRecipe is { Ingredient.ResolvedItemstack: var ingredientStack})
        {
            ingotCode = ingredientStack.Collectible.GetMetalIngotCode();
            metalMaterial = new MetalMaterial(api, ingotCode);
            if (metalMaterial.Resolved) return metalMaterial;
        }
        // If that fails, check if the ingredient can be crafted into metal bits or similar
        var childRecipes = collObj.GetGridRecipesAsIngredient(api);
        if (TryGetMetalMaterialFromIngredients(api, childRecipes, out metalMaterial))
            return metalMaterial;
        // If that fails, try to browse the grid recipes and find it recursively
        var parentRecipes = collObj.GetGridRecipes(api);
        if (TryGetMetalMaterialRecursively(api, parentRecipes, out metalMaterial, recursionDepth))
            return metalMaterial;
        // If that fails, return null
        Core.Logger.Error("[MetalMaterial] Failed to find metal material {0}", ingotCode);
        return null;
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
                if (metalMaterial?.Resolved ?? false) break;
            }
        }
        return metalMaterial?.Resolved ?? false;
    }

    private static bool TryGetMetalMaterialFromIngredients(ICoreAPI api, IEnumerable<GridRecipe> gridRecipes, out MetalMaterial? metalMaterial)
    {
        return TryGetMetalMaterial(gridRecipes, ingredient =>
        {
            var ingotCode = ingredient.GetMetalIngotCode();
            return new MetalMaterial(api, ingotCode);
        }, out metalMaterial);
    }

    private static bool TryGetMetalMaterialRecursively(ICoreAPI api, IEnumerable<GridRecipe> gridRecipes, out MetalMaterial? metalMaterial, int recursionDepth = 0)
    {
        // Stop recursion if the maximum depth is reached
        metalMaterial = null;
        if (recursionDepth < 5)
            return TryGetMetalMaterial(gridRecipes, ingredient =>
                ingredient.GetMetalMaterial(api, recursionDepth + 1), out metalMaterial);
        Core.Logger.Warning("[MetalMaterial] Recursion depth limit reached while resolving metal material.");
        return false;
    }

    private static AssetLocation GetMetalIngotCode(this CollectibleObject collObj)
    {
        var variant = collObj.GetMetalVariant();
        return new AssetLocation($"{collObj.Code.Domain}:ingot-{variant}");
    }

    // Use when what matters is the processed result (e.g. iron bloom > iron, blister steel > steel)
    public static MetalMaterial? GetMetalMaterialProcessed(this CollectibleObject collectibleObject, ICoreAPI api)
    {
        // This instead gets the metal material of the items created by smithing this item
        var smithingRecipes = collectibleObject.GetSmithingRecipesAsIngredient(api);
        MetalMaterial? metalMaterial = null;
        foreach (var recipe in smithingRecipes)
        {
            var ingredient = recipe.Output.ResolvedItemstack?.Collectible;
            if (ingredient == null) continue;
            var ingotCode = ingredient.GetMetalIngotCode();
            metalMaterial = new MetalMaterial(api, ingotCode);
            if (metalMaterial.Resolved) return metalMaterial;
        }
        return metalMaterial;
    }

    public static string GetMetalVariant(this CollectibleObject collObj)
    {
        return collObj.Variant["metal"] ?? collObj.Variant["material"] ?? collObj.LastCodePart();
    }
    
    public static bool HasMetalMaterialSimple(this CollectibleObject collObj)
    {
        return (collObj.Variant["metal"] ?? collObj.Variant["material"]) != null;
    }

    #endregion
    
    #region ItemStack
    
    public static MetalMaterial? GetMetalMaterial(this ItemStack itemStack, ICoreAPI api)
    {
        var collObj = itemStack.Collectible;
        // Resort to the CollectibleObject method for items that are not anvil workable
        if (collObj is not IAnvilWorkable anvilWorkable) return collObj?.GetMetalMaterial(api);
        // Grab from IAnvilWorkable
        var ingotStack = anvilWorkable.GetBaseMaterial(itemStack);
        var metalMaterial = new MetalMaterial(api, ingotStack);
        return metalMaterial.Resolved ? metalMaterial : collObj.GetMetalMaterial(api);
    }
    
    // Use when what matters is the processed result (e.g. iron bloom > iron, blister steel > steel)
    public static MetalMaterial? GetMetalMaterialProcessed(this ItemStack itemStack, ICoreAPI api)
    {
        var collObj = itemStack.Collectible;
        // Resort to the CollectibleObject method for items that are not anvil workable
        if (collObj is not IAnvilWorkable anvilWorkable) return collObj?.GetMetalMaterial(api);
        // Grab from IAnvilWorkable
        var ingotStack = anvilWorkable.GetBaseMaterial(itemStack);
        // Try to grab the processed material from the ingot stack
        return ingotStack.Collectible.GetMetalMaterialProcessed(api);
    }
    #endregion
}
using System;
using System.Collections.Generic;
using System.Linq;
using SmithingPlus.Metal;
using SmithingPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace SmithingPlus.SmithWithBits;

public class ItemWorkableNugget : ItemNugget, IAnvilWorkable
{
    public Item BaseMaterial => CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible as Item ?? this;

    public string MetalVariant => BaseMaterial.Variant["metal"];

    // Support for ExtraCode mod
    public bool IsBlisterSteelLike => IsBlisterSteel || Attributes["blisterSteelLike"].AsBool();

    private bool IsBlisterSteel =>
        BaseMaterial.Variant["metal"] == "blistersteel" && BaseMaterial.Code.Domain == "game";

    public int GetRequiredAnvilTier(ItemStack stack)
    {
        var key = MetalVariant;
        var defaultValue = 0;
        if (api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode
            .TryGetValue(key, out var metalPropertyVariant))
            defaultValue = metalPropertyVariant.Tier - 1;
        var attributes = stack.Collectible.Attributes;
        if ((attributes != null ? attributes["requiresAnvilTier"].Exists ? 1 : 0 : 0) != 0)
            defaultValue = stack.Collectible.Attributes["requiresAnvilTier"].AsInt(defaultValue);
        return defaultValue;
    }

    /// <summary>
    ///     Get all smithing recipes that match the given stack
    /// </summary>
    /// <param name="stack"> The stack to match </param>
    /// <returns> A list of matching recipes </returns>
    public List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
    {
        return api.GetSmithingRecipes()
            .Where((System.Func<SmithingRecipe, bool>)(r => r.Ingredient.SatisfiesAsIngredient(stack)))
            .OrderBy((System.Func<SmithingRecipe, AssetLocation>)(r => r.Output.ResolvedItemstack.Collectible.Code))
            .ToList();
    }

    public bool CanWork(ItemStack stack)
    {
        var temperature = stack.Collectible.GetTemperature(api.World, stack);
        var meltingPoint = stack.Collectible.GetMeltingPoint(api.World, null, new DummySlot(stack));
        var attributes = stack.Collectible.Attributes;
        return (attributes != null ? attributes["workableTemperature"].Exists ? 1 : 0 : 0) != 0
            ? stack.Collectible.Attributes["workableTemperature"].AsFloat(meltingPoint / 2f) <= (double)temperature
            : temperature >= meltingPoint / 2.0;
    }


    public virtual ItemStack TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil)
    {
        if (!CanWork(stack) || (beAnvil.WorkItemStack != null && !beAnvil.CanWorkCurrent))
            return null;
        var obj = api.World.GetItem(new AssetLocation("workitem-" + MetalVariant));
        if (obj == null)
            return null;
        var itemStack = new ItemStack(obj);
        itemStack.Collectible.SetTemperature(api.World, itemStack, stack.Collectible.GetTemperature(api.World, stack));
        if (beAnvil.WorkItemStack == null)
        {
            if (!Core.Config.SmithWithBits) return null;
            CreateVoxelsFromNugget(api, ref beAnvil.Voxels);
        }
        else
        {
            if (!Core.Config.BitsTopUp) return null;
            var nuggetMaterial = stack.GetOrCacheMetalMaterial(api);
            var workItemMaterial = beAnvil.WorkItemStack.GetMetalMaterialProcessed(api);
            Core.Logger.VerboseDebug(
                "[ItemWorkableNugget#TryPlaceOn] nugget base material: {0}, workItem base material: {1}",
                nuggetMaterial?.IngotCode, workItemMaterial?.IngotCode);
            if (workItemMaterial == null || !workItemMaterial.Equals(nuggetMaterial))
            {
                if (api is ICoreClientAPI capi1)
                    capi1.TriggerIngameError(this, "notequal",
                        Lang.Get("Must be the same metal to add voxels"));
                return null;
            }

            var bits = AddVoxelsFromNugget(api, ref beAnvil.Voxels);
            if (bits != 0) return itemStack;

            if (api is ICoreClientAPI capi2)
                capi2.TriggerIngameError(this, "requireshammering",
                    Lang.Get("Try hammering down before adding additional voxels"));
            return null;
        }

        return itemStack;
    }

    public ItemStack GetBaseMaterial(ItemStack stack)
    {
        Core.Logger.VerboseDebug("[ItemWorkableNugget#GetBaseMaterial] {0}", BaseMaterial.Code);
        if (stack.Collectible is ItemWorkableNugget)
        {
            if (!IsBlisterSteelLike) return new ItemStack(BaseMaterial);
            var refinedVariant = BaseMaterial.Attributes["refinedVariant"].AsString("steel");
            return new ItemStack(BaseMaterial.ItemWithVariant("metal", refinedVariant));
        }

        Core.Logger.Warning("[ItemWorkableNugget#GetBaseMaterial] Item {0} is not a workable nugget",
            stack.Collectible.Code);
        return stack;
    }

    public EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil)
    {
        return EnumHelveWorkableMode.NotWorkable;
    }

    public int VoxelCountForHandbook(ItemStack stack)
    {
        return 2;
    }

    public override void OnCreatedByCrafting(
        ItemSlot[] allInputslots,
        ItemSlot outputSlot,
        GridRecipe byRecipe)
    {
        base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe);
        if (outputSlot.Itemstack == null) return;
        var recipeTools = byRecipe?.Ingredients?.Values.Where(ing =>
            ing?.ResolvedItemstack?.Collectible is ItemChisel ||
            ing?.RecipeAttributes?[ModRecipeAttributes.RecyclingRecipe]?.AsBool() == true &&
            ing.IsTool).ToArray() ?? [];
        if (recipeTools.Length == 0) return;
        int? voxelCount = null;
        
        var inputWorkItemSlot = allInputslots?
            .FirstOrDefault(slot =>
                slot.Itemstack?.Collectible is ItemWorkItem &&
                recipeTools.Any(tool => tool.SatisfiesAsIngredient(slot.Itemstack)));
        
        if (inputWorkItemSlot != null)
        {
            var voxels = BlockEntityAnvil.deserializeVoxels(inputWorkItemSlot.Itemstack.Attributes.GetBytes("voxels"));
            voxelCount = voxels.MaterialCount();
        }
        
        var inputSmithedItemSlot = allInputslots?
            .FirstOrDefault(slot => 
                slot.Itemstack?.GetLargestSmithingRecipe(api) != null &&
                recipeTools.Any(tool => tool.SatisfiesAsIngredient(slot.Itemstack)));
        if (voxelCount == null && inputSmithedItemSlot != null)
        {
            var largestRecipe = inputSmithedItemSlot.Itemstack.GetLargestSmithingRecipe(api);
            if (largestRecipe != null)
            {
                var largestOutput = Math.Max(largestRecipe.Output.ResolvedItemstack.StackSize, 1);
                var totalVoxels = largestRecipe.Voxels.VoxelCount();
                voxelCount = totalVoxels / largestOutput;
            }
        }
        if (voxelCount == null) return;
        outputSlot.Itemstack.StackSize = Math.Max((int)(voxelCount / Core.Config.VoxelsPerBit), 1);
        var sourceSlot = inputWorkItemSlot ?? inputSmithedItemSlot;

        var temperature = sourceSlot.Itemstack.Collectible.GetTemperature(api.World, sourceSlot.Itemstack);
        outputSlot.Itemstack.Collectible.SetTemperature(api.World, outputSlot.Itemstack, temperature);
    }
    
    public List<SmithingRecipe> GetMatchingRecipes(ICoreAPI coreApi)
    {
        var ingotStack = Attributes[ModAttributes.IsPureMetal].AsBool() &&
                         CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible is ItemIngot itemIngot
            ? new ItemStack(itemIngot)
            : new ItemStack(this);

        return coreApi.GetSmithingRecipes()
            .Where(r =>
                r.Ingredient.Code.Equals(ingotStack.Collectible.Code) &&
                !r.Output.ResolvedItemstack.Collectible.Code.Equals(Code) &&
                !(r.Ingredient.RecipeAttributes?[ModRecipeAttributes.RepairOnly]?.AsBool() ?? false)
            )
            .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code)
            .ThenBy(r => r.Output.ResolvedItemstack.StackSize)
            .Select(r =>
            {
                var p = r.Clone();
                p.Voxels = r.Voxels;
                p.Ingredient.Code = Code;
                p.Ingredient.Type = ItemClass;
                p.Ingredient.ResolvedItemstack = new ItemStack(this);
                return p;
            })
            .Distinct()
            .ToList();
    }

    protected static void CreateVoxelsFromNugget(
        ICoreAPI api,
        ref byte[,,] voxels,
        bool withExtraBitChance = true)
    {
        var random = api.World.Rand;
        voxels = new byte[16, 6, 16];

        voxels[8, 0, 7] = 1;
        voxels[8, 0, 8] = 1;
        if (withExtraBitChance && random.NextDouble() < Math.Max(Core.Config.VoxelsPerBit - 2.0, 0))
            voxels[8, 1, 7] = 1;
    }

    protected static int AddVoxelsFromNugget(
        ICoreAPI api,
        ref byte[,,] voxels,
        bool withExtraBitChance = true)
    {
        var nuggetConfig = new byte[16, 2, 16];
        CreateVoxelsFromNugget(api, ref nuggetConfig, withExtraBitChance);
        var voxelsCopy = (byte[,,])voxels.Clone();
        var bits = 0;

        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
        {
            var y = 0;
            while (y < 6 && voxelsCopy[x, y, z] != 0) y++;

            if (y >= 6) return 0;
            for (var ny = 0; ny < 2; ny++)
                if (nuggetConfig[x, ny, z] != 0)
                {
                    if (y + ny >= 6) return 0;
                    voxelsCopy[x, y + ny, z] = nuggetConfig[x, ny, z];
                    if (voxelsCopy[x, y + ny, z] == 1) bits++;
                }
        }

        voxels = voxelsCopy;
        return bits;
    }
}
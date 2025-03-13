using System;
using System.Collections.Generic;
using System.Linq;
using SmithingPlus.Compat;
using SmithingPlus.ToolRecovery;
using SmithingPlus.Util;
using Vintagestory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace SmithingPlus;

public class ItemWorkableNugget : ItemNugget, IAnvilWorkable
{
    public Item BaseMaterial => CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible as Item ?? this;
    public string MetalVariant => BaseMaterial.Variant["metal"];
    // Support for ExtraCode mod
    public bool IsBlisterSteelLike => IsBlisterSteel || Attributes["blisterSteelLike"].AsBool();
    private bool IsBlisterSteel => BaseMaterial.Variant["metal"] == "blistersteel" && BaseMaterial.Code.Domain == "game";
    public override void OnCreatedByCrafting(
        ItemSlot[] allInputslots,
        ItemSlot outputSlot,
        GridRecipe byRecipe)
    {
        ItemSlot itemSlot = allInputslots.FirstOrDefault((System.Func<ItemSlot, bool>) (slot => slot.Itemstack?.Collectible is ItemWorkItem));
        if (itemSlot != null && outputSlot.Itemstack is not null)
        {
            var voxels = BlockEntityAnvil.deserializeVoxels(itemSlot.Itemstack.Attributes.GetBytes("voxels"));
            var voxelCount = voxels.MaterialCount();
            var ratio = 2f + 0.1*(voxelCount / 42f);
            outputSlot.Itemstack.StackSize = Math.Max((int)(voxelCount/ratio), 1);
            var temperature = outputSlot.Itemstack.Collectible.GetTemperature(api.World, itemSlot.Itemstack);
            outputSlot.Itemstack.Collectible.SetTemperature(api.World, outputSlot.Itemstack, temperature);
        }
        base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe);
    }
    
    public int GetRequiredAnvilTier(ItemStack stack)
    {
        string key = MetalVariant;
        int defaultValue = 0;
        MetalPropertyVariant metalPropertyVariant;
        if (api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(key, out metalPropertyVariant))
            defaultValue = metalPropertyVariant.Tier - 1;
        JsonObject attributes = stack.Collectible.Attributes;
        if ((attributes != null ? (attributes["requiresAnvilTier"].Exists ? 1 : 0) : 0) != 0)
            defaultValue = stack.Collectible.Attributes["requiresAnvilTier"].AsInt(defaultValue);
        return defaultValue;
    }
    
    private static ItemStack GetRecipeStack(ItemStack stack)
    {
        if (stack.ItemAttributes[ModAttributes.IsPureMetal].AsBool() &&
            stack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible is ItemIngot itemIngot)
        {
            return new ItemStack(itemIngot);
        }
        return stack;
    }

    public List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
    {
        return this.api.GetSmithingRecipes().Where((System.Func<SmithingRecipe, bool>) (r => r.Ingredient.SatisfiesAsIngredient(stack))).OrderBy<SmithingRecipe, AssetLocation>((System.Func<SmithingRecipe, AssetLocation>) (r => r.Output.ResolvedItemstack.Collectible.Code)).ToList<SmithingRecipe>();
    }

    
    public List<SmithingRecipe> GetMatchingRecipes(ICoreAPI coreApi)
    {
        var ingotStack = this.Attributes[ModAttributes.IsPureMetal].AsBool() &&
                         this.CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible is ItemIngot itemIngot
            ? new ItemStack(itemIngot)
            : new ItemStack(this);
        
        return coreApi.GetSmithingRecipes()
            .Where(r => 
                r.Ingredient.Code.Equals(ingotStack.Collectible.Code) &&
                !r.Output.ResolvedItemstack.Collectible.Code.Equals(this.Code) &&
                !(r.Ingredient.RecipeAttributes?[ModRecipeAttributes.RepairOnly]?.AsBool() ?? false)
                )
            .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code)
            .Select(r =>
            {
                var p = r.Clone();
                p.Ingredient.Code = this.Code;
                p.Ingredient.Type = this.ItemClass;
                p.Ingredient.ResolvedItemstack = new ItemStack(this);
                return p;
            })
            .Distinct()
            .ToList();
    }

    public bool CanWork(ItemStack stack)
    {
        float temperature = stack.Collectible.GetTemperature(api.World, stack);
        float meltingPoint = stack.Collectible.GetMeltingPoint(api.World, null, new DummySlot(stack));
        JsonObject attributes = stack.Collectible.Attributes;
        return (attributes != null ? (attributes["workableTemperature"].Exists ? 1 : 0) : 0) != 0 ? stack.Collectible.Attributes["workableTemperature"].AsFloat(meltingPoint / 2f) <= (double) temperature : temperature >= meltingPoint / 2.0;
    }


    public ItemStack TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil)
    {
        if (!CanWork(stack) || (beAnvil.WorkItemStack != null && !beAnvil.CanWorkCurrent))
            return null;
        Item obj = api.World.GetItem(new AssetLocation("workitem-" + MetalVariant));
        if (obj == null)
            return null;
        ItemStack itemstack = new ItemStack(obj);
        itemstack.Collectible.SetTemperature(api.World, itemstack, stack.Collectible.GetTemperature(api.World, stack));
        if (beAnvil.WorkItemStack == null)
        {
            if (!Core.Config.SmithWithBits) return null;
            CreateVoxelsFromNugget(api, ref beAnvil.Voxels);
            if (ThriftySmithingCompat.ThriftySmithingLoaded) itemstack.AddToCustomWorkData(beAnvil.Voxels.MaterialCount());
        }
        else
        {
            if (!Core.Config.BitsTopUp) return null;
            Core.Logger.VerboseDebug("[ItemWorkableNugget#TryPlaceOn] nugget base material: {0}, workItem base material: {1}", stack.GetBaseMaterial().Collectible.Code, beAnvil.WorkItemStack.GetBaseMaterial().Collectible.Code);
            if (!beAnvil.WorkItemStack.GetBaseMaterial().CodeMatches(stack.GetBaseMaterial()))
            {
                if (api.Side == EnumAppSide.Client)
                    ((ICoreClientAPI)api).TriggerIngameError(this, "notequal", Lang.Get("Must be the same metal to add voxels"));
                return null;
            }
            var bits = AddVoxelsFromNugget(api, ref beAnvil.Voxels);
            if (bits != 0)
            {
                if (ThriftySmithingCompat.ThriftySmithingLoaded) beAnvil.WorkItemStack.AddToCustomWorkData(bits);
                return itemstack;
            }
            if (api.Side == EnumAppSide.Client)
                ((ICoreClientAPI)api).TriggerIngameError(this, "requireshammering", Lang.Get("Try hammering down before adding additional voxels"));
            return null;
        }
        return itemstack;
    }

    public static void CreateVoxelsFromNugget(
        ICoreAPI api,
        ref byte[,,] voxels,
        bool withExtraBitChance = true)
    {
        var random = api.World.Rand;
        voxels = new byte[16, 6, 16];

        voxels[8, 0, 7] = 1;
        voxels[8, 0, 8] = 1;
        if (withExtraBitChance && random.NextDouble() < Math.Max(Core.Config.VoxelsPerBit - 2.0, 0))
        {
            voxels[8, 1, 7] = 1;
        }
    }
    
    public static int AddVoxelsFromNugget(
        ICoreAPI api,
        ref byte[,,] voxels, 
        bool withExtraBitChance = true)
    {
        var nuggetConfig = new byte[16, 2, 16];
        CreateVoxelsFromNugget(api, ref nuggetConfig, withExtraBitChance);
        var voxelsCopy = (byte[,,])voxels.Clone();
        int bits = 0;

        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                int y = 0;
                while (y < 6 && voxelsCopy[x, y, z] != 0)
                {
                    y++;
                }

                if (y >= 6)
                {
                    return 0;
                }
                for (int ny = 0; ny < 2; ny++)
                {
                    if (nuggetConfig[x, ny, z] != 0)
                    {
                        if (y + ny >= 6)
                        {
                            return 0;
                        }
                        voxelsCopy[x, y + ny, z] = nuggetConfig[x, ny, z];
                        if (voxelsCopy[x, y + ny, z] == 1) bits++;
                    }
                }
            }
        }
        voxels = voxelsCopy;
        return bits;
    }

    public ItemStack GetBaseMaterial(ItemStack stack)
    {
        Core.Logger.VerboseDebug("[ItemWorkableNugget#GetBaseMaterial] {0}", BaseMaterial.Code);
        if (stack.Collectible is ItemWorkableNugget workableNugget)
        {
            if (!IsBlisterSteelLike) return new ItemStack(BaseMaterial);
            var refinedVariant = BaseMaterial.Attributes["refinedVariant"].AsString("steel");
            return new ItemStack(BaseMaterial.ItemWithVariant("metal", refinedVariant));
        }
        Core.Logger.Warning("[ItemWorkableNugget#GetBaseMaterial] Item {0} is not a workable nugget", stack.Collectible.Code);
        return stack;
    }

    public EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil)
    {
        return EnumHelveWorkableMode.NotWorkable;
    }
}
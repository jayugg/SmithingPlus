using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace SmithingPlus;

public class ItemWorkableNugget : ItemNugget, IAnvilWorkable
{
    public Item BaseMaterial => CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible as Item ?? this;
    public string MetalVariant => CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible is Item ? CombustibleProps.SmeltedStack.ResolvedItemstack.Collectible.Variant["metal"] : Variant["metal"];
    
    public override void OnCreatedByCrafting(
        ItemSlot[] allInputslots,
        ItemSlot outputSlot,
        GridRecipe byRecipe)
    {
        ItemSlot itemSlot = allInputslots.FirstOrDefault((System.Func<ItemSlot, bool>) (slot => slot.Itemstack?.Collectible is ItemWorkItem));
        if (itemSlot != null)
        {
            var voxels = BlockEntityAnvil.deserializeVoxels(itemSlot.Itemstack.Attributes.GetBytes("voxels"));
            var voxelCount = voxels.Cast<byte>().Count(voxel => voxel != 0);
            var ratio = 2f + 0.1*(voxelCount / 42f);
            outputSlot.Itemstack.StackSize = Math.Max((int)(voxelCount/ratio), 1);
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
        if (stack.ItemAttributes["isPureMetal"].AsBool() &&
            stack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible is ItemIngot itemIngot)
        {
            return new ItemStack(itemIngot);
        }
        return stack;
    }

    public List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
    {
        return api.GetSmithingRecipes().Where((System.Func<SmithingRecipe, bool>) (r => r.Ingredient.SatisfiesAsIngredient(GetRecipeStack(stack)) || r.Ingredient.SatisfiesAsIngredient(stack))).OrderBy((System.Func<SmithingRecipe, AssetLocation>) (r => r.Output.ResolvedItemstack.Collectible.Code)).ToList();
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
        if (!CanWork(stack))
            return null;
        Item obj = api.World.GetItem(new AssetLocation("workitem-" + MetalVariant));
        if (obj == null)
            return null;
        ItemStack itemstack = new ItemStack(obj);
        itemstack.Collectible.SetTemperature(api.World, itemstack, stack.Collectible.GetTemperature(api.World, stack));
        if (beAnvil.WorkItemStack == null)
        {
            CreateVoxelsFromNugget(api, ref beAnvil.Voxels);
        }
        else
        {
            if (!string.Equals(beAnvil.WorkItemStack.Collectible.Variant["metal"], MetalVariant))
            {
                if (api.Side == EnumAppSide.Client)
                    ((ICoreClientAPI)api).TriggerIngameError(this, "notequal", Lang.Get("Must be the same metal to add voxels"));
                return null;
            }
            if (AddVoxelsFromNugget(api, ref beAnvil.Voxels)) return itemstack;
            if (api.Side == EnumAppSide.Client)
                ((ICoreClientAPI)api).TriggerIngameError(this, "requireshammering", Lang.Get("Try hammering down before adding additional voxels"));
            return null;
        }
        return itemstack;
    }

    public static void CreateVoxelsFromNugget(
        ICoreAPI api,
        ref byte[,,] voxels)
    {
        var random = api.World.Rand;
        voxels = new byte[16, 6, 16];

        voxels[8, 0, 7] = 1;
        voxels[8, 0, 8] = 1;
        if (random.NextDouble() < 0.1)
        {
            voxels[8, 1, 7] = 1;
        }
    }
    
    public static bool AddVoxelsFromNugget(ICoreAPI api, ref byte[,,] voxels)
    {
        var nuggetConfig = new byte[16, 2, 16];
        CreateVoxelsFromNugget(api, ref nuggetConfig);
        var voxelsCopy = (byte[,,])voxels.Clone();
        bool canAdd = false;

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
                    return false;
                }
                for (int ny = 0; ny < 2; ny++)
                {
                    if (nuggetConfig[x, ny, z] != 0)
                    {
                        if (y + ny >= 6)
                        {
                            return false;
                        }
                        voxelsCopy[x, y + ny, z] = nuggetConfig[x, ny, z];
                        canAdd = true;
                    }
                }
            }
        }
        voxels = voxelsCopy;
        return canAdd;
    }

    public ItemStack GetBaseMaterial(ItemStack stack)
    {
        return new ItemStack(BaseMaterial);
    }

    public EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil)
    {
        return EnumHelveWorkableMode.NotWorkable;
    }
}
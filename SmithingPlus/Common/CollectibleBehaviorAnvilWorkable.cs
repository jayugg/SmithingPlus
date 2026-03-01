#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using SmithingPlus.Common.Metal;
using SmithingPlus.Metal;
using SmithingPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace SmithingPlus.Common;

public abstract class CollectibleBehaviorAnvilWorkable(CollectibleObject collObj) :
    CollectibleBehavior(collObj), IAnvilWorkable
{
    protected ICoreAPI? Api => collObj.GetField<ICoreAPI>("api");
    protected abstract byte[,,] Voxels { get; }

    protected MetalMaterial? MetalMaterial =>
        Api != null
            ? MetalMaterialLoader.GetMaterial(Api, collObj.GetMetalVariant()) ?? collObj.GetMetalMaterialSmelted(Api)
            : null;

    protected virtual AnvilPlacementMode PlacementMode { get; set; } = AnvilPlacementMode.Normal;

    public virtual ItemStack? TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil)
    {
        if (Api == null || !CanWork(stack) || beAnvil is { WorkItemStack: not null, CanWorkCurrent: false })
            return null;
        var workItemStack = MetalMaterial?.WorkItemStack;
        if (workItemStack == null)
            return null;
        var sourceTemp = stack.GetTemperature(Api.World);
        workItemStack.SetTemperature(Api.World, sourceTemp);

        if (beAnvil.WorkItemStack == null && PlacementMode.AllowsEmpty())
        {
            TryAddVoxelsFromWorkable(Api, ref beAnvil.Voxels);
            return workItemStack;
        }

        if (!PlacementMode.AllowsPresent())
            return null;

        var workItemMaterial = beAnvil.WorkItemStack?.GetMetalMaterialProcessed(Api);
        Core.Logger.VerboseDebug(
            $"[{nameof(CollectibleBehaviorAnvilWorkable)}#{nameof(TryPlaceOn)}] base material: {MetalMaterial?.IngotCode}, " +
            $"workItem base material: {workItemMaterial?.IngotCode}");

        if (workItemMaterial == null || !workItemMaterial.Equals(MetalMaterial))
        {
            (Api as ICoreClientAPI)?.TriggerIngameError(this, "notequal",
                Lang.Get("Must be the same metal to add voxels"));
            return null;
        }

        var didSucceed = TryAddVoxelsFromWorkable(Api, ref beAnvil.Voxels);
        if (didSucceed) return workItemStack;

        (Api as ICoreClientAPI)?.TriggerIngameError(this, "requireshammering",
            Lang.Get("Try hammering down before adding additional voxels"));
        return null;
    }

    public virtual bool CanWork(ItemStack stack)
    {
        var temperature = stack.Collectible.GetTemperature(Api?.World, stack);
        var meltingPoint = stack.Collectible.GetMeltingPoint(Api?.World, null, new DummySlot(stack));
        if (stack.ItemAttributes?["workableTemperature"].Exists == true)
            return stack.ItemAttributes["workableTemperature"].AsFloat(meltingPoint / 2) <= temperature;
        return temperature >= meltingPoint / 2;
    }

    public virtual int GetRequiredAnvilTier(ItemStack stack)
    {
        var defaultValue = 0;
        if (MetalMaterial != null)
            defaultValue = MetalMaterial.Tier - 1;
        var attributes = stack.Collectible.Attributes;
        if ((attributes != null ? attributes["requiresAnvilTier"].Exists ? 1 : 0 : 0) != 0)
            defaultValue = stack.Collectible.Attributes["requiresAnvilTier"].AsInt(defaultValue);
        return defaultValue;
    }

    public virtual List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
    {
        return Api.GetSmithingRecipes()
            .Where(r =>
                ((MetalMaterial?.IngotStack is { } baseMetal && r.Ingredient.SatisfiesAsIngredient(baseMetal))
                 || r.Ingredient.SatisfiesAsIngredient(stack))
                && !r.Output.ResolvedItemstack.Collectible.Code.Equals(collObj.Code))
            .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code)
            .ThenBy(r => r.Output.ResolvedItemstack.StackSize)
            .DistinctBy(r => r.Output.ResolvedItemstack)
            .ToList();
    }

    public virtual ItemStack? GetBaseMaterial(ItemStack stack)
    {
        return MetalMaterial?.IngotStack;
    }

    public virtual EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil)
    {
        return EnumHelveWorkableMode.NotWorkable;
    }

    public virtual int VoxelCountForHandbook(ItemStack stack)
    {
        return Voxels.MaterialCount();
    }

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        if (Api == null)
        {
            Core.Logger?.Error(
                "[CollectibleBehaviorAnvilWorkable] Reflection failed: field 'api' in collectible class is null.");
            return;
        }

        if (Voxels.MaterialCount() == 0)
            Api?.Logger.Error("CollectibleBehaviorAnvilWorkable for {0} has no voxels defined. " +
                              "Please check the 'voxels' attribute in the item JSON.", collObj.Code);
    }

    protected virtual bool TryAddVoxelsFromWorkable(ICoreAPI api, ref byte[,,] beAnvilVoxels)
    {
        if (Voxels.MaterialCount() == 0)
            return false;

        // Target (anvil) dims
        var tx = beAnvilVoxels.GetLength(0);
        var ty = beAnvilVoxels.GetLength(1);
        var tz = beAnvilVoxels.GetLength(2);

        // Source (workable) dims (up to 16x16x6 typically)
        var sx = Voxels.GetLength(0);
        var sy = Voxels.GetLength(1);
        var sz = Voxels.GetLength(2);

        var ox = Math.Min(tx, sx);
        var oz = Math.Min(tz, sz);

        // Work on a copy so we can roll back on failure
        var voxelsCopy = (byte[,,])beAnvilVoxels.Clone();

        for (var x = 0; x < ox; x++)
        for (var z = 0; z < oz; z++)
        {
            // Find first empty Y in the target column
            var y = 0;
            while (y < ty && voxelsCopy[x, y, z] != 0) y++;

            // If column is full, fail (matches your early-return behavior)
            if (y >= ty) return false;

            // Place the source column starting from ny = 0 upward
            for (var ny = 0; ny < sy; ny++)
            {
                var val = Voxels[x, ny, z];
                if (val == 0) continue;

                var tyIndex = y + ny;
                if (tyIndex >= ty)
                    // Would overflow this column — abort entirely
                    return false;

                voxelsCopy[x, tyIndex, z] = val;
                if (val == 1)
                {
                }
            }
        }

        // All columns fit — commit the copy
        beAnvilVoxels = voxelsCopy;
        return true;
    }
}

public enum AnvilPlacementMode
{
    None = -1, // No placement (why would you want this? ;p)
    Normal = 0, // Can be placed both on a workitem and on an empty anvil
    Empty = 1, // Cannot be placed when workitem is present
    Present = 2 // Cannot be placed when workitem is missing
}

public static class PlacementModeExtensions
{
    public static bool AllowsPresent(this AnvilPlacementMode mode)
    {
        return mode is AnvilPlacementMode.Normal or AnvilPlacementMode.Present;
    }

    public static bool AllowsEmpty(this AnvilPlacementMode mode)
    {
        return mode is AnvilPlacementMode.Normal or AnvilPlacementMode.Empty;
    }
}
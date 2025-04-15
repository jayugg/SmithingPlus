using SmithingPlus.Compat;
using SmithingPlus.Metal;
using SmithingPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace SmithingPlus.SmithWithBits;

public class ItemXWorkableNugget : ItemWorkableNugget
{
    // Used for xskills bits compatibility
    public new ItemStack TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil)
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
            if (ThriftySmithingCompat.ThriftySmithingLoaded)
                itemStack.AddToCustomWorkData(beAnvil.Voxels.MaterialCount());
        }
        else
        {
            if (!Core.Config.BitsTopUp) return null;
            var nuggetMaterial = stack.GetOrCacheMetalMaterial(api);
            var workItemMaterial = beAnvil.WorkItemStack.GetMetalMaterialProcessed(api);
            Core.Logger.VerboseDebug(
                "[ItemWorkableNugget#TryPlaceOn] nugget metal material: {0}, workItem metal material: {1}",
                nuggetMaterial?.IngotCode, workItemMaterial?.IngotCode);
            if (!workItemMaterial?.Equals(nuggetMaterial) ?? false)
            {
                if (api.Side == EnumAppSide.Client)
                    ((ICoreClientAPI)api).TriggerIngameError(this, "notequal",
                        Lang.Get("Must be the same metal to add voxels"));
                return null;
            }

            var bits = AddVoxelsFromNugget(api, ref beAnvil.Voxels, false);
            if (bits != 0)
            {
                if (ThriftySmithingCompat.ThriftySmithingLoaded) beAnvil.WorkItemStack.AddToCustomWorkData(bits);
                return itemStack;
            }

            if (api.Side == EnumAppSide.Client)
                ((ICoreClientAPI)api).TriggerIngameError(this, "requireshammering",
                    Lang.Get("Try hammering down before adding additional voxels"));
            return null;
        }

        return itemStack;
    }
}
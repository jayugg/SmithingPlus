using SmithingPlus.Compat;
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
        var itemstack = new ItemStack(obj);
        itemstack.Collectible.SetTemperature(api.World, itemstack, stack.Collectible.GetTemperature(api.World, stack));
        if (beAnvil.WorkItemStack == null)
        {
            if (!Core.Config.SmithWithBits) return null;
            CreateVoxelsFromNugget(api, ref beAnvil.Voxels);
            if (ThriftySmithingCompat.ThriftySmithingLoaded)
                itemstack.AddToCustomWorkData(beAnvil.Voxels.MaterialCount());
        }
        else
        {
            if (!Core.Config.BitsTopUp) return null;
            Core.Logger.VerboseDebug(
                "[ItemWorkableNugget#TryPlaceOn] nugget base material: {0}, workItem base material: {1}",
                stack.GetBaseMaterial().Collectible.Code, beAnvil.WorkItemStack.GetBaseMaterial().Collectible.Code);
            if (!beAnvil.WorkItemStack.GetBaseMaterial().CodeMatches(stack.GetBaseMaterial()))
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
                return itemstack;
            }

            if (api.Side == EnumAppSide.Client)
                ((ICoreClientAPI)api).TriggerIngameError(this, "requireshammering",
                    Lang.Get("Try hammering down before adding additional voxels"));
            return null;
        }

        return itemstack;
    }
}
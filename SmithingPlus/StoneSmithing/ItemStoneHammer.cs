using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.Extra;

public class ItemStoneHammer : ItemHammer
{
    public static int MaxHitCount = 3;
    protected override void strikeAnvil(EntityAgent byEntity, ItemSlot slot)
    {
        IPlayer player = (byEntity as EntityPlayer)?.Player;
        BlockSelection currentBlockSelection = player?.CurrentBlockSelection;
        if (currentBlockSelection == null)
            return;
        var random = byEntity.World.Rand;
        BlockEntity blockEntity = byEntity.World.BlockAccessor.GetBlockEntity(currentBlockSelection.Position);
        if (blockEntity is BlockEntityAnvilPart blockEntityAnvilPart)
        {
            if (slot.Itemstack == null)
                return;
            if (!HitVoxelAndCheck(slot.Itemstack, currentBlockSelection.SelectionBoxIndex, GetHitHardness(random)))
                return;
            blockEntityAnvilPart.OnHammerHitOver(player, currentBlockSelection.HitPosition);
        }
        else
        {
            if (byEntity.World.BlockAccessor.GetBlock(currentBlockSelection.Position) is not BlockAnvil || blockEntity is not BlockEntityAnvil blockEntityAnvil)
                return;
            if (blockEntityAnvil.WorkItemStack is not {Collectible: IAnvilWorkable } workItemStack)
                return;
            var metalTier =
                ((IAnvilWorkable)workItemStack.Collectible)?.GetRequiredAnvilTier(blockEntityAnvil
                    .WorkItemStack) ?? 0;
            Core.Logger.VerboseDebug("[ItemStoneHammer#strikeAnvil] Metal tier: {0}", metalTier);
            if (!HitVoxelAndCheck(blockEntityAnvil.WorkItemStack, currentBlockSelection.SelectionBoxIndex, GetHitHardness(random, metalTier)))
                return;
            if (this.api.World.Side == EnumAppSide.Client)
                blockEntityAnvil.InvokeOnUseOver(player, currentBlockSelection.SelectionBoxIndex);
        }
        slot.Itemstack?.TempAttributes.SetBool("isAnvilAction", false);
    }

    public int GetHitHardness(Random random, int metalTier = 1)
    {
        var tierModifier = metalTier / 10f;
        var modifiedHardness = Math.Max(GetHitHardness() - tierModifier, 0.1);
        var intHardness = (int)(modifiedHardness - tierModifier);
        var decimalHardness = modifiedHardness - intHardness;
        if (random.NextDouble() < decimalHardness)
            return intHardness + 1;
        Core.Logger.VerboseDebug("[ItemStoneHammer#GetHitHardness] Hit hardness: {0}", intHardness);
        return intHardness;
    }
    
    public float GetHitHardness()
    {
        return this.Attributes["hitHardnessByType"]?.AsFloat(1.3f) ?? 1.3f;
    }
    
    public static bool HitVoxelAndCheck(ItemStack stack, int selectionBoxIndex, int hardness = 1)
    {
        int hitCount = GetVoxelHitCount(stack, selectionBoxIndex) + hardness;
        if (hitCount >= MaxHitCount)
        {
            SetVoxelHitCount(stack, selectionBoxIndex, 0);
            return true;
        }
        SetVoxelHitCount(stack, selectionBoxIndex, hitCount);
        return false;
    }

    public static void SetVoxelHitCount(ItemStack stack, int selectionBoxIndex, int hitCount)
    {
        var hitCounts = GetVoxelHitCounts(stack);
        hitCounts[selectionBoxIndex] = hitCount;
        SetVoxelHitCounts(stack, hitCounts);
    }

    public static void SetVoxelHitCounts(ItemStack stack, Dictionary<int, int> hitCounts)
    {
        byte[] byteArray = new byte[hitCounts.Count * 2];
        int index = 0;
        foreach (var kvp in hitCounts)
        {
            byteArray[index++] = (byte)kvp.Key;
            byteArray[index++] = (byte)kvp.Value;
        }
        stack.TempAttributes.SetBytes("sp:voxelHitCounts", byteArray);
    }

    public static Dictionary<int, int> GetVoxelHitCounts(ItemStack stack)
    {
        byte[] byteArray = stack.TempAttributes.GetBytes("sp:voxelHitCounts", new byte[0]);
        var hitCounts = new Dictionary<int, int>();
        for (int i = 0; i < byteArray.Length; i += 2)
        {
            int selectionBoxIndex = byteArray[i];
            int hitCount = byteArray[i + 1];
            hitCounts[selectionBoxIndex] = hitCount;
        }
        return hitCounts;
    }

    public static int GetVoxelHitCount(ItemStack stack, int selectionBoxIndex)
    {
        var hitCounts = GetVoxelHitCounts(stack);
        return hitCounts.GetValueOrDefault(selectionBoxIndex, 0);
    }
    
}

public static class XHammer
{
    public static void InvokeOnUseOver(this BlockEntityAnvil blockEntityAnvil, IPlayer player, int selectionBoxIndex)
    {
        MethodInfo onUseOverMethod = typeof(BlockEntityAnvil).GetMethod("OnUseOver", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(IPlayer), typeof(int) }, null);
        if (onUseOverMethod != null)
        {
            onUseOverMethod.Invoke(blockEntityAnvil, new object[] { player, selectionBoxIndex });
        }
    }
}
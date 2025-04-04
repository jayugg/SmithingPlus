#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SmithingPlus.HammerTweaks;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[HarmonyPatch(typeof(BlockEntityAnvil)), HarmonyPatchCategory(Core.HammerTweaksCategory)]
public static class BlockEntityAnvilPatch
{
    [HarmonyPrefix, HarmonyPatch("OnPlayerInteract")]
    public static bool Prefix_OnPlayerInteract(
        BlockEntityAnvil __instance,
        ref ItemStack ___workItemStack,
        ref bool __result,
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel)
    {
        var activeSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        var itemStack = activeSlot.Itemstack;
        if (itemStack?.Collectible is not ItemHammer itemHammer)
            return true;
        var flipItemToolMode = itemStack.TempAttributes.GetInt(ModAttributes.FlipItemToolMode);
        if (itemHammer.GetToolMode(activeSlot, byPlayer, blockSel) != flipItemToolMode)
            return true;
        if (byPlayer.Entity.Controls.ShiftKey)
        {
            __instance.RotateWorkItem(byPlayer.Entity.Controls.CtrlKey);
            __result = true;
            return false;
        }
        __instance.FlipWorkItem(___workItemStack, GetFacingHorizontalAxis(byPlayer));
        __result = true;
        return false;
    }

    private static EnumAxis GetFacingHorizontalAxis(IPlayer byPlayer)
    {
        var yaw = GameMath.Mod(byPlayer.Entity.Pos.Yaw, 2 * Math.PI);
        var facing = BlockFacing.EAST.FaceWhenRotatedBy(0.0f, (float)(yaw - Math.PI), 0.0f);
        var rotation = facing.Index switch
        {
            0 => EnumAxis.X, // North
            1 => EnumAxis.Z, // East
            2 => EnumAxis.X, // South
            3 => EnumAxis.Z, // West
            _ => EnumAxis.X  // Default to X to avoid crashes
        };
        return rotation;
    }

    [HarmonyReversePatch, HarmonyPatch("RegenMeshAndSelectionBoxes")]
    public static void RegenMeshAndSelectionBoxes(BlockEntityAnvil __instance)
    {
        throw new NotImplementedException("Reverse patch stub");
    }
    
    [HarmonyReversePatch, HarmonyPatch("HasAnyMetalVoxel")]
    public static bool HasAnyMetalVoxel(BlockEntityAnvil __instance)
    {
        throw new NotImplementedException("Reverse patch stub");
    }
    
    [HarmonyPrefix, HarmonyPatch(nameof(BlockEntityAnvil.recipeVoxels), MethodType.Getter)]
    public static bool BlockEntityAnvil_recipeVoxels_Patch(BlockEntityAnvil __instance, ref bool[,,] __result)
    {
        if (__instance.WorkItemStack == null) return true;
        if (__instance.SelectedRecipe == null)
            return true;
        __result = __instance.SelectedRecipe.Voxels;
        var rotationAxis = __instance.WorkItemStack.GetHorizontalRotationAxis();

        if (rotationAxis != null)
        {
            var rotationValue = __instance.WorkItemStack.GetHorizontalRotation(rotationAxis.Value);
            __instance.Api.Logger.Warning("Rotating work item stack {0} by {1} around {2} axis", __instance.WorkItemStack.Collectible.Code, rotationValue, rotationAxis);
            int? minY = __instance.WorkItemStack.Attributes.GetInt(ModAttributes.MinY);
            __result = __result.ToByteArray()
                .RotateAroundAxis(rotationAxis.Value, ref minY).ToBoolArray();
        }
        
        var rotation = __instance.rotation;
        for (var i = 0; i < rotation / 90; i++)
            __result = __result.ToByteArray().RotateAroundAxis(EnumAxis.Y).ToBoolArray();
        return false;
    }

    private static bool RotateWorkItem(this BlockEntityAnvil beAnvil, bool ccw)
    {
        var rotatedVoxels = RotateAroundAxis(beAnvil.Voxels, EnumAxis.Y);
        if (ccw) rotatedVoxels = rotatedVoxels.RotateAroundAxis(EnumAxis.Y);
        beAnvil.rotation = (beAnvil.rotation + (ccw ? 180 : 90)) % 360;
        beAnvil.Voxels = rotatedVoxels;
        RegenMeshAndSelectionBoxes(beAnvil);
        beAnvil.MarkDirty();
        return true;
    }

    private static byte[,,] RotateAroundAxis(this byte[,,] beAnvilVoxels, EnumAxis axis)
    {
        int? minY = null;
        return beAnvilVoxels.RotateAroundAxis(axis, ref minY);
    }

    private static void FlipWorkItem(this BlockEntityAnvil beAnvil, ItemStack workItemStack, EnumAxis axis)
    {
        if (axis == EnumAxis.Y) throw new ArgumentException("Axis Y is not supported for flipping.");
        if (!HasAnyMetalVoxel(beAnvil)) return;
        int? minY = null;
        beAnvil.recipeVoxels.ToByteArray().Union(beAnvil.Voxels).RotateAroundAxis(axis, ref minY);
        var rotatedVoxels = beAnvil.Voxels.RotateAroundAxis(axis, ref minY);
        if (minY.HasValue) beAnvil.WorkItemStack.Attributes.SetInt(ModAttributes.MinY, minY.Value);
        beAnvil.Voxels = rotatedVoxels;
        workItemStack.FlipHorizontalRotationAttribute(axis);
        RegenMeshAndSelectionBoxes(beAnvil);
        beAnvil.MarkDirty();
    }

    private static void FlipHorizontalRotationAttribute(this ItemStack workItemStack, EnumAxis axis)
    {
        var rotationAttribute = axis.HorizontalRotationAttribute();
        var rotation = workItemStack.Attributes.GetInt(rotationAttribute);
        rotation = (rotation + 180) % 360;
        workItemStack.Attributes.SetInt(rotationAttribute, rotation);
    }
    
    private static void ResolveRotations(this BlockEntityAnvil beAnvil)
    {
        if (beAnvil.Api.World.Side != EnumAppSide.Server) return;
        var workItemStack = beAnvil.WorkItemStack;
        var rotationX = workItemStack.Attributes.GetInt(ModAttributes.RotationX);
        var rotationZ = workItemStack.Attributes.GetInt(ModAttributes.RotationZ);
        if (rotationX % 360 != 180 || rotationZ % 360 != 180) return;
        var rotationY = beAnvil.rotation;
        rotationY = (rotationY + 180) % 360;
        workItemStack.Attributes.SetInt(ModAttributes.RotationX, 0);
        workItemStack.Attributes.SetInt(ModAttributes.RotationZ, 0);
        beAnvil.rotation = rotationY;
    }
    
    private static EnumAxis? GetHorizontalRotationAxis(this ItemStack workItemStack)
    {
        var rotationX = workItemStack.Attributes.GetInt(ModAttributes.RotationX);
        var rotationZ = workItemStack.Attributes.GetInt(ModAttributes.RotationZ);
        if ((rotationX != 0 && rotationZ != 0 ) ||
            (rotationX % 360 == 0 && rotationZ % 360 == 0))
            return null;
        return rotationX % 360 == 0 ? EnumAxis.Z : EnumAxis.X;
    }

    private static string HorizontalRotationAttribute(this EnumAxis axis) => 
        axis switch
        {
            EnumAxis.X => ModAttributes.RotationX,
            EnumAxis.Z => ModAttributes.RotationZ,
            EnumAxis.Y => throw new ArgumentException("Axis Y rotations are not horizontal."),
            _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
        };
    
    private static int GetHorizontalRotation(this ItemStack workItemStack, EnumAxis axis)
    {
        var rotationAttribute = axis.HorizontalRotationAttribute();
        return workItemStack.Attributes.GetInt(rotationAttribute);
    }
    
    // Allow to override and store the minY of rotation to reuse when rotating the mesh
    private static byte[,,] RotateAroundAxis(this byte[,,] voxels, EnumAxis axis, ref int? minY)
    {
        if ((int) axis > 2)
            throw new ArgumentOutOfRangeException(nameof(axis), axis, "Axis must be X, Y, or Z.");
        var rotatedVoxels = new byte[16, 6, 16];
        // Perform normal rotation around the Y axis. (From BlockEntityAnvil.RotateWorkItem)
        if (axis == EnumAxis.Y)
        {
            for (var index1 = 0; index1 < 16; ++index1)
            {
                for (var index2 = 0; index2 < 6; ++index2)
                {
                    for (var index3 = 0; index3 < 16; ++index3)
                        rotatedVoxels[index3, index2, index1] = voxels[16 - index1 - 1, index2, index3];
                }
            }
            return rotatedVoxels;
        }
        // Temporary list to hold rotated voxel info.
        var rotatedList = new List<(int X, int Y, int Z, byte Value)>();
        // Compute center of mass for all nonzero voxels.
        var center = new Vec3f { X = 7.5f, Y = 2.5f, Z = 7.5f };
        for (var x = 0; x < 16; x++)
        {
            for (var y = 0; y < 6; y++)
            {
                for (var z = 0; z < 16; z++)
                {
                    if (axis == EnumAxis.X) // Rotate around X axis (in the Y-Z plane).
                    {
                        var value = voxels[x, y, z];
                        if (value == 0) continue;
                        var newY = (int)Math.Round(2 * center.Y - y);
                        var newZ = (int)Math.Round(2 * center.Z - z);
                        rotatedList.Add((x, newY, newZ, value));
                    }
                    else // Rotate around Z axis (in the X-Y plane).
                    {
                        var value = voxels[x, y, z];
                        if (value == 0) continue;
                        var newX = (int)Math.Round(2 * center.X - x);
                        var newY = (int)Math.Round(2 * center.Y - y);
                        rotatedList.Add((newX, newY, z, value));
                    }
                }
            }
        }
        // Determine the min Y value among rotated voxels.
        var minRotY = minY ??= rotatedList.Select(point => point.Y).Prepend(int.MaxValue).Min();
        // Offset all Y's so that the lowest voxel is at y=0.
        foreach (var (x, y, z, value) in rotatedList)
        {
            var finalY = y - minRotY;
            if (x is >= 0 and < 16 && finalY is >= 0 and < 6 && z is >= 0 and < 16)
            {
                rotatedVoxels[x, finalY, z] = value;
            }
        }
        return rotatedVoxels;
    }
}
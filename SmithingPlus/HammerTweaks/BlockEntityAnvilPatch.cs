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
    
    [HarmonyPatch("recipeVoxels", MethodType.Getter)]
    public static void BlockEntityAnvil_recipeVoxels_Patch(BlockEntityAnvil __instance, ref bool[,,] __result)
    {
        var rotationAxis = __instance.WorkItemStack.GetHorizontalRotationAxis();
        if (rotationAxis == EnumAxis.Y) return;
        __result = __result.ToByteArray().RotateAroundAxis(rotationAxis).ToBoolArray();
    }

    private static bool RotateWorkItem(this BlockEntityAnvil beAnvil, bool ccw)
    {
        var rotatedVoxels = RotateAroundAxis(beAnvil.Voxels, EnumAxis.Y);
        if (ccw) rotatedVoxels = rotatedVoxels.RotateAroundAxis(EnumAxis.Y);
        beAnvil.rotation = (beAnvil.rotation + 90) % 360;
        beAnvil.Voxels = rotatedVoxels;
        beAnvil.ResolveRotations();
        RegenMeshAndSelectionBoxes(beAnvil);
        beAnvil.MarkDirty();
        return true;
    }

    private static void FlipWorkItem(this BlockEntityAnvil beAnvil, ItemStack workItemStack, EnumAxis axis)
    {
        if (axis == EnumAxis.Y) throw new ArgumentException("Axis Y is not supported for flipping.");
        if (!HasAnyMetalVoxel(beAnvil)) return;
        var rotatedVoxels = beAnvil.Voxels.RotateAroundAxis(axis);
        beAnvil.Voxels = rotatedVoxels;
        workItemStack.FlipHorizontalRotationAttribute(axis);
        beAnvil.ResolveRotations();
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
    
    private static void ResolveRotations(this ItemStack workItemStack)
    {
        var rotationX = workItemStack.Attributes.GetInt(ModAttributes.RotationX);
        var rotationZ = workItemStack.Attributes.GetInt(ModAttributes.RotationZ);
        if (rotationX % 360 != 180 || rotationZ % 360 != 180) return;
        var rotationY = workItemStack.Attributes.GetInt("rotation");
        rotationY = (rotationY + 180) % 360;
        workItemStack.Attributes.SetInt(ModAttributes.RotationX, 0);
        workItemStack.Attributes.SetInt(ModAttributes.RotationZ, 0);
        workItemStack.Attributes.SetInt("rotation", rotationY);
    }
    
    private static void ResolveRotations(this BlockEntityAnvil beAnvil)
    {
        if (beAnvil.Api.World.Side != EnumAppSide.Server) return;
        var workItemStack = beAnvil.WorkItemStack;
        ResolveRotations(workItemStack);
        beAnvil.rotation = workItemStack.Attributes.GetInt("rotation");
    }
    
    private static EnumAxis GetHorizontalRotationAxis(this ItemStack workItemStack)
    {
        var rotationX = workItemStack.Attributes.GetInt(ModAttributes.RotationX);
        var rotationZ = workItemStack.Attributes.GetInt(ModAttributes.RotationZ);
        if (rotationX != 0 && rotationZ != 0)
            throw new ArgumentException("Both X and Z rotations are set. Cannot determine horizontal axis. Resolve rotations first.");
        if (rotationX % 360 == 0 && rotationZ % 360 == 0) return EnumAxis.Y;
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

    private static byte[,,] RotateAroundAxis(this byte[,,] voxels, EnumAxis axis)
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
        CalcCoM(voxels, out var centerX, out var centerY, out var centerZ);
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
                        var newY = (int)Math.Round(2 * centerY - y);
                        var newZ = (int)Math.Round(2 * centerZ - z);
                        rotatedList.Add((x, newY, newZ, value));
                    }
                    else // Rotate around Z axis (in the X-Y plane).
                    {
                        var value = voxels[x, y, z];
                        if (value == 0) continue;
                        var newX = (int)Math.Round(2 * centerX - x);
                        var newY = (int)Math.Round(2 * centerY - y);
                        rotatedList.Add((newX, newY, z, value));
                    }
                }
            }
        }
        // Determine the min Y value among rotated voxels.
        var minRotY = rotatedList.Select(point => point.Y).Prepend(int.MaxValue).Min();
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

    /// <summary>
    /// Calculates the center of mass (CoM) of the voxels in the anvil.
    /// </summary>
    /// <param name="voxels">The voxel data.</param>
    /// <param name="centerX">Output parameter for the X coordinate of the CoM.</param>
    /// <param name="centerY">Output parameter for the Y coordinate of the CoM.</param>
    /// <param name="centerZ">Output parameter for the Z coordinate of the CoM.</param>
    /// <returns>True if the CoM was successfully calculated, false if there are no voxels.</returns>
    private static bool CalcCoM(byte[,,] voxels, out double centerX, out double centerY, out double centerZ)
    {
        double sumX = 0, sumY = 0, sumZ = 0;
        var count = 0;
        for (var x = 0; x < 16; x++)
        {
            for (var y = 0; y < 6; y++)
            {
                for (var z = 0; z < 16; z++)
                {
                    if (voxels[x, y, z] == 0) continue;
                    sumX += x;
                    sumY += y;
                    sumZ += z;
                    count++;
                }
            }
        }

        if (count == 0)
        {
            centerX = 0;
            centerY = 0;
            centerZ = 0;
            return false;
        }

        centerX = sumX / count;
        centerY = sumY / count;
        centerZ = sumZ / count;
        return true;
    }
}
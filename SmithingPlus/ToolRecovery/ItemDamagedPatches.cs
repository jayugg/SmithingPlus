using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SmithingPlus.Compat;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace SmithingPlus.ToolRecovery;

[HarmonyPatch(typeof (CollectibleObject))]
public class ItemDamagedPatches
{
    [HarmonyPrepare]
    private static bool DeduplicatePatching(MethodBase original, Harmony harmony)
    {
        if (original != null)
        {
            foreach (MethodBase patchedMethod in harmony.GetPatchedMethods())
            {
                if (patchedMethod.Name == original.Name)
                  return false;
            }
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("DamageItem")]
    private static void Prefix_DamageItem(
      IWorldAccessor world,
      Entity byEntity,
      ItemSlot itemslot,
      int amount,
      CollectibleObject __instance)
    {
        if (world.Api.Side.IsClient())
          return;
        Core.Logger.Warning("Prefix_DamageItem: {0} by {1}", __instance.Code, amount);
        world.Api.Logger.VerboseDebug("Prefix_DamageItem: {0} by {1}", __instance.Code, amount);
        world.Api.Logger.VerboseDebug("InventoryID: {0}, Class: {1}", itemslot?.Inventory?.InventoryID, itemslot?.Inventory?.ClassName);
        world.Api.Logger.VerboseDebug("Tool has durability: {0}", itemslot?.Itemstack.GetDurability());
        int? durability = itemslot?.Itemstack.GetDurability();
        if (itemslot?.Inventory == null) return;
        if (byEntity is not EntityPlayer entityPlayer)
          return;
        Core.Logger.Warning("Player: {0}", entityPlayer.Player.PlayerName);
        var toolRecipe = world.GridRecipes
            .FirstOrDefault(r =>
                r.Ingredients.Keys.Count == 2 &&
                r.Output.ResolvedItemstack.Collectible.Code.Equals(itemslot.Itemstack.Collectible.Code));
        if (toolRecipe == null)
        {
            return;
        }
        var toolHead = toolRecipe.Ingredients.Values.FirstOrDefault(k => k.Code.Path.Contains("head"))?.ResolvedItemstack;
        if (toolHead == null)
        {
            return;
        }

        Core.Logger.Warning("Tool head: {0}", toolHead);

        var smithingRecipe = world.Api.ModLoader
            .GetModSystem<RecipeRegistrySystem>()
            .SmithingRecipes
            .FirstOrDefault(r => r.Output.ResolvedItemstack.Collectible.Code.Equals(toolHead.Collectible.Code));

        if (smithingRecipe == null)
        {
            return;
        }

        var workItem = world.GetItem(new AssetLocation("game:workitem-copper"))
            .CodeWithVariant("metal", toolHead.Collectible.Variant["metal"]);

        if (workItem == null)
        {
            return;
        }
        Core.Logger.Warning("Work item: {0}", workItem);
        var wItemStack = new ItemStack(world.GetItem(workItem));
        Core.Logger.Warning("Smithing recipe: {0}", smithingRecipe.Output.ResolvedItemstack.Collectible.Code);
        var voxelCount = smithingRecipe.Voxels.Cast<bool>().Count(voxel => voxel);
        wItemStack.Attributes.SetBytes("voxels", BlockEntityAnvil.serializeVoxels(ByteVoxelsFromRecipe(smithingRecipe, world)));
        wItemStack.Attributes.SetInt("selectedRecipeId", smithingRecipe.RecipeId);
        wItemStack.AddToCustomWorkData(voxelCount);
        entityPlayer.TryGiveItemStack(wItemStack);
    }
    
    public static byte[,,] ByteVoxelsFromRecipe(SmithingRecipe recipe, IWorldAccessor world)
    {
        var random = new Random(world.Rand.Next());
        var recipeVoxels = recipe.Voxels;
        var byteVoxels = new byte[recipeVoxels.GetLength(0), recipeVoxels.GetLength(1), recipeVoxels.GetLength(2)];
        for (int x = 0; x < recipeVoxels.GetLength(0); x++)
        {
            for (int y = 0; y < recipeVoxels.GetLength(1); y++)
            {
                for (int z = 0; z < recipeVoxels.GetLength(2); z++)
                {
                    if (random.NextDouble() < 0.7)
                    {
                        byteVoxels[x, y, z] = recipeVoxels[x, y, z] ? (byte)1 : (byte)0;
                    }
                }
            }
        }
        return byteVoxels;
    }
}
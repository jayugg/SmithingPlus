#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SmithingPlus.Metal;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace SmithingPlus.CastingTweaks;

public class CollectibleBehaviorCastToolHead : CollectibleBehavior, IAnvilWorkable
{
    public CollectibleBehaviorCastToolHead(CollectibleObject collObj) : base(collObj)
    {
        Api = collObj.GetField<ICoreAPI>("api");
    }

    private ICoreAPI Api { get; set; }

    public int GetRequiredAnvilTier(ItemStack stack)
    {
        return stack.GetOrCacheMetalMaterial(Api)?.Tier ?? 0;
    }

    public List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
    {
        var smithingRecipe = stack.GetSmithingRecipe(Api);
        return smithingRecipe != null ? [smithingRecipe] : [];
    }

    public bool CanWork(ItemStack stack)
    {
        if (!stack.IsCastTool())
            return false;
        var temperature = stack.Collectible.GetTemperature(Api.World, stack);
        var threshold = GetWorkableTemperature(stack);
        Core.Logger.VerboseDebug(
            $"[CollectibleBehaviorCastToolHead#CanWork] {stack.Collectible.Code} - Temperature: {temperature}, Threshold: {threshold}");
        return temperature >= threshold;
    }

    public ItemStack? TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil)
    {
        if (beAnvil.WorkItemStack != null || !CanWork(stack))
            return null;
        var recipe = stack.GetSingleSmithingRecipe(Api);
        if (recipe == null) return null;
        var voxels = recipe.Voxels.ToByteArray();
        var world = beAnvil.Api.World;
        var random = world.Rand;
        var slagCount = (int)Math.Ceiling(0.2f * voxels.MaterialCount());
        voxels.AddSlag(slagCount, random);
        var workItemStack = stack.GetOrCacheMetalMaterial(beAnvil.Api)?.WorkItemStack;
        if (workItemStack == null)
            return null;
        beAnvil.Voxels = voxels;
        beAnvil.SelectedRecipeId = recipe.RecipeId;
        var temperature = stack.Collectible.GetTemperature(world, stack);
        workItemStack.Collectible.SetTemperature(world, workItemStack, temperature);
        return workItemStack;
    }

    public ItemStack? GetBaseMaterial(ItemStack stack)
    {
        var metalMaterial = stack.GetOrCacheMetalMaterial(Api);
        Debug.Write(
            $"[CollectibleBehaviorCastToolHead#GetBaseMaterial] {stack.Collectible.Code} -> {metalMaterial?.IngotCode}");
        return metalMaterial?.IngotStack;
    }

    public EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil)
    {
        return EnumHelveWorkableMode.TestSufficientVoxelsWorkable;
    }

    public int VoxelCountForHandbook(ItemStack stack)
    {
        var recipe = stack.GetSingleSmithingRecipe(Api);
        var voxels = recipe?.Voxels.ToByteArray();
        return voxels?.MaterialCount() ?? 0;
    }

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        Api = api;
    }

    public override void GetHeldItemName(StringBuilder dsc, ItemStack itemStack)
    {
        base.GetHeldItemName(dsc, itemStack);
        if (!itemStack.IsCastTool())
            return;
        var toolName = dsc.ToString();
        dsc.Clear();
        dsc.AppendLine(Lang.Get("Cast {0}", toolName.ToLower()));
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        if (!inSlot.Itemstack.IsCastTool())
            return;
        dsc.AppendLine(Lang.Get($"{Core.ModId}:setting-casttooldurabilitypenalty") +
                       $": {100 * Core.Config.CastToolDurabilityPenalty}%");
        dsc.AppendLine(Lang.Get($"{Core.ModId}:itemdesc-needsrefining"));
        var workableTemp = GetWorkableTemperature(inSlot.Itemstack);
        var temperature = inSlot.Itemstack?.Collectible.GetTemperature(world, inSlot.Itemstack);
        dsc.AppendLine(Lang.Get("Workable Temperature: {0}",
            workableTemp > 0
                ? temperature > workableTemp
                    ? $"<font color=\"{Constants.AnvilWorkableColor}\">{Math.Round(workableTemp)}\u00B0C</font>"
                    : $"{Math.Round(workableTemp)}\u00B0C"
                : Lang.Get($"{Core.ModId}:itemdesc-temp-always")));
    }

    public float GetWorkableTemperature(ItemStack itemStack)
    {
        var metalIngot = itemStack.GetOrCacheMetalMaterial(Api)?.IngotItem;
        var querySlot = new DummySlot(itemStack);

        var meltingPoint = metalIngot?
                               .GetMeltingPoint(Api.World, null, querySlot)
                           ?? itemStack.Collectible
                               .GetMeltingPoint(Api.World, null, querySlot);

        var defaultWorkableTemp = meltingPoint / 2f;
        var workableAttr = metalIngot?.Attributes?["workableTemperature"];

        return workableAttr?.Exists == true
            ? workableAttr.AsFloat(defaultWorkableTemp)
            : defaultWorkableTemp;
    }
}
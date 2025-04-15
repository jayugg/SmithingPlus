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

public class CollectibleBehaviorCastToolHead(CollectibleObject collObj) : CollectibleBehavior(collObj), IAnvilWorkable
{
    private ICoreAPI Api { get; set; } = collObj.GetField<ICoreAPI>("api");

    public int GetRequiredAnvilTier(ItemStack stack)
    {
        return stack.GetOrCacheMetalMaterial(Api)?.Tier ?? 0;
    }

    public List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
    {
        return [stack.GetSmithingRecipe(Api)];
    }

    public bool CanWork(ItemStack stack)
    {
        if (!stack.IsCastTool())
            return false;
        var temperature = stack.Collectible.GetTemperature(Api.World, stack);
        var meltingPoint = stack.Collectible.GetMeltingPoint(Api.World, null, new DummySlot(stack));
        var attributes = stack.Collectible.Attributes;
        return (attributes != null ? attributes["workableTemperature"].Exists ? 1 : 0 : 0) != 0
            ? stack.Collectible.Attributes["workableTemperature"].AsFloat(meltingPoint / 2f) <= (double)temperature
            : temperature >= meltingPoint / 2.0;
    }

    public ItemStack? TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil)
    {
        if (beAnvil.WorkItemStack != null)
            return null;
        var recipe = stack.GetSmithingRecipe(Api);
        var voxels = recipe.Voxels.ToByteArray();
        var random = beAnvil.Api.World.Rand;
        var slagCount = (int)Math.Ceiling(0.2f * voxels.MaterialCount());
        voxels.AddSlag(slagCount, random);
        beAnvil.Voxels = voxels;
        beAnvil.SelectedRecipeId = recipe.RecipeId;
        return stack.GetOrCacheMetalMaterial(Api)?.WorkItemStack;
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
    }
}
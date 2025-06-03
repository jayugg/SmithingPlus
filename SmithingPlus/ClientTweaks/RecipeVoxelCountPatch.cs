using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SmithingPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Delegate = System.Delegate;

namespace SmithingPlus.ClientTweaks;

[UsedImplicitly]
[HarmonyPatchCategory(Core.ClientTweaksCategories.AnvilShowRecipeVoxels)]
public static class RecipeVoxelCountPatch
{
    private static List<SmithingRecipe> _selectedRecipes = new();

    [HarmonyPostfix, HarmonyPatch(typeof(BlockEntityAnvil), "OpenDialog")]
    public static void OpenDialog_Postfix(BlockEntityAnvil __instance, ItemStack ingredient)
    {
        if (Core.Api.Side != EnumAppSide.Client) return;
        if (__instance == null || ingredient == null) return;
        var recipes = (ingredient.Collectible as IAnvilWorkable)?.GetMatchingRecipes(ingredient);
        if (recipes != null) _selectedRecipes = recipes;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GuiDialogBlockEntityRecipeSelector), "SetupDialog")]
    public static bool SetupDialog_Prefix(GuiDialogBlockEntityRecipeSelector __instance,
        List<SkillItem> ___skillItems,
        int ___prevSlotOver,
        ICoreClientAPI ___capi,
        BlockPos ___blockEntityPos
        )
    {
        if (_selectedRecipes.Count == 0) return true;
        var cellCount = Math.Max(1, ___skillItems.Count);
        var columns = Math.Min(cellCount, 7);
        var rows = (int) Math.Ceiling(cellCount / (double) columns);
        var slotSize = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
        var fixedWidth = Math.Max(300.0, columns * slotSize);
        var gridBounds = ElementBounds.Fixed(0.0, 30.0, fixedWidth, rows * slotSize);
        var nameBounds = ElementBounds.Fixed(0.0, rows * slotSize + 50.0, fixedWidth, 33.0);
        var descBounds = nameBounds.BelowCopy(fixedDeltaY: 10.0);
        var ingredientDescBounds = descBounds.BelowCopy();
        var richTextBounds = ingredientDescBounds.BelowCopy(fixedDeltaY: -10).WithFixedPadding(0, 20);
        var dialogBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        dialogBounds.BothSizing = ElementSizing.FitToChildren;

        var dialogName = "toolmodeselect" + ___blockEntityPos;
        __instance.SingleComposer = ___capi.Gui.CreateCompo(dialogName, ElementStdBounds.AutosizedMainDialog)
            .AddShadedDialogBG(dialogBounds)
            .AddDialogTitleBar(Lang.Get("Select Recipe"), __instance.OnTitleBarClose())
            .BeginChildElements(dialogBounds)
                .AddSkillItemGrid(___skillItems, columns, rows, __instance.OnSlotClick(), gridBounds, "skillitemgrid")
                .AddDynamicText("", CairoFont.WhiteSmallishText(), nameBounds, "name")
                .AddDynamicText("", CairoFont.WhiteDetailText(), descBounds, "desc")
                .AddDynamicText("", CairoFont.WhiteDetailText(), ingredientDescBounds, "ingredientDesc")
                .AddRichtext("", CairoFont.WhiteDetailText(), richTextBounds, "ingredientCounts")
            .EndChildElements()
            .Compose();
        
        __instance.SingleComposer.GetSkillItemGrid("skillitemgrid").OnSlotOver = OnSlotOver(__instance, ___skillItems, ___prevSlotOver, ___capi);
        _selectedRecipes.Clear();
        return false;
    }
    
    [HarmonyReversePatch, HarmonyPatch(typeof(GuiDialogBlockEntityRecipeSelector), "OnTitleBarClose")]
    private static void OnTitleBarClose_Reverse(GuiDialogBlockEntityRecipeSelector __instance)
    {
        throw new NotImplementedException("Reverse patch stub.");
    }

    private static Action OnTitleBarClose(this GuiDialogBlockEntityRecipeSelector recipeSelector)
    {
        return () => OnTitleBarClose_Reverse(recipeSelector);
    }
    
    [HarmonyReversePatch, HarmonyPatch(typeof(GuiDialogBlockEntityRecipeSelector), "OnSlotClick")]
    private static void OnSlotClick_Reverse(GuiDialogBlockEntityRecipeSelector __instance, int num)
    {
        throw new NotImplementedException("Reverse patch stub.");
    }

    private static Action<int> OnSlotClick(this GuiDialogBlockEntityRecipeSelector recipeSelector)
    {
        return num => OnSlotClick_Reverse(recipeSelector, num);
    }

    private static Action<int> OnSlotOver(GuiDialogBlockEntityRecipeSelector recipeSelector, List<SkillItem> skillItems, int prevSlotOver, ICoreClientAPI capi)
    {
        return num =>
        {
            if (num >= skillItems.Count || num == prevSlotOver || num >= _selectedRecipes.Count)
                return;
            var selectedRecipe = _selectedRecipes[num];
            var recipeId = selectedRecipe.RecipeId;
            var voxelCount = CacheHelper.GetOrAdd(Core.RecipeVoxelCountCache, recipeId,
                () =>  capi.GetSmithingRecipes().Find(recipe => recipe.RecipeId == recipeId).Voxels.VoxelCount());
            var baseMaterial = selectedRecipe.Output.ResolvedItemstack.GetMetalMaterialStack(capi);
            if (baseMaterial == null) return;
            var bitsCount = (int) Math.Ceiling(voxelCount / Core.Config.VoxelsPerBit);
            var countDesc = Lang.Get("Requires any of: ");
            var currentSkillItem = skillItems[num];
            recipeSelector.SingleComposer.GetDynamicText("name").SetNewText(currentSkillItem.Name);
            recipeSelector.SingleComposer.GetDynamicText("desc").SetNewText(currentSkillItem.Description);
            recipeSelector.SingleComposer.GetDynamicText("ingredientDesc").SetNewText(countDesc);
            
            var onStackClickedAction = new Action<ItemStack>(cs => capi.LinkProtocols["handbook"]?.DynamicInvoke(new LinkTextComponent("handbook://" +GuiHandbookItemStackPage.PageCodeForStack(cs))));
            
            var allMaterialStacks = HandbookInfoPatch.GetSmithingIngredientStacks(capi, selectedRecipe.Output.ResolvedItemstack, baseMaterial, voxelCount, bitsCount, recipeId);
            var ingotStackComponents = allMaterialStacks.Select(itemStack =>
                new ItemstackTextComponent(capi, itemStack, 40, 5.0, EnumFloat.Inline, onStackClickedAction) { ShowStacksize = true });
            var stackComponentsText =  VtmlUtil.Richtextify(capi, "", CairoFont.WhiteDetailText()).Concat(ingotStackComponents).ToArray();
            recipeSelector.SingleComposer.GetRichtext("ingredientCounts").SetNewText(stackComponentsText);
        };
    }
}
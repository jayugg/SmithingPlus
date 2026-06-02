#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using SmithingPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SmithingPlus.ClientTweaks;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[HarmonyPatchCategory(Core.ClientTweaksCategories.AnvilShowRecipeVoxels)]
public static class AnvilRecipeSelectorPatch
{
    private static List<SkillItem>? _defaultSkillItemCache;
    
    private struct CustomSkillItemData(object defaultData, int originalIndex)
    {
        public readonly object DefaultData = defaultData;
        public int OriginalIndex = originalIndex;
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GuiDialogBlockEntityRecipeSelector))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch([
        typeof(string),
        typeof(ItemStack[]),
        typeof(Action<int>),
        typeof(Action),
        typeof(BlockPos),
        typeof(ICoreClientAPI)
    ])]
    public static IEnumerable<CodeInstruction> RecipeSelectorCtor_Transpile_SetupDialog(
        IEnumerable<CodeInstruction> instructions)
    {
        var codeInstructions = instructions.ToList();
        // The original target to remove: instance void ...::SetupDialog()
        var setupDialogMethod = AccessTools.Method(
            typeof(GuiDialogBlockEntityRecipeSelector),
            "SetupDialog", []);

        var codeMatcher = new CodeMatcher(codeInstructions)
            .End()
            .MatchEndBackwards(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(ci => ci.Calls(setupDialogMethod))
            );
        if (codeMatcher.IsValid)
            // don't call the original SetupDialog
            codeMatcher.SetOpcodeAndAdvance(OpCodes.Pop);
        return codeMatcher.InstructionEnumeration();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GuiDialogBlockEntityRecipeSelector))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch([
        typeof(string),
        typeof(ItemStack[]),
        typeof(Action<int>),
        typeof(Action),
        typeof(BlockPos),
        typeof(ICoreClientAPI)
    ])]
    public static void CustomSetupDialog(
        string DialogTitle,
        ItemStack[] recipeOutputs,
        Action<int> onSelectedRecipe,
        Action onCancelSelect,
        BlockPos blockEntityPos,
        ICoreClientAPI capi,
        GuiDialogBlockEntityRecipeSelector __instance,
        ref List<SkillItem> ___skillItems,
        int ___prevSlotOver
    )
    {
        _defaultSkillItemCache = ___skillItems;
        var cellCount = Math.Max(1, ___skillItems.Count);
        var columns = Math.Min(cellCount, Core.CConfig?.AnvilRecipeSelectionColumns ?? 8);
        var rows = (int)Math.Ceiling(cellCount / (double)columns);
        var slotSize = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
        var fixedWidth = Math.Max(300.0, columns * slotSize);
        var searchBarBounds = ElementBounds.Fixed(0.0, 30, fixedWidth, 30.0);
        var gridBounds = searchBarBounds.BelowCopy(fixedDeltaY: 10.0).WithFixedWidth(fixedWidth).WithFixedHeight( rows * slotSize);
        var nameBounds = gridBounds.BelowCopy(fixedDeltaY: 10.0).WithFixedHeight(30);
        var descBounds = nameBounds.BelowCopy(fixedDeltaY: 10.0);
        var ingredientDescBounds = descBounds.BelowCopy().WithFixedWidth(descBounds.fixedWidth * 0.5);
        var richTextBounds = ingredientDescBounds.RightCopy()
            .WithAlignment(EnumDialogArea.RightFixed)
            .WithFixedOffset(-50, -20)
            .WithFixedPadding(0, 10);
        var dialogBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        dialogBounds.BothSizing = ElementSizing.FitToChildren;

        var dialogName = "toolmodeselect" + blockEntityPos;
        __instance.SingleComposer = capi.Gui.CreateCompo(dialogName, ElementStdBounds.AutosizedMainDialog)
            .AddShadedDialogBG(dialogBounds)
            .AddDialogTitleBar(Lang.Get("Select Recipe"), __instance.OnTitleBarClose())
            .BeginChildElements(dialogBounds)
            .AddSkillItemGrid(___skillItems, columns, rows, __instance.OnSlotClick(), gridBounds, "skillitemgrid")
            .AddTextInput(searchBarBounds, (text) => OnSearchTextChanged(__instance, text), CairoFont.WhiteSmallishText(), "searchbar")
            .AddDynamicText("", CairoFont.WhiteSmallishText(), nameBounds, "name")
            .AddDynamicText("", CairoFont.WhiteDetailText(), descBounds, "desc")
            .AddDynamicText("", CairoFont.WhiteDetailText(), ingredientDescBounds, "ingredientDesc")
            .AddRichtext("", CairoFont.WhiteDetailText(), richTextBounds, "ingredientCounts")
            .EndChildElements()
            .Compose();
        __instance.SingleComposer.GetTextInput("searchbar").SetPlaceHolderText(Lang.Get("Search..."));
        __instance.SingleComposer.GetSkillItemGrid("skillitemgrid").OnSlotOver = num =>
            OnSlotOver(__instance, ___prevSlotOver, capi, num);
    }

    private static void OnSearchTextChanged(GuiDialogBlockEntityRecipeSelector recipeSelector, string text)
    {
        if (_defaultSkillItemCache == null)
            return;
        for (var index = 0; index < _defaultSkillItemCache.Count; ++index)
        {
            _defaultSkillItemCache[index].Data = new CustomSkillItemData(_defaultSkillItemCache[index].Data, index);
        }
        var searchText = text.ToLowerInvariant();
        var filteredSkillItems = string.IsNullOrWhiteSpace(searchText)
            ? _defaultSkillItemCache
            : _defaultSkillItemCache.Where(si =>
                si.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)).ToList();
        recipeSelector.SingleComposer.GetSkillItemGrid("skillitemgrid").SetField("skillItems", filteredSkillItems);
        recipeSelector.SetField("skillItems", filteredSkillItems);
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(GuiDialogBlockEntityRecipeSelector), "OnTitleBarClose")]
    private static void OnTitleBarClose_Reverse([UsedImplicitly] GuiDialogBlockEntityRecipeSelector __instance)
    {
        throw new NotImplementedException("Reverse patch stub.");
    }

    private static Action OnTitleBarClose(this GuiDialogBlockEntityRecipeSelector recipeSelector)
    {
        return () => OnTitleBarClose_Reverse(recipeSelector);
    }

    private static Action<int> OnSlotClick(this GuiDialogBlockEntityRecipeSelector recipeSelector)
    {
        return num =>
        {
            var onSelectedRecipe = recipeSelector.GetField<Action<int>>("onSelectedRecipe");
            var index = num;
            if (recipeSelector.GetField<List<SkillItem>>("skillItems")[num].Data is CustomSkillItemData customData)
                index = customData.OriginalIndex;
            onSelectedRecipe?.Invoke(index);
            recipeSelector.SetField("didSelect", true);
            recipeSelector.TryClose();
            _defaultSkillItemCache = null;
        };
    }

    private static void OnSlotOver(
        GuiDialogBlockEntityRecipeSelector recipeSelector,
        int prevSlotOver,
        ICoreClientAPI capi,
        int num)
    {
        var skillItems = recipeSelector.GetField<List<SkillItem>>("skillItems");
        if (num >= skillItems.Count || num == prevSlotOver)
            return;
        var currentSkillItem = skillItems[num];
        recipeSelector.SingleComposer.GetDynamicText("name").SetNewText(currentSkillItem.Name);
        recipeSelector.SingleComposer.GetDynamicText("desc").SetNewText(currentSkillItem.Description);

        var text = "";
        ItemStack? materialStack = null;
        if (skillItems[num].Data is ItemStack[] data )
        {
            var stack = data[0];
            var cInterface = stack.Collectible.GetCollectibleInterface<IAnvilWorkable>();
            var recipe = cInterface?.GetMatchingRecipes(stack)[num];
            materialStack = recipe != null ? GetAdjustedIngredientStack(capi, stack, recipe.RecipeId) : stack;
        }
        if (skillItems[num].Data is CustomSkillItemData { DefaultData: ItemStack[] customData })
        {
            var stack = customData[0];
            var cInterface = stack.Collectible.GetCollectibleInterface<IAnvilWorkable>();
            var recipe = cInterface?.GetMatchingRecipes(stack)[num];
            materialStack = recipe != null ? GetAdjustedIngredientStack(capi, stack, recipe.RecipeId) : stack;
        }
        if (materialStack != null){
            text = Lang.Get("recipeselector-requiredcount", materialStack.StackSize,
                materialStack.GetName().ToLower());
            var onStackClickedAction = new Action<ItemStack>(cs =>
                capi.LinkProtocols["handbook"]
                    ?.DynamicInvoke(
                        new LinkTextComponent("handbook://" + GuiHandbookItemStackPage.PageCodeForStack(cs))));
            var ingotStackComponent =
                new ItemstackTextComponent(capi, materialStack, 50, 5.0, EnumFloat.Inline, onStackClickedAction)
                    { ShowStacksize = true };
            var stackComponentsText = VtmlUtil.Richtextify(capi, "", CairoFont.WhiteDetailText())
                .AddToArray(ingotStackComponent).ToArray();
            recipeSelector.SingleComposer.GetRichtext("ingredientCounts").SetNewText(stackComponentsText);
        }
        recipeSelector.SingleComposer.GetDynamicText("ingredientDesc").SetNewText(text);
    }
    
    public static ItemStack GetAdjustedIngredientStack(
        ICoreClientAPI capi,
        ItemStack ingredientStack,
        int recipeId)
    {
        var voxelCount = CacheHelper.GetOrAdd(Core.RecipeVoxelCountCache, recipeId,
                () => capi.GetSmithingRecipes().Find(recipe => recipe.RecipeId == recipeId)?.Voxels.VoxelCount() ?? 0);
        if (voxelCount <= 0)
            return ingredientStack;
        var stackSize = GetStackCount(ingredientStack, voxelCount);
        var adjustedIngredient = ingredientStack.Clone();
        adjustedIngredient.SetTemperature(capi.World, 0);
        adjustedIngredient.StackSize = stackSize;
        return adjustedIngredient;
    }

    private static int GetStackCount(ItemStack stack, int voxelCount)
    {
        var workable = stack.Collectible.GetCollectibleInterface<IAnvilWorkable>();
        return stack.Collectible switch
        {
            _ when workable is not null => CeilDiv(voxelCount, workable.VoxelCountForHandbook(stack)),
            _ => (int)Math.Ceiling(voxelCount * (stack.Collectible.CombustibleProps?.SmeltedRatio ?? 1.0) / 42.0)
        };
    }

    private static int CeilDiv(int numerator, int denominator)
    {
        return (numerator + denominator - 1) / denominator;
    }
}
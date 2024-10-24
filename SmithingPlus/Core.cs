using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus;

[HarmonyPatch]
public class SmithingPlusModSystem : ModSystem
{
    public static ILogger Logger;
    public static string ModId;
    public static ICoreAPI Api;
    public Harmony HarmonyInstance;

    public override void StartPre(ICoreAPI api)
    {
        Logger = Mod.Logger;
        ModId = Mod.Info.ModID;
        Api = api;
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("ItemWorkableNugget", typeof(ItemWorkableNugget));
        HarmonyInstance = new Harmony(ModId);
        HarmonyInstance.PatchAll();
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemIngot), nameof(ItemIngot.GetMatchingRecipes))]
    public static void GetMatchingRecipes_Postfix(ItemIngot __instance, ref List<SmithingRecipe> __result, ItemStack stack)
    {
        __result = Api.GetSmithingRecipes().Where((System.Func<SmithingRecipe, bool>) (
            r => r.Ingredient.SatisfiesAsIngredient(stack)
            && !(r.Ingredient.RecipeAttributes?["nuggetRecipe"]?.AsBool() ?? false)
            )).OrderBy((System.Func<SmithingRecipe, AssetLocation>) (
            r => r.Output.ResolvedItemstack.Collectible.Code)
            ).ToList();
    }
    
    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll();
        base.Dispose();
    }
}
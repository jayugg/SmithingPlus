using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SmithingPlus.BitsRecovery;
using SmithingPlus.CastingTweaks;
using SmithingPlus.ClientTweaks;
using SmithingPlus.Common;
using SmithingPlus.Common.Metal;
using SmithingPlus.Config;
using SmithingPlus.SmithWithBits;
using SmithingPlus.StoneSmithing;
using SmithingPlus.ToolRecovery;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public partial class Core : ModSystem
{
    public const string ModId = "smithingplus";
    public static ILogger Logger { get; private set; }
    public static ICoreAPI Api { get; private set; }
    public static Harmony HarmonyInstance { get; private set; }
    public static ServerConfig Config => ConfigLoader.Config;

    public static Dictionary<AssetLocation, SmithingRecipe> SmithingRecipesByOutputCode { get; } = new();
    public static Dictionary<AssetLocation, List<SmithingRecipe>> SmithingRecipesByIngredientCode { get; } = new();
    public static Dictionary<AssetLocation, List<GridRecipe>> GridRecipesByIngredientCode { get; } = new();

    public override void StartPre(ICoreAPI api)
    {
        Logger = Mod.Logger;
        Api = api;
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterCollectibleBehaviorClass($"{ModId}:JsonAnvilWorkable",
            typeof(CollectibleBehaviorJsonAnvilWorkable));
        api.RegisterCollectibleBehaviorClass($"{ModId}:WorkableNugget", typeof(CollectibleBehaviorWorkableNugget));
        api.RegisterCollectibleBehaviorClass($"{ModId}:RepairableTool", typeof(CollectibleBehaviorRepairableTool));
        api.RegisterCollectibleBehaviorClass($"{ModId}:RepairableToolHead",
            typeof(CollectibleBehaviorRepairableToolHead));
        api.RegisterCollectibleBehaviorClass($"{ModId}:BrokenToolHead", typeof(CollectibleBehaviorBrokenToolHead));
        api.RegisterCollectibleBehaviorClass($"{ModId}:DisplayWorkableTemp",
            typeof(CollectibleBehaviorDisplayWorkableTemp));
        api.RegisterCollectibleBehaviorClass($"{ModId}:ScrapeCrucible", typeof(CollectibleBehaviorScrapeCrucible));
        api.RegisterCollectibleBehaviorClass($"{ModId}:CastToolHead", typeof(CollectibleBehaviorCastToolHead));
        api.RegisterCollectibleBehaviorClass($"{ModId}:SmeltedContainer", typeof(CollectibleBehaviorSmeltedContainer));
        api.RegisterCollectibleBehaviorClass($"{ModId}:RecycledBit", typeof(CollectibleBehaviorRecycledBit));

        api.RegisterEntityBehaviorClass($"{ModId}:RecyclableArrow", typeof(RecyclableArrowBehavior));

        api.RegisterItemClass($"{ModId}:ItemStoneHammer", typeof(ItemStoneHammer));
        api.RegisterBlockEntityClass($"{ModId}:StoneAnvil", typeof(BlockEntityStoneAnvil));

        Patch();
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Event.OnEntitySpawn += AddEntityBehaviors;
        api.Event.OnEntityLoaded += AddEntityBehaviors;
    }

    private static void AddEntityBehaviors(Entity entity)
    {
        if (!Config.ArrowsDropBits || entity is not EntityProjectile projectile) return;
        if (!RecyclableArrowBehavior.IsRecyclableArrow(projectile)) return;
        Logger.VerboseDebug("Adding RecyclableArrowBehavior to {0}", entity.Code);
        entity.AddBehavior(new RecyclableArrowBehavior(entity));
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);

        var recipeRegistry = api.ModLoader.GetModSystem<RecipeRegistrySystem>();
        var recipes = recipeRegistry.SmithingRecipes;
        var ingotCode = new AssetLocation("game:ingot-copper");
        var ingotRecipe = api.Side.IsServer()
            ? recipes.FirstOrDefault(r =>
                r.Ingredient?.Code?.Equals(ingotCode) == true &&
                r.Output.ResolvedItemstack?.Collectible.Code.Equals(ingotCode) == true)
            : null;

        foreach (var collObj in api.World.Collectibles.Where(c => c?.Code != null))
        {
            collObj.AddBehaviorIf<CollectibleBehaviorDisplayWorkableTemp>(
                api.Side == EnumAppSide.Client &&
                Config.ShowWorkableTemperature &&
                collObj.GetCollectibleInterface<IAnvilWorkable>() is not null);
            collObj.AddBehaviorIf<CollectibleBehaviorScrapeCrucible>(Config.RecoverBitsOnSplit &&
                                                                     collObj is ItemChisel);
            collObj.AddBehaviorIf<CollectibleBehaviorSmeltedContainer>(Config.RecoverBitsOnSplit &&
                                                                       collObj is BlockSmeltedContainer);

            if (Config.MetalCastingTweaks && collObj.MatchesToolHeadSelector())
            {
                collObj.AddBehavior<CollectibleBehaviorCastToolHead>();
                collObj.MakeForgeable();
            }

            collObj.AddBehaviorIf<CollectibleBehaviorRecycledBit>(collObj.Code.ToString().Contains("metalbit"));

            if ((collObj.Tool != null || (collObj.IsRepairableTool() && !collObj.MatchesToolHeadSelector())) &&
                collObj.HasMetalMaterialSimple()) collObj.AddBehavior<CollectibleBehaviorRepairableTool>();
            else if (collObj.MatchesToolHeadSelector()) collObj.AddBehavior<CollectibleBehaviorRepairableToolHead>();
            else if (WildcardUtil.Match(Config.WorkItemSelector, collObj.Code.ToString()))
                collObj.AddBehavior<CollectibleBehaviorBrokenToolHead>();

            if (api.Side.IsClient()) continue;
            if (ingotRecipe?.Ingredient == null) continue;
            if (!WildcardUtil.Match(Config.IngotSelector, collObj.Code.ToString())) continue;
            if (recipes.Any(r => r.Ingredient?.Code?.Equals(collObj.Code) == true &&
                                 r.Output.ResolvedItemstack?.Collectible.Code.Equals(collObj.Code) == true)) continue;
            Logger.VerboseDebug($"Adding workable-only ingot recipe for {collObj.Code}");
            var newRecipe = new SmithingRecipe
            {
                Code = new AssetLocation(ModId, collObj.Code.Path + "-to-itself"),
                Name = ingotRecipe.Name,
                Pattern = ingotRecipe.Pattern,
                Voxels = ingotRecipe.Voxels,
                Ingredient = new CraftingRecipeIngredient
                {
                    Type = collObj.ItemClass,
                    Code = collObj.Code,
                    RecipeAttributes = ingotRecipe.Ingredient.RecipeAttributes
                },
                Output = new JsonItemStack
                {
                    Type = collObj.ItemClass,
                    Code = collObj.Code,
                    StackSize = 1
                },
                RecipeId = recipes.Count + 1
            };
            newRecipe.Ingredient.Resolve(api.World, $"[{ModId}] add ingot smithing recipe");
            newRecipe.Output.Resolve(api.World, $"[{ModId}] add ingot smithing recipe");
            recipes.Add(newRecipe);
        }

        SmithingRecipesByOutputCode.Clear();
        foreach (var recipe in recipes)
        {
            var outputCode = recipe.Output?.ResolvedItemstack?.Collectible?.Code;
            if (outputCode != null && !SmithingRecipesByOutputCode.ContainsKey(outputCode))
                SmithingRecipesByOutputCode[outputCode] = recipe;
        }

        SmithingRecipesByIngredientCode.Clear();
        foreach (var recipe in recipes)
        {
            if (recipe.Ingredients == null) continue;
            foreach (var ing in recipe.Ingredients)
            {
                var ingCode = ing?.ResolvedItemStack?.Collectible?.Code;
                if (ingCode == null) continue;
                if (!SmithingRecipesByIngredientCode.TryGetValue(ingCode, out var list))
                {
                    list = new List<SmithingRecipe>();
                    SmithingRecipesByIngredientCode[ingCode] = list;
                }
                list.Add(recipe);
            }
        }

        GridRecipesByIngredientCode.Clear();
        foreach (var recipe in api.World.GridRecipes)
        {
            if (recipe.RecipeIngredients == null) continue;
            foreach (var ing in recipe.RecipeIngredients)
            {
                var ingCode = ing?.ResolvedItemStack?.Collectible?.Code;
                if (ingCode == null) continue;
                if (!GridRecipesByIngredientCode.TryGetValue(ingCode, out var list))
                {
                    list = new List<GridRecipe>();
                    GridRecipesByIngredientCode[ingCode] = list;
                }
                list.Add(recipe);
            }
        }
    }

    private static void Patch()
    {
        if (HarmonyInstance != null) return;
        HarmonyInstance = new Harmony(ModId);
        Logger.VerboseDebug("Patching...");
        AlwaysPatchCategory.PatchIfEnabled(true);
        ToolRecoveryCategory.PatchIfEnabled(Config.EnableToolRecovery);
        SmithingRecipeAttributesPatch.PatchIfEnabled(
            Config.SmithWithBits || Config.BitsTopUp || Config.EnableToolRecovery, HarmonyInstance);
        ClientTweaksCategories.RememberHammerToolMode.PatchIfEnabled(Config.RememberHammerToolMode);
        ClientTweaksCategories.AnvilShowRecipeVoxels.PatchIfEnabled(Config.AnvilShowRecipeVoxels);
        ClientTweaksCategories.ShowWorkablePatches.PatchIfEnabled(Config.ShowWorkableTemperature);
        ClientTweaksCategories.HandbookExtraInfo.PatchIfEnabled(Config.HandbookExtraInfo);
        BitsRecoveryCategory.PatchIfEnabled(Config.RecoverBitsOnSplit);
        HelveHammerBitsRecoveryCategory.PatchIfEnabled(Config.HelveHammerBitsRecovery);
        CastingTweaksCategory.PatchIfEnabled(Config.MetalCastingTweaks);
        DynamicMoldsCategory.PatchIfEnabled(Config.DynamicMoldUnits);
        BitSmithingCategory.PatchIfEnabled(Config.SmithWithBits || Config.BitsTopUp);
        HammerTweaksCategory.PatchIfEnabled(Config.HammerTweaks);
        //StoneSmithingCategory.PatchIfEnabled(true);
    }

    private static void Unpatch()
    {
        Logger?.VerboseDebug("Unpatching...");
        HarmonyInstance?.UnpatchAll(ModId);
        HarmonyInstance = null;
    }

    public override void Dispose()
    {
        Unpatch();
        Logger = null;
        Api = null;
        base.Dispose();
    }
}
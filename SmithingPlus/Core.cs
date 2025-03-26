using System.Linq;
using HarmonyLib;
using SmithingPlus.BitsRecovery;
using SmithingPlus.ClientTweaks;
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

public partial class Core : ModSystem
{
    public static ILogger Logger { get; private set; }
    public static string ModId { get; private set; }
    public static ICoreAPI Api { get; private set; }
    public static Harmony HarmonyInstance { get; private set; }
    public static ServerConfig Config => ConfigLoader.Config;
    public object ModInventoryLock = new();

    public override void StartPre(ICoreAPI api)
    {
        Logger = Mod.Logger;
        ModId = Mod.Info.ModID;
        Api = api;
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("ItemWorkableNugget", typeof(ItemWorkableNugget));
        api.RegisterItemClass("ItemWorkableRod", typeof(ItemWorkableRod));
        api.RegisterCollectibleBehaviorClass($"{ModId}:RepairableTool", typeof(CollectibleBehaviorRepairableTool));
        api.RegisterCollectibleBehaviorClass($"{ModId}:RepairableToolHead",
            typeof(CollectibleBehaviorRepairableToolHead));
        api.RegisterCollectibleBehaviorClass($"{ModId}:BrokenToolHead", typeof(CollectibleBehaviorBrokenToolHead));
        api.RegisterCollectibleBehaviorClass($"{ModId}:AnvilWorkable", typeof(CollectibleBehaviorAnvilWorkable));
        api.RegisterCollectibleBehaviorClass($"{ModId}:ScrapeCrucible", typeof(CollectibleBehaviorScrapeCrucible));
        api.RegisterCollectibleBehaviorClass($"{ModId}:SmeltedContainer", typeof(CollectibleBehaviorSmeltedContainer));
        api.RegisterEntityBehaviorClass($"{ModId}:RecyclableArrow", typeof(RecyclableArrowBehavior));
        api.RegisterItemClass($"{ModId}:ItemStoneHammer", typeof(ItemStoneHammer));
        api.RegisterBlockEntityClass($"{ModId}:StoneAnvil", typeof(BlockEntityStoneAnvil));
        if (api.ModLoader.IsModEnabled("xskills"))
            api.RegisterItemClass("ItemXWorkableNugget", typeof(ItemXWorkableNugget));
        Patch();
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Event.OnEntitySpawn += AddEntityBehaviors;
        api.Event.OnEntityLoaded += AddEntityBehaviors;
        RegisterServerCommands(api);
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
        if (api.Side.IsClient()) return;

        var ingotCode = new AssetLocation("game:ingot-copper");
        var ingotRecipe = api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes
            .FirstOrDefault(r =>
                r.Ingredient.Code.Equals(ingotCode) && r.Output.ResolvedItemstack.Collectible.Code.Equals(ingotCode));

        foreach (var collObj in api.World.Collectibles.Where(c => c?.Code != null))
        {
            collObj.AddBehaviorIf<CollectibleBehaviorAnvilWorkable>(Config.ShowWorkableTemperature && collObj is IAnvilWorkable);
            collObj.AddBehaviorIf<CollectibleBehaviorScrapeCrucible>(Config.RecoverBitsOnSplit && collObj is ItemChisel);
            collObj.AddBehaviorIf<CollectibleBehaviorSmeltedContainer>(Config.RecoverBitsOnSplit && collObj is BlockSmeltedContainer);
            if ((collObj.Tool != null || (collObj.IsRepairableTool() && !collObj.IsRepairableToolHead())) &&
                collObj.HasMetalMaterial(api)) collObj.AddBehavior<CollectibleBehaviorRepairableTool>();
            else if (collObj.IsRepairableToolHead()) collObj.AddBehavior<CollectibleBehaviorRepairableToolHead>();
            else if (WildcardUtil.Match(Config.WorkItemSelector, collObj.Code.ToString()))
                collObj.AddBehavior<CollectibleBehaviorBrokenToolHead>();

            if (ingotRecipe == null) continue;
            if (!WildcardUtil.Match(Config.IngotSelector, collObj.Code.ToString())) continue;
            if (api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes
                .Any(r => r.Ingredient.Code.Equals(collObj.Code) &&
                          r.Output.ResolvedItemstack.Collectible.Code.Equals(collObj.Code))) continue;
            Logger.VerboseDebug($"Adding Metalbit-only recipes to {collObj.Code}");
            var newRecipe = ingotRecipe.Clone();
            newRecipe.Ingredient.Code = collObj.Code;
            newRecipe.Output.Code = collObj.Code;
            newRecipe.Output.Resolve(api.World, $"[{ModId}] add ingot smithing recipe");
            api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes.Add(newRecipe);
        }
    }

    private static void Patch()
    {
        if (HarmonyInstance != null) return;
        HarmonyInstance = new Harmony(ModId);
        Logger.VerboseDebug("Patching...");

        ToolRecoveryCategory.PatchIfEnabled(Config.EnableToolRecovery);
        ClientTweaksCategories.RememberHammerToolMode.PatchIfEnabled(Config.RememberHammerToolMode);
        ClientTweaksCategories.AnvilShowRecipeVoxels.PatchIfEnabled(Config.AnvilShowRecipeVoxels);
        ClientTweaksCategories.ShowWorkablePatches.PatchIfEnabled(Config.ShowWorkableTemperature);
        ClientTweaksCategories.HandbookExtraInfo.PatchIfEnabled(Config.HandbookExtraInfo);
        BitsRecoveryCategory.PatchIfEnabled(Config.RecoverBitsOnSplit);
        CastingTweaksCategory.PatchIfEnabled(Config.MetalCastingTweaks);
        SmithingBitsCategory.PatchIfEnabled(Config.SmithWithBits || Config.BitsTopUp);
        // StoneSmithingCategory.PatchIfEnabled(Config.StoneSmithing);
    }

    private static void Unpatch()
    {
        Logger?.VerboseDebug("Unpatching...");
        HarmonyInstance?.UnpatchAll();
        HarmonyInstance = null;
    }

    public override void Dispose()
    {
        Unpatch();
        ClearCache();
        Logger = null;
        ModId = null;
        Api = null;
        base.Dispose();
    }
}
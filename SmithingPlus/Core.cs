using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SmithingPlus.ClientTweaks;
using SmithingPlus.Config;
using SmithingPlus.Extra;
using SmithingPlus.SmithWithBits;
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
    public static ILogger Logger;
    public static string ModId;
    public static ICoreAPI Api;
    public static Harmony HarmonyInstance;
    public static ServerConfig Config;

    public static Dictionary<int, string> RecipeOutputNameCache => ObjectCacheUtil.GetOrCreate(Api, RecipeOutputNameCacheKey, () => new Dictionary<int, string>());
    public static Dictionary<string, SmithingRecipe> ToolToRecipeCache => ObjectCacheUtil.GetOrCreate(Api, ToolToRecipeCacheKey, () => new Dictionary<string, SmithingRecipe>());
    public static Dictionary<int, int> RecipeVoxelCountCache => ObjectCacheUtil.GetOrCreate(Core.Api, RecipeVoxelCountCacheKey, () => new Dictionary<int, int>());
    public override void StartPre(ICoreAPI api)
    {
        Logger = Mod.Logger;
        ModId = Mod.Info.ModID;
        Api = api;
        Config = ConfigSystem.Config;
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("ItemWorkableNugget", typeof(ItemWorkableNugget));
        api.RegisterItemClass("ItemWorkableRod", typeof(ItemWorkableRod));
        api.RegisterCollectibleBehaviorClass($"{ModId}:RepairableTool", typeof(CollectibleBehaviorRepairableTool));
        api.RegisterCollectibleBehaviorClass($"{ModId}:RepairableToolHead", typeof(CollectibleBehaviorRepairableToolHead));
        api.RegisterCollectibleBehaviorClass($"{ModId}:BrokenToolHead", typeof(CollectibleBehaviorBrokenToolHead));
        api.RegisterCollectibleBehaviorClass($"{ModId}:AnvilWorkable", typeof(CollectibleBehaviorAnvilWorkable));
        api.RegisterEntityBehaviorClass($"{ModId}:RecyclableArrow", typeof(RecyclableArrowBehavior));
        api.RegisterItemClass($"{ModId}:ItemStoneHammer", typeof(ItemStoneHammer));
        api.RegisterBlockEntityClass($"{ModId}:StoneAnvil", typeof(BlockEntityStoneAnvil));
        if (api.ModLoader.IsModEnabled("xskills")) api.RegisterItemClass("ItemXWorkableNugget", typeof(ItemXWorkableNugget));
        Patch();
    }
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Event.OnEntitySpawn += AddEntityBehaviors;
        api.Event.OnEntityLoaded += AddEntityBehaviors;
        RegisterServerCommands(api);
    }
    
    private void AddEntityBehaviors(Entity entity)
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
        SmithingRecipe ingotRecipe = api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes
            .FirstOrDefault(r =>
                r.Ingredient.Code.Equals(ingotCode) && r.Output.ResolvedItemstack.Collectible.Code.Equals(ingotCode));
        
        foreach (var collObj in api.World.Collectibles.Where(c => c?.Code != null))
        {
            if (Config.ShowWorkableTemperature && collObj is IAnvilWorkable) collObj.AddBehavior<CollectibleBehaviorAnvilWorkable>();
            
            if ((collObj.Tool != null || collObj.IsRepairableTool() && !collObj.IsRepairableToolHead()) && collObj.HasMetalMaterial(api)) collObj.AddBehavior<CollectibleBehaviorRepairableTool>();
            else if (collObj.IsRepairableToolHead()) collObj.AddBehavior<CollectibleBehaviorRepairableToolHead>();
            else if (WildcardUtil.Match(Config.WorkItemSelector, collObj.Code.ToString())) collObj.AddBehavior<CollectibleBehaviorBrokenToolHead>();
            
            if (ingotRecipe == null) continue;
            if (!WildcardUtil.Match(Config.IngotSelector, collObj.Code.ToString())) continue;
            if (api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes
                .Any(r => r.Ingredient.Code.Equals(collObj.Code) && r.Output.ResolvedItemstack.Collectible.Code.Equals(collObj.Code))) continue;
            Logger.VerboseDebug($"Adding Metalbit-only recipes to {collObj.Code}");
            var newRecipe = ingotRecipe.Clone();
            newRecipe.Ingredient.Code = collObj.Code;
            newRecipe.Output.Code = collObj.Code;
            newRecipe.Output.Resolve(api.World, $"[{ModId}] add ingot smithing recipe");
            api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes.Add(newRecipe);
        }
    }

    public static void Patch()
    {
        if (HarmonyInstance != null) return;
        HarmonyInstance = new Harmony(ModId);
        Logger.VerboseDebug("Patching...");
        if (Config.EnableToolRecovery)
        {
            HarmonyInstance.PatchCategory(ToolRecoveryCategory);
            Logger.VerboseDebug("Patched ToolRecovery...");
        }
        
        /*
        if (Config.StoneSmithing)
        {
            HarmonyInstance.PatchCategory(StoneSmithingCategory);
            Logger.VerboseDebug("Patched StoneSmithing...");
        }
        */
        
        if (Config.RememberHammerToolMode)
        {
            HarmonyInstance.PatchCategory(ClientTweaksCategories.RememberHammerToolMode);
            Logger.VerboseDebug("Patched RememberHammerToolMode...");
        }
        if (Config.AnvilShowRecipeVoxels)
        {
            HarmonyInstance.PatchCategory(ClientTweaksCategories.AnvilShowRecipeVoxels);
            Logger.VerboseDebug("Patched AnvilShowRecipeVoxels...");
        }
        
        if (Config.ShowWorkableTemperature)
        {
            HarmonyInstance.PatchCategory(ClientTweaksCategories.ShowWorkablePatches);
            Logger.VerboseDebug("Patched ShowWorkableTemperature...");
        }
        
        if (Config.HandbookExtraInfo)
        {
            HarmonyInstance.PatchCategory(ClientTweaksCategories.HandbookExtraInfo);
            Logger.VerboseDebug("Patched HandbookExtraInfo...");
        }
        
        if (Config.RecoverBitsOnSplit)
        {
            HarmonyInstance.PatchCategory(BitsRecoveryCategory);
            Logger.VerboseDebug("Patched BitsRecovery...");
        }
        
        if (Config.MetalCastingTweaks)
        {
            HarmonyInstance.PatchCategory(CastingTweaksCategory);
            Logger.VerboseDebug("Patched MetalCastingTweaks...");
        }

        if (!Config.SmithWithBits && !Config.BitsTopUp) return;
        HarmonyInstance.PatchCategory(SmithingBitsCategory);
        Logger.VerboseDebug("Patched SmithingBits...");
    }
    
    public static void Unpatch()
    {
        Logger?.VerboseDebug("Unpatching...");
        HarmonyInstance?.UnpatchAll();
        HarmonyInstance = null;
    }
    
    public override void Dispose()
    {
        Unpatch();
        Logger = null;
        ModId = null;
        Api = null;
        Config = null;
        base.Dispose();
    }
}
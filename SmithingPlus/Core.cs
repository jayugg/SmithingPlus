using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SmithingPlus.Config;
using SmithingPlus.ToolRecovery;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus;

[HarmonyPatch]
[HarmonyPatchCategory(SmithingBitsCategory)]
public class Core : ModSystem
{
    public static ILogger Logger;
    public static string ModId;
    public static ICoreAPI Api;
    public static Harmony HarmonyInstance;
    internal static readonly string ConfigName = "SmithingPlus.json";
    public static ServerConfig Config; 
    internal const string ToolRecoveryCategory = "toolRecovery";
    internal const string SmithingBitsCategory = "smithingBits";
    
    public static readonly string RecipeOutputNameCacheKey = "smithingplus:recipeOutputName";
    public static readonly string ToolToRecipeCacheKey= "smithingplus:toolToRecipe";
    public static readonly string RecipeVoxelCountCacheKey = "smithingplus:recipeVoxelCount";
    public static Dictionary<int, string> RecipeOutputNameCache => ObjectCacheUtil.GetOrCreate(Api, RecipeOutputNameCacheKey, () => new Dictionary<int, string>());
    public static Dictionary<string, SmithingRecipe> ToolToRecipeCache => ObjectCacheUtil.GetOrCreate(Api, ToolToRecipeCacheKey, () => new Dictionary<string, SmithingRecipe>());
    public static Dictionary<int, int> RecipeVoxelCountCache => ObjectCacheUtil.GetOrCreate(Core.Api, RecipeVoxelCountCacheKey, () => new Dictionary<int, int>());

    public override void StartPre(ICoreAPI api)
    {
        Logger = Mod.Logger;
        ModId = Mod.Info.ModID;
        Api = api;
        try
        {
            Config = api.LoadModConfig<ServerConfig>(ConfigName);
            if (Config != null) return;
            Config = new ServerConfig();
            Logger.VerboseDebug("Config file not found, creating a new one...");
            api.StoreModConfig(Config, ConfigName);
        } catch (Exception e) {
            Logger.Error("Failed to load config, you probably made a typo: {0}", e);
            Config = new ServerConfig();
        }
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("ItemWorkableNugget", typeof(ItemWorkableNugget));
        api.RegisterCollectibleBehaviorClass($"{ModId}:BrokenTool", typeof(CollectibleBehaviorBrokenTool));
        api.RegisterCollectibleBehaviorClass($"{ModId}:BrokenToolHead", typeof(CollectibleBehaviorBrokenToolHead));
        api.RegisterEntityBehaviorClass($"{ModId}:RecyclableArrow", typeof(RecyclableArrowBehavior));
        Patch();
    }
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Event.OnEntitySpawn += AddEntityBehaviors;
        api.Event.OnEntityLoaded += AddEntityBehaviors;
    }

    private void AddEntityBehaviors(Entity entity)
    {
        if (!Config.ArrowsDropBits || entity is not EntityProjectile) return;
        Logger.VerboseDebug("Adding RecyclableArrowBehavior to {0}", entity.Code);
        entity.AddBehavior(new RecyclableArrowBehavior(entity));
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
            if ((collObj.Tool != null || collObj.IsRepairableTool()) && collObj.HasMetalMaterial(api)) collObj.AddBehavior<CollectibleBehaviorBrokenTool>();
            else if (WildcardUtil.Match(Config.ToolHeadSelector,collObj.Code.ToString())) collObj.AddBehavior<CollectibleBehaviorBrokenTool>();
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

        if (!Config.SmithWithBits && !Config.BitsTopUp) return;
        HarmonyInstance.PatchCategory(SmithingBitsCategory);
        Logger.VerboseDebug("Patched SmithingBits...");
    }
    
    public static void Unpatch()
    {
        Logger.VerboseDebug("Unpatching...");
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
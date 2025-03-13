using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SmithingPlus.ClientTweaks;
using SmithingPlus.Config;
using SmithingPlus.Extra;
using SmithingPlus.SmithWithBits;
using SmithingPlus.ToolRecovery;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace SmithingPlus;

[HarmonyPatch]
[HarmonyPatchCategory(SmithingBitsCategory)]
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
        api.RegisterItemClass("ItemXWorkableNugget", typeof(ItemXWorkableNugget));
        api.RegisterItemClass("ItemWorkableRod", typeof(ItemWorkableRod));
        api.RegisterCollectibleBehaviorClass($"{ModId}:RepairableTool", typeof(CollectibleBehaviorRepairableTool));
        api.RegisterCollectibleBehaviorClass($"{ModId}:RepairableToolHead", typeof(CollectibleBehaviorRepairableToolHead));
        api.RegisterCollectibleBehaviorClass($"{ModId}:BrokenToolHead", typeof(CollectibleBehaviorBrokenToolHead));
        api.RegisterCollectibleBehaviorClass($"{ModId}:AnvilWorkable", typeof(CollectibleBehaviorAnvilWorkable));
        api.RegisterEntityBehaviorClass($"{ModId}:RecyclableArrow", typeof(RecyclableArrowBehavior));
        api.RegisterItemClass($"{ModId}:ItemStoneHammer", typeof(ItemStoneHammer));
        api.RegisterBlockEntityClass($"{ModId}:StoneAnvil", typeof(BlockEntityStoneAnvil));
        Patch();
    }
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Event.OnEntitySpawn += AddEntityBehaviors;
        api.Event.OnEntityLoaded += AddEntityBehaviors;
        api.ChatCommands
            .Create("setHeldTemp")
            .WithDescription("Set the temperature of held item.")
            .RequiresPrivilege("controlserver")
            .WithArgs(api.ChatCommands.Parsers.Float("temperature"), api.ChatCommands.Parsers.OptionalWord("playerName"))
            .HandleWith(args => OnSetHeldTempCommand(api, args));
        api.ChatCommands
            .Create("getSmithingQuality")
            .WithDescription("Get the smithing quality of player.")
            .RequiresPrivilege("controlserver")
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"))
            .HandleWith(args => OnGetSmithingQualityCommand(api, args));
    }

    private TextCommandResult OnSetHeldTempCommand(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        var temperature = args[0] as float? ?? 0;
        string playerName = args[1] as string;
        IServerPlayer targetPlayer;
        if (string.IsNullOrEmpty(playerName))
        {
            targetPlayer = args.Caller.Player as IServerPlayer;
        }
        else
        {
            targetPlayer = GetPlayerByName(api, playerName);
            if (targetPlayer == null)
            {
                return TextCommandResult.Error($"Player '{playerName}' not found.");
            }
        }
        if (targetPlayer == null) return TextCommandResult.Error("Player not found.");
        var heldStack = targetPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
        if (heldStack == null)
        {
            return TextCommandResult.Error($"Player '{targetPlayer.PlayerName}' has no held item.");
        }
        heldStack.Collectible.SetTemperature(targetPlayer.Entity.World, heldStack, temperature);
        targetPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
        return TextCommandResult.Success($"Held item temperature set to {heldStack.Collectible.GetTemperature(targetPlayer.Entity.World, heldStack)} for player '{targetPlayer?.PlayerName}'.");
    }
    
    private TextCommandResult OnGetSmithingQualityCommand(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        string playerName = args[0] as string;
        IServerPlayer targetPlayer;
        if (string.IsNullOrEmpty(playerName))
        {
            targetPlayer = args.Caller.Player as IServerPlayer;
        }
        else
        {
            targetPlayer = GetPlayerByName(api, playerName);
            if (targetPlayer == null)
            {
                return TextCommandResult.Error($"Player '{playerName}' not found.");
            }
        }
        if (targetPlayer == null) return TextCommandResult.Error("Player not found.");
        var smithingQuality = targetPlayer.Entity.Stats.GetBlended("sp:smithingQuality");
        return TextCommandResult.Success($"Smithing quality for player '{targetPlayer?.PlayerName}' is {smithingQuality}.");
    }
    
    private static IServerPlayer GetPlayerByName(ICoreServerAPI api, string playerName)
    {
        foreach (var player1 in api.World.AllOnlinePlayers)
        {
            var player = (IServerPlayer)player1;
            if (player.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase))
            {
                return player;
            }
        }

        return null;
    }

    private void AddEntityBehaviors(Entity entity)
    {
        if (!Config.ArrowsDropBits || entity is not EntityProjectile projectile) return;
        if (!RecyclableArrowBehavior.IsRecyclableArrow(projectile)) return;
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
            && !(r.Ingredient.RecipeAttributes?["repairOnly"]?.AsBool() ?? false)
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
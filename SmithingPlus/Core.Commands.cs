using System;
using System.Linq;
using SmithingPlus.Metal;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus;

public partial class Core
{
    private static void RegisterServerCommands(ICoreServerAPI api)
    {
        api.ChatCommands
            .Create("setHeldTemp")
            .WithAlias("spt")
            .WithDescription("Set the temperature of held item.")
            .RequiresPrivilege("controlserver")
            .WithArgs(api.ChatCommands.Parsers.Float("temperature"),
                api.ChatCommands.Parsers.OptionalWord("playerName"))
            .HandleWith(args => OnSetHeldTempCommand(api, args));
        api.ChatCommands
            .Create("setHeldDurability")
            .WithAlias("spd")
            .WithDescription("Set the durability of held item.")
            .RequiresPrivilege("controlserver")
            .WithArgs(api.ChatCommands.Parsers.Float("durability"), api.ChatCommands.Parsers.OptionalWord("playerName"))
            .HandleWith(args => OnSetHeldDurabilityCommand(api, args));
        api.ChatCommands
            .Create("getSmithingQuality")
            .WithDescription("Get the smithing quality of player.")
            .RequiresPrivilege("controlserver")
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"))
            .HandleWith(args => OnGetSmithingQualityCommand(api, args));
        api.ChatCommands
            .Create("completeHeldWorkItem")
            .WithAlias("spcw")
            .WithDescription("Complete the held work item.")
            .RequiresPrivilege("controlserver")
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"))
            .HandleWith(args => OnCompleteHeldWorkitemCommand(api, args));
        api.ChatCommands
            .Create("setHeldAttribute")
            .WithAlias("spsa")
            .WithDescription("Set a bool attribute to held item stack.")
            .RequiresPrivilege("controlserver")
            .WithArgs(api.ChatCommands.Parsers.Word("attributeKey"), api.ChatCommands.Parsers.Word("attributeValue"),
                api.ChatCommands.Parsers.OptionalWord("playerName"))
            .HandleWith(args => OnSetHeldAttributeCommand(api, args));
        api.ChatCommands
            .Create("getMetalMaterial")
            .WithDescription("Get the metal material of held item.")
            .RequiresPrivilege("controlserver")
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"))
            .HandleWith(args => OnGetMetalMaterialCommand(api, args));
        api.ChatCommands
            .Create("resetMetalMaterialCache")
            .WithDescription("Reset the metal material cache.")
            .RequiresPrivilege("controlserver")
            .HandleWith(args => ResetMetalMaterialCache(api, args));
    }

    private static TextCommandResult OnSetHeldAttributeCommand(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        var attributeKey = args[0] as string;
        var attributeValue = bool.Parse(args[1] as string ?? string.Empty);
        if (string.IsNullOrEmpty(attributeKey) || string.IsNullOrEmpty(args[1] as string))
            return TextCommandResult.Error("Attribute key or value is missing.");
        var playerName = args[2] as string;
        IServerPlayer targetPlayer;
        if (string.IsNullOrEmpty(playerName))
        {
            targetPlayer = args.Caller.Player as IServerPlayer;
        }
        else
        {
            targetPlayer = GetPlayerByName(api, playerName);
            if (targetPlayer == null) return TextCommandResult.Error($"Player '{playerName}' not found.");
        }

        if (targetPlayer == null) return TextCommandResult.Error("Player not found.");
        var heldStack = targetPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
        if (heldStack == null) return TextCommandResult.Error($"Player '{targetPlayer.PlayerName}' has no held item.");
        heldStack.Attributes.SetBool(attributeKey, attributeValue);
        targetPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
        return TextCommandResult.Success(
            $"Set held stack attribute {attributeKey} to value {attributeValue} for player '{targetPlayer.PlayerName}'.");
    }

    private static TextCommandResult OnSetHeldTempCommand(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        var temperature = args[0] as float? ?? 0;
        var playerName = args[1] as string;
        IServerPlayer targetPlayer;
        if (string.IsNullOrEmpty(playerName))
        {
            targetPlayer = args.Caller.Player as IServerPlayer;
        }
        else
        {
            targetPlayer = GetPlayerByName(api, playerName);
            if (targetPlayer == null) return TextCommandResult.Error($"Player '{playerName}' not found.");
        }

        if (targetPlayer == null) return TextCommandResult.Error("Player not found.");
        var heldStack = targetPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
        if (heldStack == null) return TextCommandResult.Error($"Player '{targetPlayer.PlayerName}' has no held item.");
        heldStack.Collectible.SetTemperature(targetPlayer.Entity.World, heldStack, temperature);
        targetPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
        return TextCommandResult.Success(
            $"Held item temperature set to {heldStack.Collectible.GetTemperature(targetPlayer.Entity.World, heldStack)} for player '{targetPlayer.PlayerName}'.");
    }

    private static TextCommandResult OnSetHeldDurabilityCommand(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        var durability = args[0] as float? ?? 0;
        var playerName = args[1] as string;
        IServerPlayer targetPlayer;
        if (string.IsNullOrEmpty(playerName))
        {
            targetPlayer = args.Caller.Player as IServerPlayer;
        }
        else
        {
            targetPlayer = GetPlayerByName(api, playerName);
            if (targetPlayer == null) return TextCommandResult.Error($"Player '{playerName}' not found.");
        }

        if (targetPlayer == null) return TextCommandResult.Error("Player not found.");
        var heldStack = targetPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
        if (heldStack == null) return TextCommandResult.Error($"Player '{targetPlayer.PlayerName}' has no held item.");
        heldStack.Attributes.SetInt("durability",
            (int)Math.Clamp(durability, 1, heldStack.Collectible.GetMaxDurability(heldStack)));
        targetPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
        return TextCommandResult.Success(
            $"Held item durability set to {heldStack.Attributes.GetInt("durability")} for player '{targetPlayer.PlayerName}'.");
    }

    private static TextCommandResult OnCompleteHeldWorkitemCommand(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        var playerName = args[0] as string;
        IServerPlayer targetPlayer;
        if (string.IsNullOrEmpty(playerName))
        {
            targetPlayer = args.Caller.Player as IServerPlayer;
        }
        else
        {
            targetPlayer = GetPlayerByName(api, playerName);
            if (targetPlayer == null) return TextCommandResult.Error($"Player '{playerName}' not found.");
        }

        if (targetPlayer == null) return TextCommandResult.Error("Player not found.");
        var heldStack = targetPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
        if (heldStack == null) return TextCommandResult.Error($"Player '{targetPlayer.PlayerName}' has no held item.");
        if (heldStack.Collectible is not ItemWorkItem)
            return TextCommandResult.Error($"Player '{targetPlayer.PlayerName}' is not holding a work item.");
        var selectedRecipe = api.GetSmithingRecipes().FirstOrDefault(r =>
            r.RecipeId == heldStack.Attributes.GetInt("selectedRecipeId"));
        if (selectedRecipe == null)
            return TextCommandResult.Error(
                $"Player '{targetPlayer.PlayerName}''s held work item has no selected recipe.");
        var recipeVoxels = selectedRecipe.Voxels;
        heldStack.Attributes.SetBytes("voxels", BlockEntityAnvil.serializeVoxels(recipeVoxels.ToByteArray()));
        targetPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
        return TextCommandResult.Success($"Held work item completed for player '{targetPlayer.PlayerName}'.");
    }

    private static TextCommandResult OnGetSmithingQualityCommand(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        var playerName = args[0] as string;
        IServerPlayer targetPlayer;
        if (string.IsNullOrEmpty(playerName))
        {
            targetPlayer = args.Caller.Player as IServerPlayer;
        }
        else
        {
            targetPlayer = GetPlayerByName(api, playerName);
            if (targetPlayer == null) return TextCommandResult.Error($"Player '{playerName}' not found.");
        }

        if (targetPlayer == null) return TextCommandResult.Error("Player not found.");
        var smithingQuality = targetPlayer.Entity.Stats.GetBlended("sp:smithingQuality");
        return TextCommandResult.Success(
            $"Smithing quality for player '{targetPlayer.PlayerName}' is {smithingQuality}.");
    }


    private static TextCommandResult OnGetMetalMaterialCommand(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        var playerName = args[0] as string;
        IServerPlayer targetPlayer;
        if (string.IsNullOrEmpty(playerName))
        {
            targetPlayer = args.Caller.Player as IServerPlayer;
        }
        else
        {
            targetPlayer = GetPlayerByName(api, playerName);
            if (targetPlayer == null) return TextCommandResult.Error($"Player '{playerName}' not found.");
        }

        if (targetPlayer == null) return TextCommandResult.Error("Player not found.");
        var heldStack = targetPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
        if (heldStack == null) return TextCommandResult.Error($"Player '{targetPlayer.PlayerName}' has no held item.");
        var metalMaterial = heldStack.Collectible.GetOrCacheMetalMaterial(api);
        if (metalMaterial == null)
            return TextCommandResult.Error($"Held item '{heldStack.GetName()}' is not a metal item.");
        return TextCommandResult.Success(
            $"Held item '{heldStack.GetName()}' has metal material {metalMaterial.Code} with ingot {metalMaterial.IngotCode}.");
    }

    private static TextCommandResult ResetMetalMaterialCache(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        ObjectCacheUtil.Delete(Api, MetalMaterialCacheKey);
        return TextCommandResult.Success("Metal material cache has been reset.");
    }

    private static IServerPlayer GetPlayerByName(ICoreServerAPI api, string playerName)
    {
        return api.World.AllOnlinePlayers
            .Cast<IServerPlayer>()
            .FirstOrDefault(player => player.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));
    }
}
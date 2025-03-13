using System;
using System.Linq;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SmithingPlus;

public partial class Core
{
    private void RegisterServerCommands(ICoreServerAPI api)
    {
        api.ChatCommands
            .Create("setHeldTemp")
            .WithAlias("sp t")
            .WithDescription("Set the temperature of held item.")
            .RequiresPrivilege("controlserver")
            .WithArgs(api.ChatCommands.Parsers.Float("temperature"), api.ChatCommands.Parsers.OptionalWord("playerName"))
            .HandleWith(args => OnSetHeldTempCommand(api, args));
        api.ChatCommands
            .Create("setHeldDurability")
            .WithAlias("sp d")
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
            .WithAlias("sp cw")
            .WithDescription("Complete the held work item.")
            .RequiresPrivilege("controlserver")
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"))
            .HandleWith(args => OnCompleteHeldWorkitemCommand(api, args));
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
        return TextCommandResult.Success($"Held item temperature set to {heldStack.Collectible.GetTemperature(targetPlayer.Entity.World, heldStack)} for player '{targetPlayer.PlayerName}'.");
    }
    
    private TextCommandResult OnSetHeldDurabilityCommand(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        var durability = args[0] as float? ?? 0;
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
        heldStack.Attributes.SetInt("durability", (int) Math.Clamp(durability, 1, heldStack.Collectible.GetMaxDurability(heldStack)));
        targetPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
        return TextCommandResult.Success($"Held item durability set to {heldStack.Attributes.GetInt("durability")} for player '{targetPlayer.PlayerName}'.");
    }
    
    private TextCommandResult OnCompleteHeldWorkitemCommand(ICoreServerAPI api, TextCommandCallingArgs args)
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
        var heldStack = targetPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
        if (heldStack == null)
        {
            return TextCommandResult.Error($"Player '{targetPlayer.PlayerName}' has no held item.");
        }
        if (heldStack.Collectible is not ItemWorkItem)
        {
            return TextCommandResult.Error($"Player '{targetPlayer.PlayerName}' is not holding a work item.");
        }
        var selectedRecipe = api.GetSmithingRecipes().FirstOrDefault(r =>
            r.RecipeId == heldStack.Attributes.GetInt("selectedRecipeId"));
        if (selectedRecipe == null)
        {
            return TextCommandResult.Error($"Player '{targetPlayer.PlayerName}''s held work item has no selected recipe.");
        }
        var recipeVoxels = selectedRecipe.Voxels;
        heldStack.Attributes.SetBytes("voxels", BlockEntityAnvil.serializeVoxels(recipeVoxels.ToByteArray()));
        targetPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
        return TextCommandResult.Success($"Held work item completed for player '{targetPlayer.PlayerName}'.");
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
}
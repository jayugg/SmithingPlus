using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace SmithingPlus;

public partial class Core
{
    private void RegisterServerCommands(ICoreServerAPI api)
    {
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

}
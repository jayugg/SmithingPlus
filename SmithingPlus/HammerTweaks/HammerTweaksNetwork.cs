using JetBrains.Annotations;
using SmithingPlus.Config;
using SmithingPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace SmithingPlus.HammerTweaks;

[UsedImplicitly]
public class HammerTweaksNetwork : ModSystem
{
    private const string ChannelName = $"{Core.ModId}:{Core.HammerTweaksCategory}";
    
    /// <summary>
    /// Always null on servers, stores the original tool modes count for the client.
    /// </summary>
    public static int? OriginalToolModesCount { get; set; }

    public override bool ShouldLoad(ICoreAPI api)
    {
        return ConfigLoader.Config?.HammerTweaks ?? false;
    }

    public override double ExecuteOrder() => base.ExecuteOrder() + 0.01;

    public override void Start(ICoreAPI api)
    {
        api.Network
            .RegisterChannel(ChannelName)
            .RegisterMessageType(typeof(FlipToolModePacket));
    }

    #region Client

    private IClientNetworkChannel ClientChannel { get; set; }
    private ICoreClientAPI Capi { get; set; }

    public override void StartClientSide(ICoreClientAPI api)
    {
        Capi = api;
        ClientChannel = api.Network.GetChannel(ChannelName);
    }

    public static void SendFlipToolMode(ICoreClientAPI capi, int flipToolModeIndex)
    {
        var response = new FlipToolModePacket
        {
            ToolMode = flipToolModeIndex
        };
        capi.Network.GetChannel(ChannelName).SendPacket(response);
    }

    #endregion

    #region Server

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Network
            .GetChannel(ChannelName)
            .SetMessageHandler<FlipToolModePacket>(ReceiveFlipToolMode);
    }

    private static void ReceiveFlipToolMode(IServerPlayer fromPlayer, FlipToolModePacket packet)
    {
        var activeSlot = fromPlayer.InventoryManager.ActiveHotbarSlot;
        activeSlot?.Itemstack?.TempAttributes.SetInt(ModAttributes.FlipItemToolMode, packet.ToolMode);
    }

    #endregion
    
    public override void Dispose()
    {
        ClientChannel = null;
        OriginalToolModesCount = null;
        base.Dispose();
    }
}
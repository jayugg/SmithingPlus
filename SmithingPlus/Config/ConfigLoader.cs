using System;
using JetBrains.Annotations;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace SmithingPlus.Config;

[UsedImplicitly]
public class ConfigLoader : ModSystem
{
    private const string ConfigName = "SmithingPlus.json";
    public static ServerConfig Config { get; private set; }

    public override double ExecuteOrder()
    {
        return 0.03;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        SendConfigToClient(api);
    }
    
    public override void StartClientSide(ICoreClientAPI api)
    {
        ReadConfigFromServer(api);
    }

    public override void StartPre(ICoreAPI api)
    {
        LoadServerConfig(api);
    }

    private void LoadServerConfig(ICoreAPI api)
    {
        try
        {
            Config = api.LoadModConfig<ServerConfig>(ConfigName);
            if (Config == null)
            {
                Config = new ServerConfig();
                Mod.Logger.VerboseDebug("Config file not found, creating a new one...");
            }

            // Clamp and warn about issues in config.
            VerifyConfig();

            api.StoreModConfig(Config, ConfigName);
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to load config, you probably made a typo: {0}", e);
            Config = new ServerConfig();
        }
    }

    public void SendConfigToClient(ICoreAPI api)
    {
        api.World.Config.SetBool("SmithingPlus_CanRepairForlornHopeEstoc", Config.CanRepairForlornHopeEstoc);
        api.World.Config.SetBool("SmithingPlus_WorkableBits", Config.SmithWithBits || Config.EnableToolRecovery);
        
        api.World.Config.SetBool("SmithingPlus_DynamicMoldUnits", Config.DynamicMoldUnits);
        api.World.Config.SetFloat("SmithingPlus_CastToolDurabilityPenalty", Config.CastToolDurabilityPenalty);
        api.World.Config.SetFloat("SmithingPlus_DurabilityPenaltyPerRepair", Config.DurabilityPenaltyPerRepair);
    }
    
    public void ReadConfigFromServer(ICoreAPI api)
    {
        Config.DynamicMoldUnits = api.World.Config.GetBool("SmithingPlus_DynamicMoldUnits");
        Config.CastToolDurabilityPenalty = api.World.Config.GetFloat("SmithingPlus_CastToolDurabilityPenalty");
        Config.DurabilityPenaltyPerRepair = api.World.Config.GetFloat("SmithingPlus_DurabilityPenaltyPerRepair");
    }

    private void VerifyConfig()
    {
        if (Config.BrokenToolVoxelPercent < 0.2)
            Mod.Logger.Warning($"[{nameof(ConfigLoader)}] Config setting {nameof(Config.BrokenToolVoxelPercent)}" +
                               $"has a very low value, your broken tools well be almost or fully empty.");

        if (Config.VoxelsPerBit is < 2 or > 3)
        {
            Mod.Logger.Warning($"[{nameof(ConfigLoader)}] Config setting {nameof(Config.VoxelsPerBit)}" +
                               $"requires a value between 2 and 3. Clamping value.");
            Config.VoxelsPerBit = Math.Clamp(Config.VoxelsPerBit, 2, 3);
        }
    }

    public override void Dispose()
    {
        Config = null;
        base.Dispose();
    }
}

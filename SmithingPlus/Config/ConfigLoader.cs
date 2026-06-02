using System;
using JetBrains.Annotations;
using Vintagestory.API.Common;

namespace SmithingPlus.Config;

[UsedImplicitly]
public class ConfigLoader : ModSystem
{
    private const string ServerConfigName = "SmithingPlus.json";
    private const string ClientConfigName = "SmithingPlusClient.json";
    public static ServerConfig Config { get; private set; }
    public static ClientConfig CConfig { get; private set; }

    public override double ExecuteOrder()
    {
        return 0.03;
    }

    public override void StartPre(ICoreAPI api)
    {
        try
        {
            CConfig = api.LoadModConfig<ClientConfig>(ClientConfigName);
            if (CConfig == null)
            {
                // Try to load settings from old mixed file
                CConfig = api.LoadModConfig<ClientConfig>(ServerConfigName);
                if (CConfig == null)
                {
                    CConfig = new ClientConfig();
                    Mod.Logger.VerboseDebug("Client Config file not found, creating a new one...");
                } else
                {
                    Mod.Logger.VerboseDebug("Client Config file not found, creating from old combined config");
                }
            }

            api.StoreModConfig(CConfig, ClientConfigName);
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to load client config, you probably made a typo: {0}", e);
            CConfig = new ClientConfig();
        }

        try
        {
            Config = api.LoadModConfig<ServerConfig>(ServerConfigName);
            if (Config == null)
            {
                Config = new ServerConfig();
                Mod.Logger.VerboseDebug("Config file not found, creating a new one...");
            }

            api.StoreModConfig(Config, ServerConfigName);
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to load config, you probably made a typo: {0}", e);
            Config = new ServerConfig();
        }
    }

    public override void Start(ICoreAPI api)
    {
        api.World.Config.SetBool("SmithingPlus_CanRepairForlornHopeEstoc", Config.CanRepairForlornHopeEstoc);
        api.World.Config.SetBool("SmithingPlus_WorkableBits", Config.SmithWithBits || Config.EnableToolRecovery);
        if (Config.BrokenToolVoxelPercent < 0.2)
            Mod.Logger.Warning($"[{nameof(ConfigLoader)}] Config setting {nameof(Config.BrokenToolVoxelPercent)}" +
                               $"has a very low value, your broken tools well be almost or fully empty.");
        ;
        if (Config.VoxelsPerBit is < 2 or > 3)
        {
            Mod.Logger.Warning($"[{nameof(ConfigLoader)}] Config setting {nameof(Config.VoxelsPerBit)}" +
                               $"requires a value between 2 and 3. Clamping value.");
            ;
            Config.VoxelsPerBit = Math.Clamp(Config.VoxelsPerBit, 2, 3);
        }
    }

    public override void Dispose()
    {
        Config = null;
        CConfig = null;
        base.Dispose();
    }
}

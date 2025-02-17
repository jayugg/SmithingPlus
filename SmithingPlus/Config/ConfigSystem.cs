using System;
using Vintagestory;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace SmithingPlus.Config;

public class ConfigSystem : ModSystem
{
    public override double ExecuteOrder() => 0.03;
    internal static readonly string ConfigName = "SmithingPlus.json";
    public static ServerConfig Config;
    
    public override void StartPre(ICoreAPI api)
    {
        try
        {
            Config = api.LoadModConfig<ServerConfig>(ConfigName);
            if (Config != null) return;
            Config = new ServerConfig();
            api.Logger.VerboseDebug("[smithingplus] Config file not found, creating a new one...");
            api.StoreModConfig(Config, ConfigName);
        } catch (Exception e) {
            api.Logger.Error("[smithingplus] Failed to load config, you probably made a typo: {0}", e);
            Config = new ServerConfig();
        }
    }

    public override void Start(ICoreAPI api)
    {
        api.World.Config.SetBool("SmithingPlus_CanRepairForlornHopeEstoc", Config.CanRepairForlornHopeEstoc);
    }
}
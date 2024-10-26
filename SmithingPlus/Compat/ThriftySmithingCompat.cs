using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace SmithingPlus.Compat;

public class ThriftySmithingCompat : ModSystem
{
    public static bool ThriftySmithingLoaded { get; private set; } = false;
    public override double ExecuteOrder() => 1.5;
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        ThriftySmithingLoaded = api.ModLoader.IsModEnabled("thriftysmithing");
    }
}
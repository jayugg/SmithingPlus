using Vintagestory.API.Common;

namespace SmithingPlus.Compat;

public class ThriftySmithingCompat : ModSystem
{
    public static bool ThriftySmithingLoaded { get; private set; } = false;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        ThriftySmithingLoaded = api.ModLoader.IsModEnabled("thriftysmithing");
    }
}
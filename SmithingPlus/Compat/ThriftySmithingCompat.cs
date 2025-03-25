using Vintagestory.API.Common;

namespace SmithingPlus.Compat;

public class ThriftySmithingCompat : ModSystem
{
    public static bool ThriftySmithingLoaded { get; private set; }

    public override double ExecuteOrder()
    {
        return 1.5;
    }

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        ThriftySmithingLoaded = api.ModLoader.IsModEnabled("thriftysmithing");
    }
}
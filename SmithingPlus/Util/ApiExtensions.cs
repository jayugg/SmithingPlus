using JetBrains.Annotations;
using Vintagestory.API.Common;

namespace SmithingPlus.Util;

public static class ApiExtensions
{
    [CanBeNull]
    public static T GetModSystem<T>(this ICoreAPI api) where T : ModSystem
    {
        return api.ModLoader.GetModSystem<T>();
    }
}
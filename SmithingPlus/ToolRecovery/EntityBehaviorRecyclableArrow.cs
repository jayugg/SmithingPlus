using SmithingPlus.Metal;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus.ToolRecovery;

public class RecyclableArrowBehavior : EntityBehavior
{
    public RecyclableArrowBehavior(Entity entity) : base(entity)
    {
    }

    public override string PropertyName()
    {
        return $"{Core.ModId}:recyclablearrow";
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer,
        ref EnumHandling handling)
    {
        Core.Logger.VerboseDebug("GetDrops: {0}", entity.Code);
        if (entity is not EntityProjectile { ProjectileStack: { } stack } entityProjectile ||
            !IsRecyclableArrow(entityProjectile)) return base.GetDrops(world, pos, byPlayer, ref handling);
        Core.Logger.VerboseDebug("Arrow died: {0}", stack);
        var metalMaterial = stack.GetOrCacheMetalMaterial(world.Api);
        var metalVariant = metalMaterial?.Code;
        if (metalVariant == null) return base.GetDrops(world, pos, byPlayer, ref handling);
        Core.Logger.VerboseDebug("Arrow metal: {0}", metalVariant);
        var metalBitStack = metalMaterial.MetalBitStack;
        if (metalBitStack == null) return base.GetDrops(world, pos, byPlayer, ref handling);
        handling = EnumHandling.PreventDefault;
        return new[] { metalBitStack };
    }

    public static bool IsRecyclableArrow(EntityProjectile projectile)
    {
        var projectileItem = projectile.ProjectileStack?.Collectible;
        if (projectileItem == null) return false;
        Core.Logger.VerboseDebug("Testing recyclable arrow: {0}", projectileItem.Code);
        return WildcardUtil.Match(Core.Config.ArrowSelector, projectile.ProjectileStack?.Collectible.Code.ToString());
    }
}
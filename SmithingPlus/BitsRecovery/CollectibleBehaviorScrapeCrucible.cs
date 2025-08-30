#nullable enable
using SmithingPlus.Metal;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.BitsRecovery;

public class CollectibleBehaviorScrapeCrucible(CollectibleObject collObj) : CollectibleBehavior(collObj)
{
    public const float MaxScrapeTemperature = 50;

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection? blockSel,
        EntitySelection entitySel,
        bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
    {
        if (byEntity is not EntityPlayer entityPlayer) return;
        if (blockSel?.Position is null) return;
        handHandling = EnumHandHandling.NotHandled;
        // Check if player can access the block
        if (!CanAccessBlock(entityPlayer, blockSel)) return;
        if (!IsSelectingValidCrucible(entityPlayer, blockSel)) return;
        handling = EnumHandling.PreventDefault;
        handHandling = EnumHandHandling.PreventDefault;
        byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), entityPlayer, entityPlayer.Player,
            0.3f);
    }

    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel, ref EnumHandling handling)
    {
        if (byEntity is not EntityPlayer) return false;
        handling = EnumHandling.PreventDefault;
        byEntity.StartAnimation("knifecut");
        return secondsUsed < 1.5;
    }

    public override void OnHeldInteractStop(float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        ref EnumHandling handling)
    {
        byEntity.StopAnimation("knifecut");
        if (byEntity.World.Side != EnumAppSide.Server) return;
        if (byEntity is not EntityPlayer entityPlayer) return;
        var groundStorage = TryGetSelectedGroundStorage(entityPlayer, blockSel);
        if (!TryGetCrucibleStack(entityPlayer, blockSel, out var crucibleSlot) ||
            crucibleSlot?.Itemstack is not { } crucibleStack)
            return;
        var playerInventory = entityPlayer.Player.InventoryManager;
        var activeSlot = playerInventory.ActiveHotbarSlot;
        // Check that player is interacting with a valid crucible
        if (activeSlot?.Itemstack is null) return;
        var world = entityPlayer.World;
        if (!TryGetOutputStack(crucibleStack, world, out var outputStack)) return;
        var outputUnits = crucibleStack.Attributes.GetInt("units");
        var outputBitCount = outputUnits / 5;
        var metalMaterial = outputStack.GetOrCacheMetalMaterial(byEntity.Api);
        var metalBitStack = metalMaterial?.MetalBitStack;
        if (metalMaterial == null)
        {
            Core.Logger.VerboseDebug(
                $"[{nameof(OnHeldInteractStop)}] {outputStack.GetName()} has no valid metal material.");
            return;
        }

        if (metalBitStack == null)
        {
            Core.Logger.VerboseDebug(
                $"[{nameof(OnHeldInteractStop)}] {metalMaterial.IngotCode} has no valid metal bit stack.");
            return;
        }

        var metalTier = metalMaterial.Tier;
        metalBitStack.StackSize = outputBitCount;
        metalBitStack.SetTemperatureFrom(world, crucibleStack);
        var firedCrucibleCode = crucibleStack.Collectible.CodeWithVariant("type", "fired");
        var firedCrucibleItem = world.GetBlock(firedCrucibleCode);
        if (firedCrucibleItem == null)
            Core.Logger.Warning(
                $"[{nameof(OnHeldInteractStop)}] Something went wrong, cannot find fired crucible with code {firedCrucibleCode}");
        var emptyCrucibleStack = new ItemStack(firedCrucibleItem);
        if (!playerInventory.TryGiveItemstack(metalBitStack, true))
            world.SpawnItemEntity(metalBitStack, blockSel.Position);
        crucibleSlot.Itemstack = emptyCrucibleStack;
        crucibleSlot.MarkDirty();
        groundStorage?.MarkDirty();
        var chiselDamage = (2 + metalTier) * outputBitCount;
        activeSlot.Itemstack.Collectible.DamageItem(world, entityPlayer, activeSlot, chiselDamage);
        activeSlot.MarkDirty();
    }

    private static bool TryGetOutputStack(ItemStack crucibleStack, IWorldAccessor world, out ItemStack output)
    {
        output = crucibleStack.Attributes.GetItemstack("output");
        output?.ResolveBlockOrItem(world); // Vanilla does this in BlockSmeltedContainer.GetContents
        if (output is null ||
            crucibleStack.Attributes.TryGetInt("units") is < 5)
            return false;
        Core.Logger.VerboseDebug($"[{nameof(CollectibleBehaviorScrapeCrucible)}] Output: {0}", output.Collectible.Code);
        return true;
    }

    private static bool IsSelectingValidCrucible(EntityPlayer entityPlayer, BlockSelection blockSel)
    {
        var groundStorage = TryGetSelectedGroundStorage(entityPlayer, blockSel);
        var world = entityPlayer.World;
        if (groundStorage?.GetSlotAt(blockSel) is not { Itemstack: { } itemStack }) return false;
        return itemStack.IsSmeltedContainer() && itemStack.GetTemperature(world) < MaxScrapeTemperature;
    }

    private static bool TryGetCrucibleStack(EntityPlayer entityPlayer, BlockSelection? blockSel, out ItemSlot? atSlot)
    {
        atSlot = null;
        if (blockSel == null) return false;
        var groundStorage = TryGetSelectedGroundStorage(entityPlayer, blockSel);
        if (groundStorage?.GetSlotAt(blockSel) is not { Itemstack: not null } targetSlot) return false;
        atSlot = targetSlot;
        return true;
    }

    private static BlockEntityGroundStorage? TryGetSelectedGroundStorage(EntityPlayer entityPlayer,
        BlockSelection blockSel)
    {
        var blockEntity = entityPlayer.World.BlockAccessor.GetBlockEntity(blockSel.Position);
        return blockEntity as BlockEntityGroundStorage;
    }

    private static bool CanAccessBlock(EntityPlayer entityPlayer, BlockSelection blockSel)
    {
        var modSystem = entityPlayer.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
        return modSystem?.IsReinforced(blockSel.Position) != true &&
               entityPlayer.World.Claims.TryAccess(entityPlayer.Player, blockSel.Position,
                   EnumBlockAccessFlags.BuildOrBreak);
    }
}
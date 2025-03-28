using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace SmithingPlus.BitsRecovery;

public class CollectibleBehaviorScrapeCrucible : CollectibleBehavior
{
    public CollectibleBehaviorScrapeCrucible(CollectibleObject collObj) : base(collObj)
    {
    }

    public const float MaxScrapeTemperature = 50;
    
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel,
        bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
    {
        if (byEntity is not EntityPlayer entityPlayer) return;
        if (blockSel?.Position is null) return;
        // Check if player can access the block
        if (!CanAccessBlock(entityPlayer, blockSel)) return;
        if (!IsSelectingValidCrucible(entityPlayer, blockSel)) return;
        handling = EnumHandling.PreventDefault;
        handHandling = EnumHandHandling.PreventDefault;
        byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), entityPlayer, entityPlayer.Player, 0.3f);
    }
    
    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
        EntitySelection entitySel, ref EnumHandling handling)
    {
        if (byEntity is not EntityPlayer) return false;
        handling = EnumHandling.PreventDefault;
        byEntity.StartAnimation("knifecut");
        return secondsUsed < 1.5;
    }
    
    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
        EntitySelection entitySel, ref EnumHandling handling)
    {
        if (secondsUsed < 1.5) return;
        if (byEntity.World.Side != EnumAppSide.Server) return;
        if (byEntity is not EntityPlayer entityPlayer) return;
        var groundStorage = TryGetSelectedGroundStorage(entityPlayer, blockSel);
        if (!TryGetCrucibleStack(entityPlayer, blockSel, out var crucibleStack, out var crucibleSlot)) return;
        var playerInventory = entityPlayer.Player.InventoryManager;
        var activeSlot = playerInventory.ActiveHotbarSlot;
        // Check that player is interacting with a valid crucible
        if (activeSlot?.Itemstack is null ) return;
        var world = entityPlayer.World;
        if (!TryGetOutputStack(crucibleStack, world, out var outputStack)) return;
        var outputUnits = crucibleStack.Attributes.GetInt("units");
        var outputBitCount = outputUnits / 5;
        var (metalVariant, metalTier) = GetMetalVariantAndTier(byEntity, outputStack);
        var metalBitStack = new ItemStack(world.GetItem("game:metalbit-copper").ItemWithVariant("metal", metalVariant), outputBitCount);
        metalBitStack.SetTemperatureFrom(world, crucibleStack);
        var emptyCrucibleStack = new ItemStack(world.GetBlock(crucibleStack.Collectible.CodeWithVariant("type", "burned")));
        if (!playerInventory.TryGiveItemstack(metalBitStack, true))
            world.SpawnItemEntity(metalBitStack, blockSel.Position);
        crucibleSlot.Itemstack = emptyCrucibleStack;
        crucibleSlot.MarkDirty();
        groundStorage.MarkDirty();
        var chiselDamage = (2 + metalTier) * outputBitCount;
        activeSlot.Itemstack.Collectible.DamageItem(world, entityPlayer, activeSlot, chiselDamage);
        activeSlot.MarkDirty();
        byEntity.StopAnimation("knifecut");
    }

    private static (string metalVariant, int metalTier) GetMetalVariantAndTier(EntityAgent byEntity, ItemStack outputStack)
    {
        var metalVariant = outputStack.Collectible.GetMetalMaterial();
        byEntity.Api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(metalVariant, out var metalProperty);
        var metalTier = metalProperty?.Tier ?? 0;
        return (metalVariant, metalTier);
    }

    private static bool TryGetOutputStack(ItemStack crucibleStack, IWorldAccessor world, out ItemStack outputStack)
    {
        outputStack = null;
        if (!crucibleStack.Attributes.TryGetAttribute("output", out var output) ||
            crucibleStack.Attributes.TryGetInt("units") is < 5)
            return false;
        var outputJsonItemStack = JsonObject.FromJson(output.ToJsonToken()).AsObject<JsonItemStack>();
        if (!outputJsonItemStack.Resolve(world, $"[{Core.ModId}] CollectibleBehaviorScrapeCrucible.TryGetOutputStack"))
            return false;
        outputStack = outputJsonItemStack.ResolvedItemstack;
        return true;
    }

    private static bool IsSelectingValidCrucible(EntityPlayer entityPlayer, BlockSelection blockSel)
    {
        var groundStorage = TryGetSelectedGroundStorage(entityPlayer, blockSel);
        var world = entityPlayer.World;
        if (groundStorage?.GetSlotAt(blockSel) is not { Itemstack: { } itemStack }) return false;
        return itemStack.IsSmeltedContainer() && itemStack.GetTemperature(world) < MaxScrapeTemperature;
    }

    private static bool TryGetCrucibleStack(EntityPlayer entityPlayer, BlockSelection blockSel, out ItemStack crucibleStack, out ItemSlot atSlot)
    {
        crucibleStack = null;
        atSlot = null;
        var groundStorage = TryGetSelectedGroundStorage(entityPlayer, blockSel);
        if (groundStorage?.GetSlotAt(blockSel) is not { Itemstack: { } itemStack } targetSlot) return false;
        crucibleStack = itemStack;
        atSlot = targetSlot;
        return true;
    }

    private static BlockEntityGroundStorage TryGetSelectedGroundStorage(EntityPlayer entityPlayer, BlockSelection blockSel)
    {
        var blockEntity = entityPlayer.World.BlockAccessor.GetBlockEntity(blockSel.Position);
        return blockEntity as BlockEntityGroundStorage;
    }

    private static bool CanAccessBlock(EntityPlayer entityPlayer, BlockSelection blockSel)
    {
        var modSystem = entityPlayer.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
        return modSystem?.IsReinforced(blockSel.Position) != true &&
               entityPlayer.World.Claims.TryAccess(entityPlayer.Player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak);
    }
    
}
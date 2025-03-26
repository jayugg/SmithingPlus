using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace SmithingPlus.BitsRecovery;

// TODO Needs refactoring
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
        ModSystemBlockReinforcement modSystem = byEntity.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
        var player = entityPlayer.Player;
        // Check if player can access the block
        if ((modSystem != null ? modSystem.IsReinforced(blockSel.Position) ? 1 : 0 : 0) != 0) 
            return;
        if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            return;
        var blockEntity = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
        if (blockEntity is not BlockEntityGroundStorage groundStorage) return;
        var world = entityPlayer.World;
        if (groundStorage.GetSlotAt(blockSel) is not { Itemstack: { } crucibleStack } || 
            crucibleStack.IsSmeltedContainer() != true || crucibleStack.GetTemperature(world) > MaxScrapeTemperature) return;
        handling = EnumHandling.PreventDefault;
        handHandling = EnumHandHandling.PreventDefault;
        if (byEntity.World.Side != EnumAppSide.Client) return;
        byEntity.World.PlaySoundFor(new AssetLocation("sounds/effect/toolbreak"), entityPlayer.Player, 0.3f);
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
        var player = entityPlayer.Player;
        var blockEntity = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
        if (blockEntity is not BlockEntityGroundStorage groundStorage) return;
        var world = entityPlayer.World;
        if (groundStorage.GetSlotAt(blockSel) is not { Itemstack: { } crucibleStack } targetSlot || 
            crucibleStack.IsSmeltedContainer() != true || crucibleStack.GetTemperature(world) > MaxScrapeTemperature) return;
        var activeSlot = player.InventoryManager.ActiveHotbarSlot;
        // Check that player is interacting with a valid crucible
        if (activeSlot?.Itemstack is null ) return;
        if (!crucibleStack.Attributes.TryGetAttribute("output", out var output) ||
            crucibleStack.Attributes.TryGetInt("units") is < 5)
            return;
        var outputJsonItemStack = JsonObject.FromJson(output.ToJsonToken()).AsObject<JsonItemStack>();
        if (!outputJsonItemStack.Resolve(world, $"[{Core.ModId}] CollectibleBehaviorScrapeCrucible.OnPlayerInteractStop"))
            return;
        // Compute the output of scraping the crucible
        var outputStack = outputJsonItemStack.ResolvedItemstack;
        var outputUnits = crucibleStack.Attributes.GetInt("units");
        var outputBits = outputUnits / 5;
        var metalVariant = outputStack.Collectible.GetMetalMaterial();
        byEntity.Api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(metalVariant, out var metalProperty);
        var metalTier = metalProperty?.Tier ?? 0;
        var metalBitStack = new ItemStack(world.GetItem("game:metalbit-copper").ItemWithVariant("metal", metalVariant), outputBits);
        metalBitStack.SetTemperatureFrom(world, crucibleStack);
        var emptyCrucibleStack = new ItemStack(world.GetBlock("game:crucible-burned"));
        if (!player.InventoryManager.TryGiveItemstack(metalBitStack, true))
            world.SpawnItemEntity(metalBitStack, blockSel.Position);
        targetSlot.Itemstack = emptyCrucibleStack;
        activeSlot.Itemstack.Collectible.DamageItem(world, player.Entity, activeSlot, (2 + metalTier) * outputBits);
        activeSlot.MarkDirty();
        targetSlot.MarkDirty();
        groundStorage.MarkDirty();
        byEntity.StopAnimation("knifecut");
    }
}
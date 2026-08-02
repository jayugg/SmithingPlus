#nullable enable
using System.Linq;
using System.Text;

using SmithingPlus.Common.Metal;
using SmithingPlus.Util;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus.ToolRecovery;

public class CollectibleBehaviorRepairableTool : CollectibleBehavior
{
    public CollectibleBehaviorRepairableTool(CollectibleObject collObj) : base(collObj)
    {
    }

    protected virtual string LangKey => "Repaired";

    public override void GetHeldItemInfo(ItemSlot? inSlot, StringBuilder dsc, IWorldAccessor world,
        bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        var itemstack = inSlot?.Itemstack;
        var collectible = itemstack?.Collectible;
        var code = collectible?.Code;

        if (code == null || inSlot == null)
        {
            Core.Logger.Error("Failed to get code for itemstack {0}", itemstack);
            return;
        }

        if (!WildcardUtil.Match(Core.Config.RepairableToolSelector, code.ToString()))
            return;

        var brokenCount = itemstack!.GetBrokenCount();
        if (brokenCount <= 0) return;

        if (Core.Config.ShowRepairedCount) dsc.AppendLine(Lang.Get($"{LangKey} {{0}} times", brokenCount));
        if (Core.Config.ShowRepairSmithName && itemstack!.GetRepairSmith() is { } repairSmith)
            dsc.AppendLine(Lang.Get("Last repaired by {0}", repairSmith));
    }

    public override void OnDestroyItem(IWorldAccessor world, Entity byEntity, ItemSlot itemSlot, ref EnumHandling bhHandling)
    {
        if (world.Api.Side.IsClient())
            return;

        var durability = itemSlot.Itemstack?.GetRemainingDurability();
        if (durability is null or > 0) return; // Was this item (likely) destroyed due to its durability reaching zero?

        Core.Logger.VerboseDebug(
            "Broken tool in InventoryID: {0}, Entity: {1}",
            itemSlot.Inventory?.InventoryID,
            byEntity.GetName()
        );
        var entityPlayer = byEntity as EntityPlayer;
        var itemStack = itemSlot.Itemstack;
        if (itemStack is null) return;

        var toolCode = itemStack.Collectible.Code.ToString();
        var smithingRecipe = CacheHelper.GetOrAdd(
            Core.ToolToRecipeCache,
            toolCode,
            () => GetHeadSmithingRecipe(world.Api, itemStack)
        );

        if (smithingRecipe == null)
        {
            Core.Logger.VerboseDebug("Head or tool smithing recipe not found for: {0}", toolCode);
            return;
        }

        var metalMaterial = itemStack.GetOrCacheMetalMaterial(byEntity.Api);
        var workItem = metalMaterial?.WorkItem;

        if (workItem is null)
        {
            Core.Logger.VerboseDebug(
                $"Work item not found. Metal material: {metalMaterial?.IngotCode}, " + $"collectible: {itemStack.Collectible.Code}"
            );
            return;
        }

        Core.Logger.VerboseDebug("Found work item: {0}", workItem.Code);
        var wItemStack = new ItemStack(workItem);
        Core.Logger.VerboseDebug(
            "Found smithing recipe: {0}",
            smithingRecipe.Output.ResolvedItemstack!.Collectible.Code
        );
        var byteVoxels = ByteVoxelsFromRecipe(smithingRecipe, smithingRecipe.Output.ResolvedItemstack.StackSize);
        wItemStack.Attributes.SetBytes("voxels", BlockEntityAnvil.serializeVoxels(byteVoxels));
        wItemStack.Attributes.SetInt("selectedRecipeId", smithingRecipe.RecipeId);
        var cloneStack = itemStack.Clone();
        cloneStack.CloneBrokenCount(itemStack, 1);
        wItemStack.SetRepairedToolStack(cloneStack);

        var gaveStack = false;
        if (entityPlayer != null) gaveStack = entityPlayer.TryGiveItemStack(wItemStack);
        if (!gaveStack) world.SpawnItemEntity(wItemStack, byEntity.Pos.XYZ);
        Core.Logger.VerboseDebug(
            gaveStack ? "Gave work item {0} to player {1}" : "Dropped work item {0} to player {1}",
            wItemStack.Collectible.Code,
            entityPlayer?.Player.PlayerName
        );
        itemSlot.MarkDirty();
    }

    private static SmithingRecipe? GetHeadSmithingRecipe(ICoreAPI api, ItemStack itemStack)
    {
        var toolHead = GetToolHead(api, itemStack);
        var smithingRecipe = toolHead.GetSmithingRecipe(api);
        return smithingRecipe;
    }

    private static ItemStack GetToolHead(ICoreAPI api, ItemStack itemStack)
    {
        var toolRecipe = itemStack
            .GetGridRecipes(api)
            .FirstOrDefault(r =>
                r.Output?.ResolvedItemStack?.StackSize == 1
            );
        var toolHead = toolRecipe?.RecipeIngredients
            .FirstOrDefault(k =>
                k.ResolvedItemStack?.Collectible?.HasBehavior<CollectibleBehaviorRepairableToolHead>() ?? false
            )
            ?.ResolvedItemStack;

        if (toolHead == null)
        {
            toolHead = itemStack;
            Core.Logger.VerboseDebug("Tool head not found for: {0}", itemStack);
        }

        Core.Logger.VerboseDebug("Tool head: {0}", toolHead);
        return toolHead;
    }

    private static byte[,,] ByteVoxelsFromRecipe(SmithingRecipe recipe, int stackSize = 1)
    {
        var recipeVoxels = recipe.Voxels;
        if (Core.Config.BrokenToolVoxelPercent < 0.2)
            Core.Logger.Warning(
                $"[ItemDamagedPatches#ByteVoxelsFromRecipe] Config setting {nameof(Core.Config.BrokenToolVoxelPercent)}"
                + $"has a very low value, your broken tools well be almost or fully empty."
            );
        var byteVoxels = recipeVoxels.ErodeToPercentage(Core.Config.BrokenToolVoxelPercent);
        return byteVoxels;
    }
}

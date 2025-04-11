using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.Util;

#nullable enable
public class MetalMaterial
{
    private readonly AssetLocation? _ingotCode;
    private ItemStack? _ingotStack;

    public AssetLocation IngotCode => _ingotCode;
    public ICoreAPI Api { get; }
    public string Variant => IngotCode.EndVariant();
    public int Tier => Api.GetModSystem<SurvivalCoreSystem>()?.metalsByCode
        .TryGetValue(Variant, out var metalProperty) == true
        ? metalProperty?.Tier ?? 0
        : 0;
    public bool Resolved => IngotItem != null;
    public ItemIngot? IngotItem => Api.World.GetItem(IngotCode) as ItemIngot;
    private Item? MetalBitItem => TryGetItem("metalbit");
    public ItemWorkItem? WorkItem => TryGetItem("workitem") as ItemWorkItem;

    public ItemStack? MetalBitStack => MetalBitItem != null ? new ItemStack(MetalBitItem) : null;
    public ItemStack? IngotStack => _ingotStack ??= IngotItem != null ? new ItemStack(IngotItem) : null;
    
    public MetalMaterial(ICoreAPI api, AssetLocation ingotCode)
    {
        Api = api;
        _ingotCode = ingotCode;
        _ingotStack = IngotStack;
        if (!Resolved)
            Core.Logger.Error("[MetalMaterial] Failed to load ingot item {0}", IngotCode);
    }

    public MetalMaterial(ICoreAPI api, ItemStack ingotStack)
    {
        Api = api;
        _ingotStack = ingotStack;
        _ingotCode = ingotStack.Collectible.Code;

        if (!Resolved)
            Core.Logger.Error("[MetalMaterial] Failed to load ingot item {0}", IngotCode);
    }
    
    private Item? TryGetItem(string firstPathPart)
    {
        return Api.World.GetItem(new AssetLocation($"{IngotCode.Domain}:{firstPathPart}-{Variant}")) ??
               Api.World.GetItem(new AssetLocation($"{IngotCode.Domain}:{firstPathPart}-{IngotCode.Path}")) ??
               Api.World.GetItem(new AssetLocation($"game:{firstPathPart}-{Variant}")) ??
               Api.World.GetItem(new AssetLocation($"game:{firstPathPart}-{IngotCode.Path}"));
    }
    
    public bool Equals(MetalMaterial? other)
    {
        if (other == null) return false;
        if (IngotCode.Equals(other.IngotCode)) return true;
        return IngotStack?.Collectible.Code.Equals(other.IngotStack?.Collectible.Code) ?? false;
    }
}
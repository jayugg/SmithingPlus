using System;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.Metal;

#nullable enable
public class MetalMaterial : IEquatable<MetalMaterial>
{
    private readonly ICoreAPI _api;
    private ItemStack? _ingotStack;

    public MetalMaterial(ICoreAPI api, AssetLocation ingotCode)
    {
        _api = api;
        IngotCode = ingotCode;
        if (!Resolved)
            Core.Logger.Error("[MetalMaterial] Failed to load ingot item {0}", IngotCode);
    }

    public MetalMaterial(ICoreAPI api, ItemStack ingotStack)
    {
        _api = api;
        _ingotStack = ingotStack;
        IngotCode = ingotStack.Collectible.Code;
        if (!Resolved)
            Core.Logger.Error("[MetalMaterial] Failed to load ingot item {0}", IngotCode);
    }

    public AssetLocation IngotCode { get; }

    public string Variant => IngotCode.EndVariant();

    public int Tier => _api.GetModSystem<SurvivalCoreSystem>()?.metalsByCode
        .TryGetValue(Variant, out var metalProperty) == true
        ? metalProperty?.Tier ?? 0
        : 0;

    public bool Resolved => IngotItem is not null;
    private ItemIngot? IngotItem => _api.World.GetItem(IngotCode) as ItemIngot;
    private Item? MetalBitItem => TryGetItem("metalbit");
    public ItemWorkItem? WorkItem => TryGetItem("workitem") as ItemWorkItem;
    public ItemStack? MetalBitStack => MetalBitItem != null ? new ItemStack(MetalBitItem) : null;
    public ItemStack? IngotStack => _ingotStack ??= IngotItem != null ? new ItemStack(IngotItem) : null;
    public ItemStack? WorkItemStack => WorkItem != null ? new ItemStack(WorkItem) : null;

    public bool Equals(MetalMaterial? other)
    {
        if (other is null) return false;
        return IngotCode.Equals(other.IngotCode) ||
               (IngotStack?.Collectible.Code.Equals(other.IngotStack?.Collectible.Code) ?? false);
    }

    private Item? TryGetItem(string firstPathPart)
    {
        return _api.World.GetItem(new AssetLocation($"{IngotCode.Domain}:{firstPathPart}-{Variant}")) ??
               _api.World.GetItem(new AssetLocation($"{IngotCode.Domain}:{firstPathPart}-{IngotCode.Path}")) ??
               _api.World.GetItem(new AssetLocation($"game:{firstPathPart}-{Variant}")) ??
               _api.World.GetItem(new AssetLocation($"game:{firstPathPart}-{IngotCode.Path}"));
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as MetalMaterial);
    }

    public override int GetHashCode()
    {
        return IngotCode.GetHashCode();
    }
}
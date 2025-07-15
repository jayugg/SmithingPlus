using System;
using Newtonsoft.Json;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.Metal;

#nullable enable
[JsonObject(MemberSerialization.OptIn)]
public class MetalMaterial : IEquatable<MetalMaterial>
{
    // These json properties might be null, fallback uses classic vanilla naming conventions
    [JsonProperty("ingot")] private AssetLocation? _ingotCode;
    [JsonProperty("metalbit")] private AssetLocation? _metalBitCode;
    [JsonProperty("tier")] private int? _tier;
    [JsonProperty("workitem")] private AssetLocation? _workItemCode;
    [JsonProperty("code")] public required AssetLocation Code { get; init; }
    public bool Resolved { get; private set; }
    public string Variant => Code.Path;
    public AssetLocation IngotCode => _ingotCode ?? new AssetLocation(Code.Domain, $"ingot-{Variant}");
    public AssetLocation MetalBitCode => _metalBitCode ?? new AssetLocation(Code.Domain, $"metalbit-{Variant}");
    public AssetLocation WorkItemCode => _workItemCode ?? new AssetLocation(Code.Domain, $"workitem-{Variant}");
    public ItemIngot? IngotItem { get; private set; }
    public Item? MetalBitItem { get; private set; }
    public ItemWorkItem? WorkItem { get; private set; }
    public ItemStack? IngotStack => IngotItem != null ? new ItemStack(IngotItem) : null;
    public ItemStack? MetalBitStack => MetalBitItem != null ? new ItemStack(MetalBitItem) : null;
    public ItemStack? WorkItemStack => WorkItem != null ? new ItemStack(WorkItem) : null;
    public int Tier { get; private set; }

    public bool Equals(MetalMaterial? other)
    {
        return other is not null && Code.Equals(other._ingotCode);
    }

    /// <summary>
    ///     Resolves the items and stacks using the provided API.
    ///     Sets the resolved flag to true if the ingot is successfully loaded.
    /// </summary>
    public void Resolve(ICoreAPI api)
    {
        IngotItem = api.World.GetItem(IngotCode) as ItemIngot;
        MetalBitItem = api.World.GetItem(MetalBitCode);
        WorkItem = api.World.GetItem(WorkItemCode) as ItemWorkItem;
        Tier = _tier ?? GetTier(api);
        if (IngotItem != null)
        {
            Resolved = true;
        }
        else
        {
            var ingot = api.World.GetItem(new AssetLocation("game:ingot-copper"));
            api.Logger.Warning("Ingot: " + ingot?.Code);
            Resolved = false;
            api.Logger.Error(
                $"[MetalMaterial] Failed to resolve ingot item {IngotCode} for metal material {Code}");
        }

        if (MetalBitItem == null)
            api.Logger.Warning(
                $"[MetalMaterial] Failed to resolve metal bit item {MetalBitCode} for metal material {Code}");
        if (WorkItem == null)
            api.Logger.Warning(
                $"[MetalMaterial] Failed to resolve work item {WorkItemCode} for metal material {Code}");
    }

    private int GetTier(ICoreAPI api)
    {
        return api.GetModSystem<SurvivalCoreSystem>()?.metalsByCode
            .TryGetValue(Variant, out var metalProperty) == true
            ? metalProperty?.Tier ?? 0
            : 0;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as MetalMaterial);
    }

    public override int GetHashCode()
    {
        return Code.GetHashCode();
    }
}
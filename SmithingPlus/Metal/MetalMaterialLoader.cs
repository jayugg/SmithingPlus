using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SmithingPlus.Util;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.Metal;

#nullable enable
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public class MetalMaterialLoader : ModSystem
{
    private readonly Dictionary<AssetLocation, MetalMaterial> _metalMaterials = new();
    public Dictionary<AssetLocation, MetalMaterial> ResolvedMaterials { get; private set; } = new();

    public override double ExecuteOrder()
    {
        return 0.06; // Runs after SurvivalCoreSystem
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        Core.Logger.Notification("[MetalMaterial] Loading custom metal materials...");
        foreach (var (assetLocation, metalMaterials) in api.Assets.GetMany<MetalMaterial[]>(api.Logger,
                     "config/metalmaterials.json"))
        foreach (var metalMaterial in metalMaterials)
        {
            Core.Logger.Notification(
                $"[MetalMaterial] Loading metal material {metalMaterial.Code} from {assetLocation}");
            if (!_metalMaterials.TryAdd(metalMaterial.Code, metalMaterial))
                Core.Logger.Warning(
                    $"[MetalMaterial] Duplicate metal material found {metalMaterial.Code} at location {assetLocation}. Ignoring duplicate.");
        }

        var metalsByCode = api.GetModSystem<SurvivalCoreSystem>()?.metalsByCode;
        if (metalsByCode == null) return;
        Core.Logger.Notification("[MetalMaterial] Loading metal materials from worldproperties/block/metal ...");
        foreach (var metalVariant in metalsByCode.Keys)
        {
            if (_metalMaterials.ContainsKey(new AssetLocation(metalVariant)))
            {
                Core.Logger.Warning(
                    $"[MetalMaterial] Default metal material {metalVariant} overridden by custom-defined material.");
                continue;
            }

            // Warn if multiple materials have the same variant but different domains
            var similarMaterials = _metalMaterials.Values.Where(m => m.Variant == metalVariant).ToArray();
            if (similarMaterials.Any())
                Core.Logger.Warning(
                    $"[MetalMaterial] Warning: Found multiple metal materials with the same variant and different domains'{metalVariant}': {string.Join(", ", similarMaterials.Select(m => m.Code))}.");
            var metalMaterial = new MetalMaterial
            {
                Code = new AssetLocation(metalVariant)
            };
            _metalMaterials.TryAdd(metalVariant, metalMaterial);
        }

        Core.Logger.Notification(
            $"[MetalMaterial] Loaded {_metalMaterials.Count} metal materials: {string.Join(", ", _metalMaterials.Keys)}");
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        Core.Logger.Notification("[MetalMaterial] Resolving metal materials...");
        var resolvedCount = 0;
        foreach (var metalMaterial in _metalMaterials.Values)
        {
            metalMaterial.Resolve(api);
            if (metalMaterial.Resolved) resolvedCount++;
        }

        ResolvedMaterials = _metalMaterials
            .Where(kvp => kvp.Value.Resolved)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Core.Logger.Notification("[Metal Material] Done resolving metal materials.");
        Core.Logger.Notification(
            $"[Metal Material] Resolved {resolvedCount} out of {_metalMaterials.Count} metal materials.");
    }

    public static MetalMaterial? GetMaterial(ICoreAPI api, AssetLocation code)
    {
        var resolvedMaterials = api.GetModSystem<MetalMaterialLoader>()?.ResolvedMaterials;
        if (resolvedMaterials == null)
        {
            Core.Logger.VerboseDebug(
                "[MetalMaterial] Metal materials not resolved. You might be calling GetMaterial too early.");
            return null;
        }

        if (resolvedMaterials.TryGetValue(code, out var material)) return material;
        Core.Logger.VerboseDebug($"[MetalMaterial] Metal material {code} not found. Will return null.");
        return null;
    }
}
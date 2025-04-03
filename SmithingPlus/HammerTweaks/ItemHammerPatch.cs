using System;
using System.Linq;
using Cairo;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus.HammerTweaks;

[HarmonyPatchCategory(Core.HammerTweaksCategory)]
[HarmonyPatch(typeof(ItemHammer))]
public class ItemHammerPatch : ModSystem
{
    public static int OriginalToolModesCount = -1;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ItemHammer.GetToolModes))]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix_GetToolModes(ItemHammer __instance, ItemSlot slot,
        IClientPlayer forPlayer, BlockSelection blockSel, ref SkillItem[] __result, ref SkillItem[] ___toolModes)
    {
        try
        {
            if (forPlayer?.Entity?.Api is not ICoreClientAPI capi) return;
            if (__result is null) return;

            if (___toolModes is not null)
            {
                // Store original tool modes count
                if (OriginalToolModesCount < 0)
                    OriginalToolModesCount = ___toolModes.Length;
                // If configuration is toggled off, remove extra tool mode added by this mod
                if (!Core.Config.HammerTweaks)
                {
                    if (___toolModes.Length > OriginalToolModesCount)
                        __result = ___toolModes = ___toolModes.Take(OriginalToolModesCount).ToArray();

                    if (__instance.GetToolMode(slot, forPlayer, blockSel) < OriginalToolModesCount)
                        return;
                    __instance.SetToolMode(slot, forPlayer, blockSel, 0);
                    return;
                }
                // Only add new toolmode if it hasn’t been added yet.
                if (___toolModes.Length > OriginalToolModesCount) return;
            }
            
            var newModes= GetOrCreateFlipToolMode(capi);
            __result = ___toolModes = ___toolModes?.Concat(newModes).ToArray() ?? newModes;
        }
        catch (ArgumentNullException ex)
        {
            Core.Logger.Error(ex);
        }
    }
    
    private static SkillItem[] GetOrCreateFlipToolMode(ICoreClientAPI capi)
    {
        return ObjectCacheUtil.GetOrCreate(capi, "extraHammerToolModes", () => new[]
        {
            new SkillItem
            {
                Code = new AssetLocation("flip"),
                Name = Lang.Get("Flip")
            }.WithIcon(capi, DrawFlipSvg)
        });
    }
    
    private static void DrawFlipSvg(
        Context cr,
        int x,
        int y,
        float canvasWidth,
        float canvasHeight,
        double[] rgba)
    {
        var matrix1 = cr.Matrix;
        cr.Save();
        const float iconWidth = 11f;
        const float iconHeight = 8f;
        var scaling = Math.Min(canvasWidth / iconWidth, canvasHeight / iconHeight);
        var transX = x + canvasWidth / 2 + iconHeight / 2;
        var transY = y;
        // Define the pivot as the center of the unscaled shape.
        var pivotX = iconWidth / 2.0;
        var pivotY = iconHeight / 2.0;

        // First, translate to the upper‒left.
        matrix1.Translate(transX, transY);
        // Move to the pivot.
        matrix1.Translate(pivotX, pivotY);
        // Rotate 90° (π/2 radians) around the pivot.
        matrix1.Rotate(Math.PI / 2);
        // Move back from the pivot.
        matrix1.Translate(-pivotX, -pivotY);
        // Apply scaling.
        matrix1.Scale(scaling, scaling);

        cr.Matrix = matrix1;
        cr.Operator = Operator.Over;
        Pattern source1 = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
        cr.SetSource(source1);
        cr.NewPath();
        cr.MoveTo(0.566406, 1.558594);
        cr.LineTo(229.0 / 64.0, 1.558594);
        cr.LineTo(229.0 / 64.0, 6.441406);
        cr.LineTo(0.566406, 6.441406);
        cr.ClosePath();
        cr.MoveTo(0.566406, 1.558594);
        cr.Tolerance = 0.1;
        cr.Antialias = Antialias.Default;
        cr.FillRule = FillRule.Winding;
        cr.FillPreserve();
        source1.Dispose();
        cr.Operator = Operator.Over;
        cr.LineWidth = 1.0;
        cr.MiterLimit = 10.0;
        cr.LineCap = LineCap.Butt;
        cr.LineJoin = LineJoin.Miter;
        Pattern source2 = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
        cr.SetSource(source2);
        cr.NewPath();
        cr.MoveTo(0.566406, 1.558594);
        cr.LineTo(229.0 / 64.0, 1.558594);
        cr.LineTo(229.0 / 64.0, 6.441406);
        cr.LineTo(0.566406, 6.441406);
        cr.ClosePath();
        cr.MoveTo(0.566406, 1.558594);
        cr.Tolerance = 0.1;
        cr.Antialias = Antialias.Default;
        var matrix2 = new Matrix(1.038961, 0.0, 0.0, 1.038961, 0.0454545, 0.0);
        source2.Matrix = matrix2;
        cr.StrokePreserve();
        source2.Dispose();
        cr.Operator = Operator.Over;
        cr.LineWidth = 1.0;
        cr.MiterLimit = 10.0;
        cr.LineCap = LineCap.Butt;
        cr.LineJoin = LineJoin.Miter;
        Pattern source3 = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
        cr.SetSource(source3);
        cr.NewPath();
        cr.MoveTo(5.550781, 0.0);
        cr.LineTo(5.550781, 8.0);
        cr.Tolerance = 0.1;
        cr.Antialias = Antialias.Default;
        var matrix3 = new Matrix(1.038961, 0.0, 0.0, 1.038961, 0.0454545, 0.0);
        source3.Matrix = matrix3;
        cr.StrokePreserve();
        source3.Dispose();
        cr.Operator = Operator.Over;
        Pattern source4 = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
        cr.SetSource(source4);
        cr.NewPath();
        cr.MoveTo(475.0 / 64.0, 1.558594);
        cr.LineTo(10.433594, 1.558594);
        cr.LineTo(10.433594, 6.441406);
        cr.LineTo(475.0 / 64.0, 6.441406);
        cr.ClosePath();
        cr.MoveTo(475.0 / 64.0, 1.558594);
        cr.Tolerance = 0.1;
        cr.Antialias = Antialias.Default;
        cr.FillRule = FillRule.Winding;
        cr.FillPreserve();
        source4.Dispose();
        cr.Operator = Operator.Over;
        cr.LineWidth = 1.0;
        cr.MiterLimit = 10.0;
        cr.LineCap = LineCap.Butt;
        cr.LineJoin = LineJoin.Miter;
        Pattern source5 = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
        cr.SetSource(source5);
        cr.NewPath();
        cr.MoveTo(475.0 / 64.0, 1.558594);
        cr.LineTo(10.433594, 1.558594);
        cr.LineTo(10.433594, 6.441406);
        cr.LineTo(475.0 / 64.0, 6.441406);
        cr.ClosePath();
        cr.MoveTo(475.0 / 64.0, 1.558594);
        cr.Tolerance = 0.1;
        cr.Antialias = Antialias.Default;
        var matrix4 = new Matrix(1.038961, 0.0, 0.0, 1.038961, 0.0454545, 0.0);
        source5.Matrix = matrix4;
        cr.StrokePreserve();
        source5.Dispose();
        cr.Restore();
    }

    public override void Dispose()
    {
        OriginalToolModesCount = -1;
        base.Dispose();
    }
}
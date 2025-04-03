using System;
using System.Linq;
using Cairo;
using HarmonyLib;
using JetBrains.Annotations;
using SmithingPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SmithingPlus.HammerTweaks;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[HarmonyPatch(typeof(ItemHammer)), HarmonyPatchCategory(Core.HammerTweaksCategory)]
public class ItemHammerPatch
{
    private const string ToolModeCacheKey = $"{Core.ModId}:extraHammerToolModes";
    
    [HarmonyPostfix, HarmonyPatch(nameof(ItemHammer.GetToolModes)), HarmonyPriority(Priority.Last)]
    public static void Postfix_GetToolModes(ItemHammer __instance, ItemSlot slot,
        IClientPlayer forPlayer, BlockSelection blockSel, ref SkillItem[] __result, ref SkillItem[] ___toolModes)
    {
        try
        {
            if (!Core.Config.HammerTweaks) return;
            if (forPlayer?.Entity?.Api is not ICoreClientAPI capi) return;
            if (__result is null || slot.Itemstack is null) return;
            if (___toolModes is not null)
            {
                var originalToolModesCount = HammerTweaksNetwork.OriginalToolModesCount ??= ___toolModes.Length;
                // Store original tool modes count
                slot.Itemstack.TempAttributes.SetInt(ModAttributes.FlipItemToolMode, originalToolModesCount);
                // Sync attribute the server
                HammerTweaksNetwork.SendFlipToolMode(capi, originalToolModesCount);
                // Only add new toolmode if it hasn’t been added yet.
                if (___toolModes.Length > originalToolModesCount) return; 
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
        return ObjectCacheUtil.GetOrCreate(capi, ToolModeCacheKey, () => new[]
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
      Matrix matrix1 = cr.Matrix;
      cr.Save();
      float num1 = 119f;
      float num2 = 115f;
      float num3 = Math.Min(canvasWidth / num1, canvasHeight / num2);
      matrix1.Translate((double) x + (double) Math.Max(0.0f, (float) (((double) canvasWidth - (double) num1 * (double) num3) / 2.0)), (double) y + (double) Math.Max(0.0f, (float) (((double) canvasHeight - (double) num2 * (double) num3) / 2.0)));
      matrix1.Scale((double) num3, (double) num3);
      cr.Matrix = matrix1;
      cr.Operator = Operator.Over;
      cr.LineWidth = 15.0;
      cr.MiterLimit = 10.0;
      cr.LineCap = LineCap.Butt;
      cr.LineJoin = LineJoin.Miter;
      Pattern source1 = (Pattern) new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
      cr.SetSource(source1);
      cr.NewPath();
      cr.MoveTo(100.761719, 29.972656);
      cr.CurveTo(7429.0 / 64.0, 46.824219, 111.929688, 74.050781, 3137.0 / 32.0, 89.949219);
      cr.CurveTo(78.730469, 112.148438, 45.628906, 113.027344, 23.527344, 93.726563);
      cr.CurveTo(-13.023438, 56.238281, 17.898438, 7.355469, 61.082031, 7.5);
      cr.Tolerance = 0.1;
      cr.Antialias = Antialias.Default;
      Matrix matrix2 = new Matrix(1.0, 0.0, 0.0, 1.0, 219.348174, -337.87843);
      source1.Matrix = matrix2;
      cr.StrokePreserve();
      source1?.Dispose();
      cr.Operator = Operator.Over;
      Pattern source2 = (Pattern) new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
      cr.SetSource(source2);
      cr.NewPath();
      cr.MoveTo(5241.0 / 64.0, 177.0 / 16.0);
      cr.CurveTo(86.824219, 21.769531, 91.550781, 36.472656, 92.332031, 47.808594);
      cr.LineTo(100.761719, 29.972656);
      cr.LineTo(118.585938, 21.652344);
      cr.CurveTo(107.269531, 20.804688, 5927.0 / 64.0, 15.976563, 5241.0 / 64.0, 177.0 / 16.0);
      cr.ClosePath();
      cr.MoveTo(5241.0 / 64.0, 177.0 / 16.0);
      cr.Tolerance = 0.1;
      cr.Antialias = Antialias.Default;
      cr.FillRule = FillRule.Winding;
      cr.FillPreserve();
      source2?.Dispose();
      cr.Restore();
    }
}
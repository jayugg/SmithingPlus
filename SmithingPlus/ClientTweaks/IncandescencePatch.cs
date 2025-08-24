using System;
using HarmonyLib;
using JetBrains.Annotations;
using Vintagestory.API.MathTools;

namespace SmithingPlus.ClientTweaks;

[UsedImplicitly]
[HarmonyPatchCategory(Core.NeverPatchCategory)] // Turn off patch for now, need to tweak values further
public class IncandescencePatch
{
    // Wien's displacement constant (m·K)
    private const float WienB = 2.897e-3f;

    // Define the “visible glow” range
    private const double TminVisibleC = 300; // below ~300 °C almost no visible glow
    private const double TmaxVisibleC = 1500; // around here you’re near full intensity


    [HarmonyPostfix]
    [HarmonyPatch(typeof(ColorUtil), nameof(ColorUtil.GetIncandescenceColorAsColor4f))]
    public static void PatchSmithingInfo(int temperature, ref float[] __result)
    {
        if (temperature < 500)
        {
            __result = new float[4];
            return;
        }

        var incandescenceColor = GetIncandescenceColor(temperature);
        __result = new[]
        {
            incandescenceColor[0],
            incandescenceColor[1],
            incandescenceColor[2],
            GetIncandescenceAlpha(temperature)
        };
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ColorUtil), nameof(ColorUtil.getIncandescenceColor))]
    public static void PatchSmithingInfo(int temperature, ref int[] __result)
    {
        if (temperature < 520)
        {
            __result = new int[4];
            return;
        }

        var incandescenceColor = GetIncandescenceColor(temperature);
        __result = new[]
        {
            (int)Math.Clamp(byte.MaxValue * incandescenceColor[0], 0, byte.MaxValue),
            (int)Math.Clamp(byte.MaxValue * incandescenceColor[1], 0, byte.MaxValue),
            (int)Math.Clamp(byte.MaxValue * incandescenceColor[2], 0, byte.MaxValue),
            (int)Math.Clamp(byte.MaxValue * GetIncandescenceAlpha(temperature), 0, byte.MaxValue)
        };
    }

    /// <summary>
    /// Returns an RGBA color (components 0–1) approximating black‐body incandescence at the given temperature (°C).
    /// </summary>
    public static float[] GetIncandescenceColor(int temperatureC)
    {
        // 1) Convert to Kelvin
        var temperatureK = temperatureC + 273.15;

        // 2) Compute peak wavelength (m → nm)
        var wavelengthNm = WienB / temperatureK * 1e9;

        // 3) Convert to linear RGB [0,1]
        var (rLin, gLin, bLin) = WavelengthToLinearRgb(wavelengthNm);

        // 4) Convert to final float array with alpha = 1
        return new[]
        {
            (float)rLin,
            (float)gLin,
            (float)bLin,
            1f
        };
    }

    /// <summary>
    ///     α based on normalized T^4 between TminVisibleC and TmaxVisibleC.
    /// </summary>
    private static float GetIncandescenceAlpha(int temperatureC)
    {
        // map Celsius to 0–1
        var tNorm = (temperatureC - TminVisibleC) / (TmaxVisibleC - TminVisibleC);
        tNorm = Math.Clamp(tNorm, 0.0, 1.0);

        // use a power of 4 to mimic total radiative output ∝ T^4
        var alpha = Math.Pow(tNorm, 4.0);

        return (float)alpha;
    }

    // Maps a wavelength in the visible range (380–780 nm) to linear RGB, with intensity & gamma correction.
    private static (double r, double g, double b) WavelengthToLinearRgb(double wl)
    {
        double r = 0, g = 0, b = 0;

        if (wl >= 380 && wl < 440)
        {
            r = -(wl - 440) / (440 - 380);
            g = 0;
            b = 1;
        }
        else if (wl >= 440 && wl < 490)
        {
            r = 0;
            g = (wl - 440) / (490 - 440);
            b = 1;
        }
        else if (wl >= 490 && wl < 510)
        {
            r = 0;
            g = 1;
            b = -(wl - 510) / (510 - 490);
        }
        else if (wl >= 510 && wl < 580)
        {
            r = (wl - 510) / (580 - 510);
            g = 1;
            b = 0;
        }
        else if (wl >= 580 && wl < 645)
        {
            r = 1;
            g = -(wl - 645) / (645 - 580);
            b = 0;
        }
        else if (wl >= 645 && wl <= 780)
        {
            r = 1;
            g = 0;
            b = 0;
        }

        // Intensity factor to account for eye sensitivity near the ends
        double factor;
        if (wl >= 380 && wl < 420)
            factor = 0.3 + 0.7 * (wl - 380) / (420 - 380);
        else if (wl >= 420 && wl < 701)
            factor = 1.0;
        else if (wl >= 701 && wl <= 780)
            factor = 0.3 + 0.7 * (780 - wl) / (780 - 700);
        else
            factor = 0.0;

        // Gamma correction
        const double gamma = 0.8;
        r = r * factor > 0 ? Math.Pow(r * factor, gamma) : 0;
        g = g * factor > 0 ? Math.Pow(g * factor, gamma) : 0;
        b = b * factor > 0 ? Math.Pow(b * factor, gamma) : 0;

        return (r, g, b);
    }
}
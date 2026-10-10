using System;
using System.Collections.Generic;
using HarmonyLib;

namespace BetterBoplBalance.Patches;

// Shortens the animations for placing tesla coil and engine
[HarmonyPatch(typeof(PlantObjectOnPlatform))]
public class PlantObjectPatch
{
    const float DurationScale = 0.5f;
    static readonly Dictionary<SpriteFrame[], SpriteFrame[]> cache = [];

    // Modifies the animation data by evenly speeding the animation up
    static SpriteFrame[] Scaled(SpriteFrame[] orig)
    {
        if (!cache.TryGetValue(orig, out var scaled))
        {
            scaled = new SpriteFrame[orig.Length];
            for (int i = 0; i < orig.Length; i++)
            {
                int d = orig[i].frameDuration;
                int n = Math.Max(1, (int)Math.Round(Math.Abs(d) * DurationScale));
                scaled[i] = new SpriteFrame(orig[i].sprite, d < 0 ? -n : n);
            }
            cache[orig] = scaled;
        }
        return scaled;
    }

    static void ShortenAnim(PlantObjectOnPlatform __instance, string animName)
    {
        // If there's no ground, PlantObject exits early and never starts recover. This check stops the patch from scaling some other animation in that case.
        // Swapping only currentAnim is safe because the callback and the "fire on last frame" flag are separate fields that stay as they were.
        var anim = __instance.animator;
        if (anim.currentAnim == __instance.animData.GetAnimation(animName))
            anim.currentAnim = Scaled(anim.currentAnim);
    }

    [HarmonyPatch("OnEnterAbility")]
    [HarmonyPostfix]
    private static void ShortenCast(PlantObjectOnPlatform __instance) => ShortenAnim(__instance, "cast");

    [HarmonyPatch("PlantObject")]
    [HarmonyPostfix]
    private static void ShortenRecover(PlantObjectOnPlatform __instance) => ShortenAnim(__instance, "recover");
}

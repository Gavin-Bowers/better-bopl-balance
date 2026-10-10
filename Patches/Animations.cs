using System;
using System.Collections.Generic;
using BoplFixedMath;
using HarmonyLib;

namespace BetterBoplBalance.Patches;

// Shared helper for shortening ability animations
public static class AnimShortener
{
    // Keyed by the original array (reference equality) and the scale, so abilities can use different scales
    static readonly Dictionary<(SpriteFrame[], float), SpriteFrame[]> cache = [];

    // Modifies the animation data by evenly speeding the animation up
    static SpriteFrame[] Scaled(SpriteFrame[] orig, float durationScale)
    {
        if (!cache.TryGetValue((orig, durationScale), out var scaled))
        {
            scaled = new SpriteFrame[orig.Length];
            for (int i = 0; i < orig.Length; i++)
            {
                int d = orig[i].frameDuration;
                int n = Math.Max(1, (int)Math.Round(Math.Abs(d) * durationScale));
                scaled[i] = new SpriteFrame(orig[i].sprite, d < 0 ? -n : n);
            }
            cache[(orig, durationScale)] = scaled;
        }
        return scaled;
    }

    // Call right after the game starts animName. Only scales if that animation is the one playing,
    // so an early exit that started something else is left alone.
    // Swapping only currentAnim is safe because the callback and the "fire on last frame" flag are separate fields that stay as they were.
    public static void ShortenAnim(SpriteAnimator animator, AnimationData animData, string animName, float durationScale)
    {
        if (animator.currentAnim == animData.GetAnimation(animName))
            animator.currentAnim = Scaled(animator.currentAnim, durationScale);
    }

    // For abilities that start animations inside nested callbacks, where a postfix per call site isn't practical.
    // Registered arrays are swapped for scaled ones whenever any SpriteAnimator starts them (see SpriteAnimatorPatch).
    static readonly Dictionary<SpriteFrame[], SpriteFrame[]> replacements = [];

    public static void RegisterReplacement(AnimationData animData, string animName, float durationScale)
    {
        SpriteFrame[] orig = animData.GetAnimation(animName);
        replacements[orig] = Scaled(orig, durationScale);
    }

    public static bool TryGetReplacement(SpriteFrame[] orig, out SpriteFrame[] scaled) =>
        replacements.TryGetValue(orig, out scaled);
}

[HarmonyPatch(typeof(SpriteAnimator))]
public class SpriteAnimatorPatch
{
    [HarmonyPatch(nameof(SpriteAnimator.beginAnimThenDoAction), [typeof(SpriteFrame[]), typeof(Action)])]
    [HarmonyPrefix]
    private static void ReplaceAnim(ref SpriteFrame[] anim)
    {
        if (anim != null && AnimShortener.TryGetReplacement(anim, out var scaled))
            anim = scaled;
    }
}

// Shortens the animations for placing tesla coil and engine
[HarmonyPatch(typeof(PlantObjectOnPlatform))]
public class PlantObjectPatch
{
    const float DurationScale = 0.5f;

    [HarmonyPatch("OnEnterAbility")]
    [HarmonyPostfix]
    private static void ShortenCast(PlantObjectOnPlatform __instance) =>
        AnimShortener.ShortenAnim(__instance.animator, __instance.animData, "cast", DurationScale);

    // If there's no ground, PlantObject exits early and never starts recover
    [HarmonyPatch("PlantObject")]
    [HarmonyPostfix]
    private static void ShortenRecover(PlantObjectOnPlatform __instance) =>
        AnimShortener.ShortenAnim(__instance.animator, __instance.animData, "recover", DurationScale);
}

[HarmonyPatch(typeof(Spike))]
public class SpikePatch
{
    const float RecoverDurationScale = 0.5f;
    const float CastDurationScale = 0.5f;

    // If there's no ground, CastSpike exits early and never starts recover
    [HarmonyPatch("CastSpike")]
    [HarmonyPostfix]
    private static void ShortenRecover(Spike __instance) =>
        AnimShortener.ShortenAnim(__instance.animator, __instance.animData, "recover", RecoverDurationScale);
    
    [HarmonyPatch("OnEnterAbility")]
    [HarmonyPostfix]
    private static void ShortenSpikeAttack(Spike __instance) =>
        AnimShortener.ShortenAnim(__instance.animator, __instance.animData, "cast", CastDurationScale);
}

// Speeds up macho throw. Its animations chain through nested callbacks (enter -> grab -> pickup, beginThrow -> exit),
// all started with beginAnimThenDoAction, so they're swapped via SpriteAnimatorPatch.
// Walk/idle/turn while holding the boulder are left alone.
[HarmonyPatch(typeof(MachoThrow2))]
public class MachoThrowPatch
{
    const float DurationScale = 0.5f;
    static readonly string[] Anims = ["enter", "grab", "pickup", "beginThrow", "exit"];

    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    private static void SpeedUpAnims(MachoThrow2 __instance)
    {
        foreach (string name in Anims)
            AnimShortener.RegisterReplacement(__instance.animData, name, DurationScale);

        // The ground platform shrinks every tick during grab, so a shorter grab would take a smaller bite.
        // Raise the rate to compensate. Each instance is a fresh copy of the prefab, so this doesn't compound.
        __instance.scaleReductionOnGroundPlatform /= (Fix)DurationScale;
    }
}

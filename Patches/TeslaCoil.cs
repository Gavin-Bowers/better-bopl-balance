using System;
using System.Runtime.CompilerServices;
using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BetterBoplBalance.Patches;

// Keeps a tesla coil line in its flicker state (safe to cross) while the pair moves fast relative to each other.
// The line's StretchableLightning periodically flickers on its own: it plays "flicker", disables the line's hitbox and
// hides the inner sprite, then UnFlicker restores them when the animation ends. While the pair is fast, this patch
// starts that flicker and restarts it in place of UnFlicker, so the line stays safe until the pair slows down.
// The coils themselves stay lethal.
[HarmonyPatch]
public class TeslaLinePatch
{
    // Relative speed (units per second) above which the line flickers
    static readonly Fix MaxRelativeSpeed = (Fix)15L;
    // Smoothing for the measured speed (0-1). Lower ignores brief bumps better but reacts slower.
    static readonly Fix SpeedSmoothing = (Fix)0.25;
    // Logs the relative speed each time a line starts or stops being held in the flicker state, for tuning
    const bool LogToggles = true;

    class PairState
    {
        public SimpleSparkNode child;
        public Vec2 prevOffset;
        public bool hasPrev;
        public Fix smoothedSpeed;
        public bool isFast;
    }

    // Weak keys so destroyed coils and lines don't leak between rounds
    static readonly ConditionalWeakTable<SimpleSparkNode, PairState> states = new();
    static readonly ConditionalWeakTable<StretchableLightning, PairState> lineStates = new();

    [HarmonyPatch(typeof(SimpleSparkNode), "UpdateSim")]
    [HarmonyPrefix]
    private static void TrackRelativeSpeed(SimpleSparkNode __instance, Fix simDeltaTime)
    {
        if (!__instance.CanConnect()) return;
        SimpleSparkNode child = __instance.childNode;

        PairState s = states.GetOrCreateValue(__instance);
        if (s.child != child)
        {
            // New partner, so the previous offset isn't comparable
            s.child = child;
            s.hasPrev = false;
            s.smoothedSpeed = Fix.Zero;
            s.isFast = false;
        }

        Fix rawSpeed = Fix.Zero;
        Vec2 offset = child.hitbox.position - __instance.hitbox.position;
        // Coils on the same platform can't move relative to each other. Measuring them anyway picks up the gap
        // rotating when the platform spins (e.g. after a bump), so they're treated as stationary.
        bool samePlatform = __instance.attachedGround != null && __instance.attachedGround == child.attachedGround;
        if (!samePlatform && s.hasPrev && simDeltaTime > Fix.Zero)
            rawSpeed = Vec2.Magnitude(offset - s.prevOffset) / simDeltaTime;
        s.prevOffset = offset;
        s.hasPrev = true;

        s.smoothedSpeed += (rawSpeed - s.smoothedSpeed) * SpeedSmoothing;
        bool wasFast = s.isFast;
        s.isFast = s.smoothedSpeed > MaxRelativeSpeed;
        if (LogToggles && s.isFast != wasFast)
            Debug.Log($"[BetterBoplBalance] Tesla line {(s.isFast ? "held flickering" : "released")}, relative speed {(float)s.smoothedSpeed:0.0}");

        // Link the line to this pair's state so the StretchableLightning patches can read it
        StretchableLightning line = GetLine(__instance);
        if (line == null) return;
        lineStates.Remove(line);
        lineStates.Add(line, s);

        if (s.isFast)
            StartFlicker(line);
    }

    // ActivateLightning turns the line on mid-tick; flicker it right away so a fast pair is never briefly lethal
    [HarmonyPatch(typeof(SimpleSparkNode), "ActivateLightning")]
    [HarmonyPostfix]
    private static void FlickerOnActivate(SimpleSparkNode __instance)
    {
        if (!states.TryGetValue(__instance, out PairState s) || !s.isFast) return;
        StretchableLightning line = GetLine(__instance);
        if (line != null)
            StartFlicker(line);
    }

    // End of the flicker animation. Restart it instead of restoring the line while the pair is still fast.
    [HarmonyPatch(typeof(StretchableLightning), "UnFlicker")]
    [HarmonyPrefix]
    private static bool HoldFlicker(StretchableLightning __instance)
    {
        if (!lineStates.TryGetValue(__instance, out PairState s) || !s.isFast) return true;
        __instance.spriteAnim.beginAnimThenDoAction(__instance.animData.GetAnimation("flicker"), new Action(__instance.UnFlicker));
        return false;
    }

    static StretchableLightning GetLine(SimpleSparkNode node)
    {
        DPhysicsBox box = node.lightningLine;
        if (box == null || box.IsDestroyed) return null;
        return box.GetComponent<StretchableLightning>();
    }

    // Same as StretchableLightning's own periodic flicker. Skipped if the line isn't on yet, is already flickering
    // (hitbox disabled), or wasn't fully set up by InitLightning.
    static void StartFlicker(StretchableLightning line)
    {
        if (!line.gameObject.activeInHierarchy || line.hitbox == null || !line.hitbox.enabled
            || line.childSpriteRen == null || line.spriteAnim == null) return;
        line.spriteAnim.beginAnimThenDoAction(line.animData.GetAnimation("flicker"), new Action(line.UnFlicker));
        line.hitbox.enabled = false;
        line.childSpriteRen.enabled = false;
    }
}

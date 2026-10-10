using BoplFixedMath;
using HarmonyLib;

namespace BetterBoplBalance.Patches;

[HarmonyPatch(typeof(DetPhysics))]
public class PhysicsPatch
{
    // Beam width is beamWidth * player scale, so this keeps size scaling but shrinks relative to default
    static readonly Fix BeamWidthMultiplier = (Fix)0.25;

    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    private static void RemoveBeamPush(DetPhysics __instance)
    {
        __instance.beamPlatformPushForce = (Fix)500.0;
        __instance.beamBlackHolePushForce = (Fix)0.2;
        __instance.beamWidth *= BeamWidthMultiplier;
    }
}

[HarmonyPatch(typeof(Beam))]
public class BeamPatch
{
    static readonly Fix CooldownMultiplier = (Fix)1.5;
    static readonly Fix FastAimSpeed = (Fix)0.5;
    static readonly Fix SlowAimSpeed = (Fix)0.005;
    // Seconds the fast aim speed lasts, counted from when the beam can first turn
    // (grounded beams snap to the aim direction during their first frame, so the window starts after it)
    static readonly Fix FastAimDuration = (Fix)0.5;

    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    private static void IncreaseCooldown(Beam __instance)
    {
        // Runs on each spawned instance (copied from the prefab), so this doesn't compound
        __instance.ability.Cooldown *= CooldownMultiplier;
    }

    [HarmonyPatch("OnEnterAbility")]
    [HarmonyPostfix]
    private static void FlattenAimCurve(Beam __instance)
    {
        // Constant multiplier of 1, so angularAimSpeed is the actual turn speed
        __instance.angularAimSpeedAnim01.keys =
        [
            new() { time = Fix.Zero, value = Fix.One },
                    new() { time = Fix.One,  value = Fix.One },
                ];

        // timeSinceActivation is unused by the game, so it holds the time the fast aim window starts.
        // Grounded beams wait for OnLeftFirstFrame; air beams can turn immediately.
        __instance.timeSinceActivation = __instance.inAir ? Fix.Zero : Fix.MaxValue;
    }

    [HarmonyPatch("OnLeftFirstFrame")]
    [HarmonyPrefix]
    private static void StartFastAimWindow(Beam __instance)
    {
        __instance.timeSinceActivation = __instance.timeSinceEnter;
    }

    [HarmonyPatch("UpdateSim")]
    [HarmonyPrefix]
    private static void SetAimSpeedByState(Beam __instance)
    {
        // timeSinceEnter resets in OnEnterAbility and is incremented later in UpdateSim
        Fix fastAimStart = __instance.timeSinceActivation;
        bool fastAim = __instance.state == BeamState.enter
            && __instance.timeSinceEnter >= fastAimStart
            && __instance.timeSinceEnter < fastAimStart + FastAimDuration;
        __instance.angularAimSpeed = fastAim ? FastAimSpeed : SlowAimSpeed;
    }
}

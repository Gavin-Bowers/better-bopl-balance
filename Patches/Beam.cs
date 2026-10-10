using BoplFixedMath;
using HarmonyLib;

namespace BetterBoplBalance.Patches;

[HarmonyPatch(typeof(DetPhysics))]
public class PhysicsPatch
{
    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    private static void RemoveBeamPush(DetPhysics __instance)
    {
        __instance.beamPlatformPushForce = (Fix)0.0;
        __instance.beamBlackHolePushForce = (Fix)0.0;
    }
}

[HarmonyPatch(typeof(Beam))]
public class BeamPatch
{
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
    }

    [HarmonyPatch("UpdateSim")]
    [HarmonyPrefix]
    private static void SetAimSpeedByState(Beam __instance)
    {
        __instance.angularAimSpeed = __instance.state == BeamState.enter
            ? (Fix)0.5   // speed while charging
            : (Fix)0.005;  // speed while firing
    }
}

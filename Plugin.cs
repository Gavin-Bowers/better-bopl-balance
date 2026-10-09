using System;
using System.Collections.Generic;

using BepInEx;
using BoplFixedMath;
using HarmonyLib;

namespace BetterBoplBalance
{
    [BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Harmony harmony = new(ModInfo.PLUGIN_GUID);
            harmony.PatchAll();
        }

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

        // Shortens the animations for placing tesla coil and engine
        [HarmonyPatch(typeof(PlantObjectOnPlatform))]
        public class PlantObjectPatch
        {
            const float DurationScale = 0.5f;
            static readonly Dictionary<SpriteFrame[], SpriteFrame[]> cache = [];
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
    }
}
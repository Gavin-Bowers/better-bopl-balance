using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BetterBoplBalance.Patches;

// Tesla coil weight: works like SpikeMassPatch. A coil's BoplBody mass is merged into the platform it's attached to
// (SimpleSparkNode.Initialize -> hitbox.ManualInit -> AddCircleToRR), so this sets the mass before that happens.
// [HarmonyPatch]
// public class TeslaCoilMassPatch
// {
//     const float MassMultiplier = 10f;

//     // Read from the prefab (never modified), so duplicated coils don't compound the increase
//     static bool baseCaptured;
//     static Fix baseMass;
//     static Fix baseMomentOfInertia;

//     [HarmonyPatch(typeof(PlaceSparkNode), "Awake")]
//     [HarmonyPostfix]
//     private static void CaptureBaseMass(PlaceSparkNode __instance)
//     {
//         if (baseCaptured || __instance.plantablePrefab == null) return;
//         BoplBody prefabBody = __instance.plantablePrefab.GetComponent<BoplBody>();
//         if (prefabBody == null)
//         {
//             Debug.LogWarning("[BetterBoplBalance] Tesla coil prefab has no BoplBody, mass patch disabled");
//             return;
//         }
//         baseMass = prefabBody.mass;
//         baseMomentOfInertia = prefabBody.momentOfInertia;
//         baseCaptured = true;
//     }

//     // Covers coils from PlaceSparkNode.Place and ShootDuplicator copies. skipInit = true (Macho re-attaching a thrown
//     // boulder) reuses the existing physics body, so the mass set at creation carries over.
//     [HarmonyPatch(typeof(SimpleSparkNode), "Initialize")]
//     [HarmonyPrefix]
//     private static void SetCoilMass(SimpleSparkNode __instance, bool skipInit)
//     {
//         if (!baseCaptured || skipInit) return;
//         BoplBody body = __instance.GetComponent<BoplBody>();
//         if (body == null || baseMass == Fix.Zero) return; // zero mass means immovable; leave it alone

//         Fix factor = (Fix)MassMultiplier;
//         body.mass = baseMass * factor;
//         // Scale inertia the same way, matching how BoplBody rescales both together
//         if (baseMomentOfInertia != Fix.Zero)
//             body.momentOfInertia = baseMomentOfInertia * factor;
//     }
// }

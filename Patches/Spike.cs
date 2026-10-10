using BoplFixedMath;
using HarmonyLib;

namespace BetterBoplBalance.Patches;

// Spike weight: a spike's BoplBody mass is merged into the platform it's attached to (SpikeAttack.Initialize -> AddBoxToRR).
// In the base game this mass is the same for every spike regardless of player size, because the scale is applied
// after the merge and BoplBody skips mass rescaling for attached (composite) bodies.
// This sets mass = prefab mass * multiplier * scale before the merge, so spikes are lighter but scale with size.
[HarmonyPatch]
public class SpikeMassPatch
{
    const float MassMultiplier = 0.2f;

    // Read from the prefab (never modified), so duplicated spikes don't compound the reduction
    static bool baseCaptured;
    static Fix baseMass;
    static Fix baseMomentOfInertia;

    [HarmonyPatch(typeof(Spike), "Awake")]
    [HarmonyPostfix]
    private static void CaptureBaseMass(Spike __instance)
    {
        if (baseCaptured || __instance.spikePrefab == null) return;
        BoplBody prefabBody = __instance.spikePrefab.GetComponent<BoplBody>();
        if (prefabBody == null) return;
        baseMass = prefabBody.mass;
        baseMomentOfInertia = prefabBody.momentOfInertia;
        baseCaptured = true;
    }

    // Covers spikes from Spike.CastSpike and ShootDuplicator copies. skipInit = true (Macho re-attaching a thrown boulder)
    // reuses the existing physics body, so the mass set at creation carries over.
    [HarmonyPatch(typeof(SpikeAttack), "Initialize")]
    [HarmonyPrefix]
    private static void SetSpikeMass(SpikeAttack __instance, bool skipInit)
    {
        if (!baseCaptured || skipInit) return;
        BoplBody body = __instance.GetComponent<BoplBody>();
        if (body == null || baseMass == Fix.Zero) return; // zero mass means immovable; leave it alone

        Fix factor = (Fix)MassMultiplier;
        body.mass = baseMass * factor;
        // Scale inertia the same way, matching how BoplBody rescales both together
        if (baseMomentOfInertia != Fix.Zero)
            body.momentOfInertia = baseMomentOfInertia * factor;
    }
}

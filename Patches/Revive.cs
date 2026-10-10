using HarmonyLib;
using BoplFixedMath;
using UnityEngine;

namespace BetterBoplBalance.Patches;

[HarmonyPatch(typeof(PlayerCollision))]
public class RevivePatch
{
    [HarmonyPatch("SpawnClone")]
    [HarmonyPostfix]
    private static void ReplaceReviveOnRevivedClone(SlimeController __result, SlimeController slimeContToRevive)
    {
        if (__result == null) return; // clone cap reached, nothing spawned

        // An exception here would abort PlayerCollision.killPlayer mid-revive and softlock the round
        try
        {
            // Clone abilities are built from the dying body's abilities in the same order
            int count = Mathf.Min(__result.abilities.Count, slimeContToRevive.abilities.Count);
            for (int i = 0; i < count; i++)
            {
                AbilityMonoBehaviour old = __result.abilities[i];
                if (old == null || old.GetComponent<Revive>() == null) continue;

                // Only replace revives that were cast this life (they have a placed indicator)
                AbilityMonoBehaviour used = slimeContToRevive.abilities[i];
                Revive usedRevive = used != null ? used.GetComponent<Revive>() : null;
                if (usedRevive == null || usedRevive.reviveIndicator == null) continue;

                Sprite reviveSprite = __result.AbilityReadyIndicators[i].GetPrimarySprite();
                NamedSprite pick = RandomAbility.GetRandomAbilityPrefab(
                    slimeContToRevive.abilityIconsFull, slimeContToRevive.abilityIconsDemo, reviveSprite);

                Updater.DestroyFix(old.gameObject);
                AbilityMonoBehaviour newAbility = FixTransform.InstantiateFixed(pick.associatedGameObject, Vec2.zero)
                    .GetComponent<AbilityMonoBehaviour>();
                newAbility.gameObject.SetActive(false);

                __result.abilities[i] = newAbility;
                __result.AbilityReadyIndicators[i].SetSprite(pick.sprite, true);
                __result.AbilityReadyIndicators[i].ResetAnimation();
                // Don't touch abilityCooldownTimers: the clone's Init() hasn't run yet (array is null)
                // and will set every slot to -1.5 on the next tick, same as the other abilities
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BetterBoplBalance] Revive replacement failed: {e}");
        }
    }
}
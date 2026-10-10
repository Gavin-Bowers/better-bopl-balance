using HarmonyLib;
using BoplFixedMath;
using UnityEngine;

namespace BetterBoplBalance.Patches;

[HarmonyPatch(typeof(QuickSpell))]
public class RevivePatch
{
    [HarmonyPatch("Cast")]
    [HarmonyPostfix]
    private static void ReplaceReviveAfterUse(QuickSpell __instance)
    {
        if (__instance.GetComponent<Revive>() == null) return; // other QuickSpells

        SlimeController sc = __instance.playerInfo.slimeController;
        int slot = __instance.playerInfo.AbilityButtonUsedIndex012;
        if (sc == null || slot < 0 || slot >= sc.abilities.Count) return;

        Sprite reviveSprite = sc.AbilityReadyIndicators[slot].GetPrimarySprite();
        NamedSprite pick = RandomAbility.GetRandomAbilityPrefab(sc.abilityIconsFull, sc.abilityIconsDemo, reviveSprite);

        AbilityMonoBehaviour newAbility = FixTransform.InstantiateFixed(pick.associatedGameObject, Vec2.zero)
            .GetComponent<AbilityMonoBehaviour>();
        newAbility.gameObject.SetActive(false); // abilities stay inactive until used, same as SpawnClone

        sc.abilities[slot] = newAbility;
        sc.AbilityReadyIndicators[slot].SetSprite(pick.sprite, true);
        sc.AbilityReadyIndicators[slot].ResetAnimation();
        sc.abilityCooldownTimers[slot] = Fix.Zero; // starts on full cooldown; (Fix)100000L = ready immediately
    }
}
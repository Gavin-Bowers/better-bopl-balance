using BepInEx;
using HarmonyLib;

namespace BetterBoplBalance;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        Harmony harmony = new(ModInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }
}

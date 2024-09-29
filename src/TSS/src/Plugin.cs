using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace TwinStickArmSprint
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    [BepInProcess("h3vr.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> HeadArmswinger;

        public void Start()
        {
            LoadConfigFile();

            Harmony.CreateAndPatchAll(typeof(Patch));
        }

        private void LoadConfigFile()
        {
            HeadArmswinger = Config.Bind("General",
                                    "Head-Based Armswinger",
                                    false,
                                    "If true, act like regular Armswinger (no TwinStick), except use head direction instead of controller direction.");
        }
    }
}
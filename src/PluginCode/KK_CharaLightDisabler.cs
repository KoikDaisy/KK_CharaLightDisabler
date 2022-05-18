using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Diagnostics;

//plugin made with the help of IllusionMods/PluginTemplate https://github.com/IllusionMods/PluginTemplate and helpful people in the Koikatsu fan discord https://universalhentai.com/
namespace KK_CharaLightDisabler
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess("KoikatuVR")]
    [BepInProcess("Koikatu")]
    [BepInProcess("CharaStudio")]
    [BepInProcess("Koikatsu Party")]
    [BepInProcess("Koikatsu Party VR")]
    public class KK_CharaLightDisabler : BaseUnityPlugin
    {
        public const string PluginName = "KK_CharaLightDisabler";

        public const string GUID = "koikdaisy.kkcharalightdisabler";

        public const string Version = "1.0.1";

        internal static new ManualLogSource Logger;

        private static ConfigEntry<bool> _KK_CharaLightDisabler_Enabled;

        private void Awake()
        {
            Logger = base.Logger;

            _KK_CharaLightDisabler_Enabled = Config.Bind("Enable Plugin", "Enabled", true, "If enabled, will remove character light from any HScene, VR HScene, or studio map containing any object called " + PluginName + ". Useful for maps which provide their own lighting.");

            if (_KK_CharaLightDisabler_Enabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            }
        }

        private static class Hooks
        {
            //STUDIO MAP SWITCH HOOK
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.Map), "LoadMapCoroutine")]
            private static void StudioDisableCharaLight(Studio.Map __instance)
            {
                int mapID = __instance.no;
                __instance.StartCoroutine(Controller.WaitForNull(Controller.WaitForStudioLoaded(__instance, mapID), __instance));
            }

            //HSCENE MAIN GAME HOOK
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HScene), "Awake")]
            private static void MainGameDisableCharaLight(HScene __instance)
            {
                __instance.StartCoroutine(Controller.WaitForNull(Controller.WaitForHSceneLoaded(), __instance));
            }

            //HSCENE VR GAME HOOK
            [HarmonyPostfix]
            [HarmonyPatch(typeof(BaseLoader), "Awake")]
            private static void VRDisableCharaLight(BaseLoader __instance)
            {
                if (Process.GetCurrentProcess().ProcessName.Contains("VR") && 
                    GameObject.Find("AssetBundleManager") != null && 
                    GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().LoadSceneName == "VRHScene" && 
                    GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().PrevLoadSceneName == "VRCharaSelect")
                {
                    __instance.StartCoroutine(Controller.WaitForNull(Controller.WaitForVRLoaded(), __instance));
                }
            }
            

        }
        private static class Controller
        {
            private static readonly string hSceneLightName = "Directional Light";
            private static readonly string studioLightName = "Directional Chara";
            private static readonly string VRLightName = "Directional light";
            private static readonly string[] VRLightParentNames = new string[] { "koikdaisy.kkcamlocklightvr", "Camera (eye)" };

            //WAITS UNTIL ASSETBUNDLEMANAGER IS OCCUPYING A NULL SCENE, AFTER WHICH IT STARTS ANOTHER SPECIFIED COROUTINE
            public static IEnumerator WaitForNull(IEnumerator iEnumToStart, MonoBehaviour __instance)
            {
                yield return new WaitUntil(() => { return GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().baseScene == null; });
                __instance.StartCoroutine(iEnumToStart);

            }

            //STUDIO MAP SWITCHING COROUTINE
            public static IEnumerator WaitForStudioLoaded(Studio.Map __instance, int mapID)
            {
                yield return new WaitUntil(() => { return
                    //execute when loading into new studio map, OR
                    ((GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().baseScene != null) ||
                    //execute when unloading to none.
                    (GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().baseScene == null && (GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().AddSceneName.Length == 0))); });
                Logger.Log(LogLevel.Debug, PluginName + ": Loaded studio map, disable chara light: " + (GameObject.Find(PluginName) != null));
                SetCharaLightEnabled((GameObject.Find(PluginName) == null), studioLightName);
            }

            //HSCENE MAIN GAME COROUTINE
            public static IEnumerator WaitForHSceneLoaded()
            {
                yield return new WaitUntil(() => { return (GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().baseScene != null); });
                Logger.Log(LogLevel.Debug, PluginName + ": Loaded HScene, disable chara light: " + (GameObject.Find(PluginName) != null));
                SetCharaLightEnabled((GameObject.Find(PluginName) == null), hSceneLightName);
            }

            //HSCENE VR GAME COROUTINE
            public static IEnumerator WaitForVRLoaded()
            {
                yield return new WaitUntil(() => { return (GameObject.Find("AssetBundleManager").GetComponent<Manager.Scene>().baseScene != null); });
                Logger.Log(LogLevel.Debug, PluginName + ": Loaded VRHScene, disable chara light: " + (GameObject.Find(PluginName) != null));
                SetCharaLightEnabledVR((GameObject.Find(PluginName) == null), VRLightName, VRLightParentNames);
            }

            //STUDIO + MAIN GAME EXECUTOR
            private static void SetCharaLightEnabled(bool enabled, string lightName)
            {
                if (_KK_CharaLightDisabler_Enabled.Value)
                {
                    Light[] lights = FindObjectsOfType<Light>();

                    foreach (Light light in lights)
                    {
                        if (light.gameObject.name == lightName)
                        {
                            light.enabled = enabled;
                            break;
                        }
                    }
                }
            }

            //VR GAME EXECUTOR
            private static void SetCharaLightEnabledVR(bool enabled, string lightName, string[] lightParentNames)
            {
                if (_KK_CharaLightDisabler_Enabled.Value)
                {
                    Light[] lights = FindObjectsOfType<Light>();

                    foreach (Light light in lights)
                    {
                        foreach (string n in lightParentNames)
                        {
                            if (light.gameObject.name == lightName && light.gameObject.transform.parent.name == n)
                            {
                                light.enabled = enabled;
                                break;
                            }

                        }
                    }
                }
            }
        }
    }
}

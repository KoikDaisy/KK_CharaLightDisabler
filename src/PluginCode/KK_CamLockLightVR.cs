using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

//plugin made with the help of IllusionMods/PluginTemplate https://github.com/IllusionMods/PluginTemplate and helpful people in the Koikatsu fan discord https://universalhentai.com/
//studied Mantas' HLightControl to implement functionality https://github.com/Mantas-2155X/HLightControl
namespace KK_CamLockLightVR
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess("KoikatuVR")]
    [BepInProcess("Koikatsu Party VR")]
    public class KK_CamLockLightVR : BaseUnityPlugin
    {
        public const string PluginName = "KK_CamLockLightVR";

        public const string GUID = "koikdaisy.kkcamlocklightvr";

        public const string Version = "1.0.0";

        internal static new ManualLogSource Logger;

        private static ConfigEntry<bool> _KKCamLockLightVR_Enabled;
        private static ConfigEntry<Vector3> _KKCamLockLightVR_Rotation;

        private void Awake()
        {
            Logger = base.Logger;

            _KKCamLockLightVR_Enabled = Config.Bind("Enable Plugin", "Enabled", true, "If enabled, will lock the camera light in one position so head movements don't jitter the shadows. Greatly improves experience on maps with casted shadows.");
            _KKCamLockLightVR_Rotation = Config.Bind("Light Default Rotation", "Enabled", new Vector3(50f, 245f, 180f), "The default rotation of the locked camera light");
            
            Harmony.CreateAndPatchAll(typeof(HookIntoVRHScene), GUID);
        }

        private static class HookIntoVRHScene
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSprite), "SetLightData")]
            private static void ReplaceLightParent(HSprite __instance)
            {
                if (_KKCamLockLightVR_Enabled.Value)
                {
                    Light[] lights = FindObjectsOfType<Light>();

                    GameObject newParent = new GameObject(GUID);
                    

                    foreach (Light light in lights)
                    {
                        //When using SteamVR, the camera directional light is nested in "VRTK/[VRTK_SDKManager]/SDKSetups/SteamVR/VRCameraBase/[CameraRig]/Camera (eye)"
                        //(I found that out using CheatTools inside an HScene, just press F12)
                        //So all I had to do was look for a parent with the name "Camera (eye)"
                        if (light.transform.parent.name == "Camera (eye)")
                        {
                            //and then swap its parent with a newly-created game object
                            light.transform.SetParent(newParent.transform);
                            //and then we apply the default rotation
                            light.transform.SetPositionAndRotation(new Vector3(0f, 0f, 0f), Quaternion.Euler(_KKCamLockLightVR_Rotation.Value));
                        }
                    }
                }
            }
        }
    }
}

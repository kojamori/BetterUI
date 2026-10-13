using BetterUI.UI;
using HarmonyLib;
using JetBrains.Annotations;
using SFS;

namespace BetterUI.Patches;

// The purpose of this patch is to hook into the scene loading process and display a confirmation dialog before loading a new scene.
// Used by the Better Mod Menu mainly before the user goes from home scene to the build or world scene.

[HarmonyPatch(typeof(SceneLoader), "LoadScene")]
public class SceneLoading
{
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(SceneLoader), "LoadScene")]
    public static void OriginalLoadScene(SceneLoader instance, string sceneName, SceneLoader.Settings sceneSettings) {}

    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public static bool Prefix(string sceneName, SceneLoader.Settings sceneSettings, SceneLoader __instance)
    {
        return LoadSceneConfirmation.BeforeLoadScene(sceneName, sceneSettings, __instance);
    }
}
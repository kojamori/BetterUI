using System;
using System.Linq;
using System.Text;
using BetterModMenu.UI;
using HarmonyLib;
using JetBrains.Annotations;
using SFS;
using SFS.Input;
using SFS.UI;
using UnityEngine;

namespace BetterModMenu.Patches;

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
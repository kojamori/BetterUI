// Portions of this file include code contributed by Luna Tomato
// Copyright (c) 2026 Luna Tomato
// Licensed under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using MenuWallpaper;

namespace BetterUI.Patches;

[HarmonyPatch(typeof(SFS.UI.HomeManager), "Start")]
public static class HomeManagerStartPatch
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    static void Postfix(SFS.UI.HomeManager __instance)
    {
        WallpaperService.OnHomeStart(__instance);
    }
}


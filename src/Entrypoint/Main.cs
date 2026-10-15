using System;
using System.Collections.Generic;
using BetterUI.Patches;
using BetterUI.UI;
using HarmonyLib;
using JetBrains.Annotations;
using MenuWallpaper;
using ModLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetterUI.Entrypoint;

[UsedImplicitly]
public class Main : Mod
{
    [UsedImplicitly]
    public static Main Instance { get; private set; }

    public Main() => Instance = this;

    public override string ModNameID => "BetterUI";
    public override string DisplayName => "Better UI";
    public override string Author => "kojamori & Luna Tomato";
    public override string MinimumGameVersionNecessary => "1.6.00.16";
    public override string ModVersion => "1.2.1";
    public override string Description => "A mod that improves the in game user interface (UI).";
    public override Dictionary<string, string> Dependencies => new()
    {
        { "UITools", "v1.1.5" }
    };
    public override string IconLink => null;
    public override Action LoadKeybindings => null;

    public override void Early_Load()
    {
        new Harmony(ModNameID).PatchAll();
        ModLoadNotification.EarlyLoadInitialise();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try
        {
            if (SFS.UI.HomeManager.main != null)
                WallpaperService.OnHomeStart(SFS.UI.HomeManager.main);
            else
                WallpaperService.EnsureRunner();
        }
        catch { }
    }

    public override void Load()
    {
        Settings.Settings.Init();

        WallpaperConfig.Load(ModFolder);
        WallpaperState.Load();
        try { WallpaperSettingsMenu.Register(); } catch { }

        try
        {
            if (SFS.UI.HomeManager.main != null)
                WallpaperService.OnHomeStart(SFS.UI.HomeManager.main);
            else
                WallpaperService.EnsureRunner();

            WallpaperService.ApplySavedToExistingView();

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BetterUI] MenuWallpaper setup failed: " + e.Message);
            try { WallpaperService.EnsureRunner(); } catch { }
        }
    }
}

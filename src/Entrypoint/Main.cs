using System;
using System.Collections.Generic;
using System.Reflection;
using BetterUI.Patches;
using BetterUI.UI;
using HarmonyLib;
using JetBrains.Annotations;
using MenuWallpaper;
using ModLoader;
using ModLoader.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetterUI.Entrypoint;

[UsedImplicitly]
public class Main : Mod
{
    [UsedImplicitly]
    public static Main Instance { get; private set; }

    public Main() => Instance = this;

    public static DefaultFolder Folder => new (Instance.ModFolder);

    public override string ModNameID => "BetterUI";
    public override string DisplayName => "Better UI";
    public override string Author => "kojamori & Luna Tomato";
    public override string MinimumGameVersionNecessary => "1.6.00.16";
    public override string ModVersion => "1.1.1";
    public override string Description => "A mod that improves the in game user interface (UI).";
    public override Dictionary<string, string> Dependencies => new();
    public override string IconLink => null;
    public override Action LoadKeybindings => null;

    public override void Early_Load()
    {
        new Harmony(ModNameID).PatchAll();
        ModLoadNotification.EarlyLoadInitialise();
    }
    
    public override void Load()
    {
        Settings.Settings.Init();
        
        
        #region Menu Wallpaper Loading
        WallpaperConfig.Load(ModFolder);
        WallpaperState.Load();

        try
        {
            if (SFS.UI.HomeManager.main != null)
                WallpaperService.OnHomeStart(SFS.UI.HomeManager.main);
            WallpaperService.EnsureRunner();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BetterUI] MenuWallpaper setup failed: " + e.Message);
            try
            {
                WallpaperService.EnsureRunner();
            }
            catch (Exception e2)
            {
                Debug.LogWarning("[BetterUI] MenuWallpaper runner setup failed: " + e2.Message);
            }
        }

        Debug.Log("[BetterUI] " + ModVersion + " loaded");
        #endregion
    }
}


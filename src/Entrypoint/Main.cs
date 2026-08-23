using System;
using System.Collections.Generic;
using BetterModMenu.UI;
using HarmonyLib;
using JetBrains.Annotations;
using ModLoader;

namespace BetterModMenu.Entrypoint;

[UsedImplicitly]
public class Main : Mod
{
    [UsedImplicitly]
    public static Main Instance { get; private set; }

    public Main() => Instance = this;

    public static DefaultFolder Folder => new (Instance.ModFolder);

    public override string ModNameID => "BetterModMenu";
    public override string DisplayName => "Better Mod Menu";
    public override string Author => "kojamori";
    public override string MinimumGameVersionNecessary => "1.6.00.16";
    public override string ModVersion => "1.0.0";
    public override string Description => "A mod that improves the in-game mod menu.";
    public override Dictionary<string, string> Dependencies => new();
    public override string IconLink => null;
    public override Action LoadKeybindings => null;

    public override void Early_Load()
    {
        new Harmony(ModNameID).PatchAll();
        ModsLoadedUI.EarlyLoadInitialise();
    }
    
    public override void Load()
    {
        Settings.Settings.Init();
    }
}
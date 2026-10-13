using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using ModLoader.UI;
using SFS.Parts;
using SFS.UI;
using SFS.Variables;
using static BetterUI.Utils.ModsListHelper;

namespace BetterUI.UI;

public static class ModLoadNotification
{
    #region State and Stopwatch

    private static readonly Stopwatch Stopwatch = new();
    private static readonly Bool_Reference CodeModsLoaded = new();
    private static readonly Bool_Reference AssetModsLoaded = new();
    public static readonly Bool_Reference AllModsLoaded = new();

    #endregion
    public static void EarlyLoadInitialise()
    {
        Stopwatch.Start();
        UnityEngine.Debug.Log("Waiting for custom assets to load...");

        CustomAssetsReady.OnLoadDone += () =>
        {
            Stopwatch.Stop();
            AssetModsLoaded.Value = true;
        };

        _ = CustomAssetsReady.WaitForCustomAssetsAsync();

        void CheckIfLoaded()
        {
            if (CodeModsLoaded.Value && AssetModsLoaded.Value)
            {
                AllModsLoaded.Value = true;
            }
        }

        CodeModsLoaded.OnChange += CheckIfLoaded;
        AssetModsLoaded.OnChange += CheckIfLoaded;
        AllModsLoaded.OnChange += OnAllModsLoaded;
    }

    private static void OnAllModsLoaded()
    {
        if (!AllModsLoaded.Value) return;
        
        var codeMods = GetModsByStatus(type: ModsListElement.ModType.Mod, FilterState.Active).Count;
        var assetMods = GetModsByStatus(type: ModsListElement.ModType.AssetsPack, FilterState.Active)
            .Count;
        var textureMods =
            GetModsByStatus(type: ModsListElement.ModType.TexturesPack, FilterState.Active).Count;
        var planetMods =
            GetModsByStatus(type: ModsListElement.ModType.SolarSystem, FilterState.Active).Count;
        Menu.read.ShowReport(
            $"Loaded: {codeMods} code mod(s), {assetMods} asset pack(s), {textureMods} texture pack(s), and {planetMods} custom solar system(s) in {Stopwatch.ElapsedMilliseconds} ms",
            null);
    }

    #region Listening for Mod Loading to Finish 

    private static class CustomAssetsReady
    {
        internal static async UniTaskVoid WaitForCustomAssetsAsync()
        {
            while (!CustomAssetsLoader.finishedLoading)
                await UniTask.Yield();

            OnLoadDone?.Invoke();
        }

        public static event Action OnLoadDone;
    }

    [HarmonyPatch(typeof(ModLoader.ModLoader), "Start")]
    private static class ModsLoaderPatch
    {
        [UsedImplicitly]
        private static void Postfix()
        {
            CodeModsLoaded.Value = true;
        }        
    }

    #endregion
    
}



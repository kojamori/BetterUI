using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using ModLoader.UI;
using SFS.Parts;
using SFS.UI;
using SFS.Variables;
using static BetterModMenu.UI.ModsListHelper;

namespace BetterModMenu.UI;

public static class ModsLoadedUI
{
    #region State and Stopwatch

    private static Stopwatch _stopwatch = new Stopwatch();
    private static Bool_Reference CodeModsLoaded = new();
    private static Bool_Reference AssetModsLoaded = new();

    #endregion
    public static void EarlyLoadInitialise()
    {
        _stopwatch.Start();
        UnityEngine.Debug.Log("Waiting for custom assets to load...");

        CustomAssetsReady.OnLoadDone += () =>
        {
            _stopwatch.Stop();
            AssetModsLoaded.Value = true;
        };

        _ = CustomAssetsReady.WaitForCustomAssetsAsync();

        CodeModsLoaded.OnChange += CheckIfLoaded;
        AssetModsLoaded.OnChange += CheckIfLoaded;
    }

    private static void CheckIfLoaded()
    {
        if (CodeModsLoaded.Value && AssetModsLoaded.Value)
        {
            int codeMods = GetModsByStatus(type: ModsListElement.ModType.Mod, ModsListHelper.FilterState.Active).Count;
            int assetMods = GetModsByStatus(type: ModsListElement.ModType.AssetsPack, ModsListHelper.FilterState.Active)
                .Count;
            int textureMods =
                GetModsByStatus(type: ModsListElement.ModType.TexturesPack, ModsListHelper.FilterState.Active).Count;
            int planetMods =
                GetModsByStatus(type: ModsListElement.ModType.SolarSystem, ModsListHelper.FilterState.Active).Count;
            Menu.read.ShowReport(
                $"Loaded: {codeMods} code mod(s), {assetMods} asset pack(s), {textureMods} texture pack(s), and {planetMods} custom solar system(s) in {_stopwatch.ElapsedMilliseconds} ms",
                null);
        }
    }

    #region Listening for Mod Loading to Finish 

    private static class CustomAssetsReady
    {
        internal static async UniTaskVoid WaitForCustomAssetsAsync()
        {
            while (!CustomAssetsLoader.finishedLoading)
                await UniTask.Yield();

            OnLoadDone?.Invoke();
            IsLoaded = true;
        }

        public static event Action OnLoadDone;
        public static bool IsLoaded { get; private set; }
    }

    [HarmonyPatch(typeof(ModLoader.ModLoader), "Start")]
    private static class ModsLoaderPatch
    {
        private static void Postfix()
        {
            CodeModsLoaded.Value = true;
        }        
    }

    #endregion
    
}



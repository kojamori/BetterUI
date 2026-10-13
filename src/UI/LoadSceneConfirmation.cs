using System;
using System.Linq;
using System.Text;
using SFS;
using SFS.Input;
using SFS.UI;
using UnityEngine;
using static BetterUI.Utils.ModsListHelper;

namespace BetterUI.UI;

public static class LoadSceneConfirmation
{
    // ReSharper disable once InconsistentNaming
    public static bool BeforeLoadScene(string sceneName, SceneLoader.Settings sceneSettings, SceneLoader __instance)
    {
        if (sceneName == "Home" || ModLoadNotification.AllModsLoaded.Value)
        {
            return true;
        }
        
        // Check for unloaded active content
        var unloadedAssets = GetModsByStatus(
            ModLoader.UI.ModsListElement.ModType.AssetsPack, 
            FilterState.Active, 
            LoadState.NotLoaded);

        var unloadedTextures = GetModsByStatus(
            ModLoader.UI.ModsListElement.ModType.TexturesPack, 
            FilterState.Active, 
            LoadState.NotLoaded);
        
        if (!unloadedAssets.Any() && !unloadedTextures.Any())
        {
            return true;
        }
        
        // Build Report
        var report = new StringBuilder();
        report.AppendLine("The following mods are still loading:");
        report.AppendLine();

        if (unloadedAssets.Any())
        {
            report.AppendLine($"<b>Asset Packs ({unloadedAssets.Count}):</b>");
            foreach (var mod in unloadedAssets)
                report.AppendLine($"   • {mod.name}");
        }

        if (unloadedTextures.Any())
        {
            report.AppendLine($"$<b>Texture Packs ({unloadedTextures.Count}):</b>");
            foreach (var mod in unloadedTextures)
                report.AppendLine($"   • {mod.name}");
        }

        report.AppendLine("\nEntering now may result in missing parts, textures or other assets.");
        
        HandleConfirmationAsync(__instance, sceneName, sceneSettings, report.ToString());
        
        return false;
    }
    
    private static async void HandleConfirmationAsync(SceneLoader loader, string sceneName, SceneLoader.Settings settings, string message)
    {
        try 
        {
            var confirmed = MenuGenerator.OpenConfirmationAsync(
                closeMode: CloseMode.Current,
                text: () => message, 
                confirmText: () => "Enter Anyway",
                denyText: () => "Wait"
            );
            
            bool result = await confirmed;

            if (result)
            {
                Patches.SceneLoading.OriginalLoadScene(loader, sceneName, settings);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BetterUI] Error in confirmation handler: {ex}");
        }
    }
}
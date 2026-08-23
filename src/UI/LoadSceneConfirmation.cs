using System;
using System.Linq;
using System.Text;
using SFS;
using SFS.Input;
using SFS.UI;
using UnityEngine;

namespace BetterModMenu.UI;

public static class LoadSceneConfirmation
{
    // ReSharper disable once InconsistentNaming
    public static bool BeforeLoadScene(string sceneName, SceneLoader.Settings sceneSettings, SceneLoader __instance)
    {
        if (sceneName == "Home" || ModsLoadedUI.AllModsLoaded.Value)
        {
            return true;
        }
        
        // Check for unloaded active content
        var unloadedAssets = ModsListHelper.GetModsByStatus(
            ModLoader.UI.ModsListElement.ModType.AssetsPack, 
            ModsListHelper.FilterState.Active, 
            ModsListHelper.LoadState.NotLoaded);

        var unloadedTextures = ModsListHelper.GetModsByStatus(
            ModLoader.UI.ModsListElement.ModType.TexturesPack, 
            ModsListHelper.FilterState.Active, 
            ModsListHelper.LoadState.NotLoaded);
        
        if (!unloadedAssets.Any() && !unloadedTextures.Any())
        {
            return true;
        }
        
        // Build Report
        StringBuilder report = new StringBuilder();
        report.AppendLine("The following content packs are still loading:");
        report.AppendLine();

        if (unloadedAssets.Any())
        {
            report.AppendLine("<b>Asset Packs:</b>");
            foreach (var mod in unloadedAssets)
                report.AppendLine($"   • {mod.name}");
        }

        if (unloadedTextures.Any())
        {
            report.AppendLine("<b>Texture Packs:</b>");
            foreach (var mod in unloadedTextures)
                report.AppendLine($"   • {mod.name}");
        }

        report.AppendLine("\nEntering now may result in missing parts or textures.");
        
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
            Debug.LogError($"[BetterModMenu] Error in confirmation handler: {ex}");
        }
    }
}
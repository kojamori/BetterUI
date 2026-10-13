using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using ModLoader.UI;
using UnityEngine;
using static BetterUI.Settings.Settings;
using static BetterUI.UI.BetterModMenu;

namespace BetterUI.Patches;

// The purpose of this patch is to not display mods in the mod menu that are filtered out by the settings.

[HarmonyPatch(typeof(ModsMenu), "DrawUI")]
public static class ModMenuPatches
{
    [UsedImplicitly]
    public static bool Prefix()
    {
        var elements = Traverse.Create(typeof(ModsMenu))
            .Field<List<ModsListElement.ModData>>("elements").Value;
        
        var elementPool = Traverse.Create(ModsMenu.main)
            .Field<Pool<ModsListElement>>("elementPool").Value;

        if (elements == null || elementPool == null)
            return false;

        elementPool.Reset();

        foreach (var modData in elements)
        {
            // the main filtering logic, not much else is changed
            if (!Data.showTexturePacks && modData.type == ModsListElement.ModType.TexturesPack) continue;
            if (!Data.showCodeMods && modData.type == ModsListElement.ModType.Mod) continue;
            if (!Data.showPartPacks && modData.type == ModsListElement.ModType.AssetsPack) continue;
            if (!Data.showCustomSolarSystems && modData.type == ModsListElement.ModType.SolarSystem) continue;

            var item = elementPool.GetItem();
            item.gameObject.SetActive(true);
            item.DrawUI(modData);

            if (modData.icon != null)
            {
                item.iconHolder.gameObject.SetActive(true);
                item.iconImage.sprite = Sprite.Create(
                    modData.icon,
                    new Rect(0f, 0f, modData.icon.width, modData.icon.height),
                    new Vector2(0.5f, 0.5f));
            }
            else if (modData.loadingTexture?.IsCompleted == true && modData.loadingTexture.Result != null)
            {
                item.iconHolder.gameObject.SetActive(true);
                Texture2D result = modData.loadingTexture.Result;
                item.iconImage.sprite = Sprite.Create(
                    result,
                    new Rect(0f, 0f, result.width, result.height),
                    new Vector2(0.5f, 0.5f));
            }
            else
            {
                item.iconHolder.gameObject.SetActive(false);
            }
        }

        return false;
    }
}


// just load the UI when the menu is opened, so we don't have to wait for the mod list to be drawn
// fires only once

[HarmonyPatch(typeof(ModsMenu), "OnOpen")]
public static class ModsMenuOnOpenPatch
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(ModsMenu __instance)
    {
        InjectUI(__instance);
    }
}
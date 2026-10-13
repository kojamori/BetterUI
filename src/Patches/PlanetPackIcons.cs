using HarmonyLib;
using ModLoader;
using ModLoader.UI;
using SFS.Parsers.Json;
using SFS.Parts;
using SFS.WorldBase;
using System;
using BetterUI.Utils;
using JetBrains.Annotations;
using UnityEngine;

namespace BetterUI.Patches;

// The purpose of this patch is to add support for custom solar system icons in the Mods menu.
// The base game has no icon field, so we use CustomSolarSystemSettings.
// then, we read the icon path from Import_Settings.txt and load the icon from the solar system folder.

[HarmonyPatch(typeof(PlanetLoader), "GetSolarSystemsForModsMenu")]
public static class Patches {
    [UsedImplicitly]
    public static bool Prefix() {
        foreach (var solarSystem in PlanetLoader.GetAllSolarSystems())
        {
            var file = PlanetLoader
                .GetSolarSystemFolder(solarSystem)
                .GetFile("Import_Settings.txt");

            ModsSettings.main.settings.solarSystemsActive.TryAdd(solarSystem, true);

            var data = new ModsListElement.ModData
            {
                name = solarSystem,
                author = "",
                description = "",
                icon = null,
                type = ModsListElement.ModType.SolarSystem,
                version = "",
                saveName = solarSystem
            };

            CustomSolarSystemSettings solarSystemSettings = null;
            if (file.Exists() &&
                JsonWrapper.TryLoadJson(file, out solarSystemSettings) &&
                solarSystemSettings != null)
            {
                data.author = string.IsNullOrWhiteSpace(solarSystemSettings.authorName)
                    ? ""
                    : solarSystemSettings.authorName;

                data.description = string.IsNullOrWhiteSpace(solarSystemSettings.description)
                    ? ""
                    : solarSystemSettings.description;

                data.version = string.IsNullOrWhiteSpace(solarSystemSettings.version)
                    ? ""
                    : solarSystemSettings.version;

                
                // The new code for adding icons, nothing else is changed
                if (!string.IsNullOrEmpty(solarSystemSettings.icon))
                {
                    try
                    {
                        data.icon = TextureUtility.FromFile(
                            PlanetLoader.GetSolarSystemFolder(solarSystem)
                                .GetFile(solarSystemSettings.icon)
                        );
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"Failed to load icon for custom solar system \"{solarSystem}\": {e}"
                        );
                    }
                }


            }
            else
            {
                data.description = "Failed to load the solar system's Import_Settings.txt";
            }

            if (!DevSettings.FullVersion)
            {
                data.description = "Planet pack purchase is required to load custom solar systems";
            }

            ModsMenu.AddMod(data);

            CustomAssetsLoader.onUnload.Push(() =>
            {
                ModsMenu.RemoveMod(data);
            });
        }

        return false;
    }
}


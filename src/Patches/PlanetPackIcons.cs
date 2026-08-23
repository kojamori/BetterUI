using HarmonyLib;
using ModLoader;
using ModLoader.UI;
using SFS.Parsers.Json;
using SFS.Parts;
using SFS.WorldBase;
using System;
using JetBrains.Annotations;
using UnityEngine;

namespace BetterModMenu.Patches;

[UsedImplicitly]
public class CustomSolarSystemSettings : SolarSystemSettings 
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    // ReSharper disable once ConvertToConstant.Global
    public string icon = "";
}

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

            if (file.Exists() &&
                JsonWrapper.TryLoadJson(file, out CustomSolarSystemSettings solarSystemSettings))
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

                
                // The new code for adding icons.
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


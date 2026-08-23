using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ModLoader;
using ModLoader.UI;
using SFS.Parts;
using SFS.WorldBase;
using Object = UnityEngine.Object;

namespace BetterModMenu;

public static class ModsListHelper
{
    public enum FilterState
    {
        Active,
        Inactive,
        All
    }

    public enum LoadState
    {
        Loaded,
        NotLoaded,
        All
    }
    
    public static readonly Dictionary<string, ModsListElement.ModData> ModLoadStates = new Dictionary<string, ModsListElement.ModData>();

    public static List<ModsListElement.ModData> GetModsByStatus(
        ModsListElement.ModType type,
        FilterState state,
        LoadState loadState = LoadState.All,
        Func<string, bool> additionalCheck = null)
    {
        var allElements = GetElements();
        if (allElements == null) return new List<ModsListElement.ModData>();

        return allElements.Where(element =>
        {
            if (element.type != type) return false;
            if (additionalCheck != null && !additionalCheck(element.saveName)) return false;
            
            bool isActive = IsTypeActive(element.saveName, type);
            bool isLoaded = IsModLoaded(element.saveName, type);

            bool stateMatch = state switch
            {
                FilterState.Active => isActive,
                FilterState.Inactive => !isActive,
                FilterState.All => true,
                _ => false
            };

            bool loadStateMatch = loadState switch
            {
                LoadState.Loaded => isLoaded,
                LoadState.NotLoaded => !isLoaded,
                LoadState.All => true,
                _ => false
            };

            return stateMatch && loadStateMatch;
        }).ToList();
    }
    
    public static Loader ModLoader
    {
        get
        {
            if (field == null)
            {
                field = Object.FindFirstObjectByType<Loader>();
            }
            return field;
        }
    }

    private static List<ModsListElement.ModData> GetElements()
    {
        return Traverse.Create(typeof(ModsMenu)).Field("elements").GetValue<List<ModsListElement.ModData>>();
    }

    private static bool IsTypeActive(string saveName, ModsListElement.ModType type)
    {
        if (ModsSettings.main == null || ModsSettings.main.settings == null) 
            return true;

        var settings = ModsSettings.main.settings;
        switch (type)
        {
            case ModsListElement.ModType.Mod:
                return settings.modsActive.TryGetValue(saveName, out bool val) && val;
            case ModsListElement.ModType.AssetsPack:
                return settings.assetPacksActive.TryGetValue(saveName, out val) && val;
            case ModsListElement.ModType.TexturesPack:
                return settings.texturePacksActive.TryGetValue(saveName, out val) && val;
            case ModsListElement.ModType.SolarSystem:
                return settings.solarSystemsActive.TryGetValue(saveName, out val) && val;
            default:
                return false;
        }
    }
    
    private static bool IsModLoaded(string saveName, ModsListElement.ModType type)
    {
        switch (type)
        {
            case ModsListElement.ModType.Mod:
                if (ModLoader == null) return false;
                // Code mods are added to 'loadedMods' synchronously during Early_Load
                var loadedMods = Traverse.Create(ModLoader).Field("loadedMods").GetValue<List<Mod>>();
                return loadedMods != null && loadedMods.Any(m => m.ModNameID == saveName);

            case ModsListElement.ModType.SolarSystem:
                // Solar systems are registered synchronously at the very start of LoadSequence
                return PlanetLoader.GetAllSolarSystems().Contains(saveName);

            case ModsListElement.ModType.AssetsPack:
            case ModsListElement.ModType.TexturesPack:
                if (ModLoadStates.TryGetValue(saveName, out var state))
                {
                    // If it's still "n/a", the async bundle read for this pack hasn't finished yet.
                    if (state.description == "n/a") return false;
                    
                    if (state.description != null && (state.description.StartsWith("ERROR:"))) 
                        return false;
                    
                    return true;
                }
                
                if (CustomAssetsLoader.finishedLoading)
                {
                    return IsTypeActive(saveName, type);
                }
                
                return false;

            default:
                return false;
        }
    }
    
    public static List<Mod> GetListOfCodeModsFromLoader(FilterState state)
    {
        if (ModLoader == null) return new List<Mod>();
        
        var allMods = Traverse.Create(ModLoader).Field("mods").GetValue<List<Mod>>() ?? new List<Mod>();

        return allMods.Where(mod =>
        {
            bool isActive = IsTypeActive(mod.ModNameID, ModsListElement.ModType.Mod);
            return state switch
            {
                FilterState.Active => isActive,
                FilterState.Inactive => !isActive,
                FilterState.All => true,
                _ => false
            };
        }).ToList();
    }
    
    public static List<ModsListElement.ModData> GetListOfPlanetPacksFromLoader(FilterState state)
    {
        var allElements = GetElements();
        if (allElements == null) return new List<ModsListElement.ModData>();

        return allElements.Where(element => element.type == ModsListElement.ModType.SolarSystem)
            .Where(element => 
            {
                bool isActive = IsTypeActive(element.saveName, ModsListElement.ModType.SolarSystem);
                return state switch
                {
                    FilterState.Active => isActive,
                    FilterState.Inactive => !isActive,
                    FilterState.All => true,
                    _ => false
                };
            })
            .ToList();
    }
    
    
    [HarmonyPatch(typeof(ModsMenu), "AddMod")]
    [HarmonyPostfix]
    public static void TrackModLoadState(ModsListElement.ModData data)
    {
        if (!string.IsNullOrEmpty(data.saveName))
        {
            // Overwrite with the latest state. The game calls this twice per pack:
            // 1st time: description = "n/a" (pending async load)
            // 2nd time: description = "Real Description" (async load finished)
            ModLoadStates[data.saveName] = data;
        }
    }
}
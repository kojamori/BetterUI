using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ModLoader;
using ModLoader.UI;
using SFS.Parts; // Required for CustomAssetsLoader
using SFS.WorldBase; // Required for PlanetLoader
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetterModMenu.UI;

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

    /// <summary>
    /// Tracks the latest known state of each mod/pack by its saveName.
    /// Populate this by ensuring your Harmony instance patches ModsMenu.AddMod (see patch below).
    /// </summary>
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

    /// <summary>
    /// Checks exactly how many mods of a specific type are loaded vs not yet loaded.
    /// Evaluates each mod individually, returning accurate counts instantly, even mid-loading.
    /// </summary>
    public static (int LoadedCount, int NotLoadedCount) GetModLoadCounts(
        ModsListElement.ModType type,
        Func<string, bool> additionalCheck = null)
    {
        var allElements = GetElements();
        if (allElements == null) return (0, 0);

        int loaded = 0;
        int notLoaded = 0;

        foreach (var element in allElements)
        {
            if (element.type != type) continue;
            if (additionalCheck != null && !additionalCheck(element.saveName)) continue;

            if (IsModLoaded(element.saveName, type))
                loaded++;
            else
                notLoaded++;
        }

        return (loaded, notLoaded);
    }

    private static GameObject modLoaderGameObject;
    private static Loader _loader;

    public static Loader ModLoader
    {
        get
        {
            if (modLoaderGameObject == null)
            {
                var scene = SceneManager.GetSceneByName("Base_PC");
                if (scene.isLoaded)
                {
                    modLoaderGameObject = scene.GetRootGameObjects()
                        .FirstOrDefault(g => g.name == "Mod Loader");
                        
                    if (modLoaderGameObject != null)
                    {
                        _loader = modLoaderGameObject.GetComponent<Loader>();
                    }
                }
            }
            return _loader;
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

    /// <summary>
    /// Evaluates the load state of a SPECIFIC mod/pack individually.
    /// </summary>
    private static bool IsModLoaded(string saveName, ModsListElement.ModType type)
    {
        switch (type)
        {
            case ModsListElement.ModType.Mod:
                if (ModLoader == null) return false;
                // Code mods are added to 'loadedMods' synchronously during Early_Load (Awake)
                var loadedMods = Traverse.Create(ModLoader).Field("loadedMods").GetValue<List<Mod>>();
                return loadedMods != null && loadedMods.Any(m => m.ModNameID == saveName);

            case ModsListElement.ModType.SolarSystem:
                // Solar systems are registered synchronously at the very start of LoadSequence
                return PlanetLoader.GetAllSolarSystems().Contains(saveName);

            case ModsListElement.ModType.AssetsPack:
            case ModsListElement.ModType.TexturesPack:
                // Check our tracked state first (requires the Harmony patch below)
                if (ModLoadStates.TryGetValue(saveName, out var state))
                {
                    // If it's still "n/a", the async bundle read for THIS SPECIFIC pack hasn't finished yet.
                    if (state.description == "n/a") return false;
                    
                    // If it explicitly failed, it's not loaded.
                    if (state.description != null && (state.description.StartsWith("ERROR") || state.description.Contains("Failed"))) 
                        return false;
                    
                    // Metadata is resolved and assets are registered. It is loaded.
                    return true;
                }

                // FALLBACK: If the Harmony patch hasn't been applied yet, we can only use the global flag.
                // This is less precise but prevents false positives.
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
    
    /// <summary>
    /// Planet/Solar System packs are represented as ModData elements, not Mod class instances.
    /// </summary>
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

    // =========================================================================
    // HARMONY PATCH: Required for 100% accurate per-pack load state tracking
    // =========================================================================
    // Call `yourHarmonyInstance.PatchAll(typeof(ModsListHelper));` in your mod's 
    // Awake/Start to activate this. It intercepts every time the game updates 
    // the mod menu, allowing us to know exactly when a specific pack finishes 
    // its async load (when its description changes from "n/a" to the real value).
    
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
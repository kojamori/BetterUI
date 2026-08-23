using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFS.Parsers.Json;
using UnityEngine;
using BetterModMenu.Entrypoint;

namespace BetterModMenu.Settings;

[Serializable]
public class SettingsData
{
    public bool showTexturePacks = true;
    public bool showCodeMods = true;
    public bool showPartPacks = true;
    public bool showCustomSolarSystems = true;
}


public static class Settings
{
    private const string SettingsFileName = "settings.json"; 
    public static readonly SettingsData Filters = new();

    private static IFile FilePath => Main.Folder.GetFile(SettingsFileName);

    public static void Init()
    {
        Load();
        Save();
        Application.quitting += Save;
    }

    public static void Save()
    {
        JsonWrapper.SaveAsJson(FilePath, Filters, true);
    }

    private static void Load()
    {
        if (!FilePath.Exists())
            return;

        string text = FilePath.ReadText();
        if (JToken.Parse(text) is JObject json)
        {
            JsonReader reader = json.CreateReader();
            JsonSerializer.Create().Populate(reader, Filters);
        }
        else
        {
            Debug.LogError($"[BetterModMenu] Settings.Load: Invalid JSON in \"{FilePath}\"");
        }
    }
}
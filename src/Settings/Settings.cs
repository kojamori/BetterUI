using System;
using System.IO;
using BetterUI.Entrypoint;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BetterUI.Settings;

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
    public static readonly SettingsData Data = new();

    private static string PathFile =>
        Path.Combine(Main.Instance != null ? Main.Instance.ModFolder : ".", SettingsFileName);

    public static void Init()
    {
        Load();
        Save();
        Application.quitting += Save;
    }

    public static void Save()
    {
        try
        {
            string dir = Path.GetDirectoryName(PathFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(PathFile, JsonConvert.SerializeObject(Data, Formatting.Indented));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BetterUI] Settings.Save failed: " + e.Message);
        }
    }

    private static void Load()
    {
        try
        {
            if (!File.Exists(PathFile)) return;
            var text = File.ReadAllText(PathFile);
            if (JToken.Parse(text) is JObject json)
            {
                var reader = json.CreateReader();
                JsonSerializer.Create().Populate(reader, Data);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BetterUI] Settings.Load failed: " + e.Message);
        }
    }
}

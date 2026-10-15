// This file is code contributed by Luna Tomato
// Copyright (c) 2026 Luna Tomato
// Licensed under the MIT License


using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MenuWallpaper
{
    public static class WallpaperConfig
    {
        public static string ModFolder;

        public static bool Cover = true;

        // fullscreen | fit | 16:9 | 9:16 | 1:1 | 4:3
        public static string FitMode = "fullscreen";

        public static bool ShowResetButton = true;

        public static bool VideoSound = false;
        public static float VideoVolume = 0.5f;

        public static bool CopyFiles = true;
        public static int MaxCopyMB = 2048;

        public static void Load(string modFolder)
        {
            ModFolder = modFolder;
            try
            {
                if (string.IsNullOrEmpty(modFolder)) return;
                string path = Path.Combine(modFolder, "config.txt");
                if (!File.Exists(path)) return;

                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                    string val = line.Substring(eq + 1).Trim();
                    switch (key)
                    {
                        case "cover": Cover = ParseBool(val, Cover); break;
                        case "fitmode": FitMode = NormalizeFitMode(val); break;
                        case "showresetbutton": ShowResetButton = ParseBool(val, ShowResetButton); break;
                        case "videosound": VideoSound = ParseBool(val, VideoSound); break;
                        case "videovolume": VideoVolume = Mathf.Clamp01(ParseFloat(val, VideoVolume)); break;
                        case "copyfiles": CopyFiles = ParseBool(val, CopyFiles); break;
                        case "maxcopymb": MaxCopyMB = Math.Max(1, ParseInt(val, MaxCopyMB)); break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MenuWallpaper] config.txt read failed, using defaults: " + e.Message);
            }
        }

        public static bool SaveSettingsKeys()
        {
            try
            {
                if (string.IsNullOrEmpty(ModFolder)) return false;
                string path = Path.Combine(ModFolder, "config.txt");
                var lines = new System.Collections.Generic.List<string>();
                var skip = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "cover", "videosound", "videovolume", "showresetbutton", "copyfiles", "maxcopymb"
                };
                if (File.Exists(path))
                {
                    foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                    {
                        string tr = raw.Trim();
                        if (tr.Length > 0 && !tr.StartsWith("#") && !tr.StartsWith("//"))
                        {
                            int eq = tr.IndexOf('=');
                            if (eq > 0)
                            {
                                string key = tr.Substring(0, eq).Trim().ToLowerInvariant();
                                if (skip.Contains(key)) continue;
                            }
                        }
                        lines.Add(raw);
                    }
                }
                while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                    lines.RemoveAt(lines.Count - 1);
                lines.Add("");
                lines.Add("Cover=" + (Cover ? "true" : "false"));
                lines.Add("FitMode=" + FitMode);
                lines.Add("ShowResetButton=" + (ShowResetButton ? "true" : "false"));
                lines.Add("VideoSound=" + (VideoSound ? "true" : "false"));
                lines.Add("VideoVolume=" + VideoVolume.ToString(CultureInfo.InvariantCulture));
                lines.Add("CopyFiles=" + (CopyFiles ? "true" : "false"));
                lines.Add("MaxCopyMB=" + MaxCopyMB);
                File.WriteAllLines(path, lines.ToArray(), new UTF8Encoding(false));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MenuWallpaper] SaveSettingsKeys failed: " + e.Message);
                return false;
            }
        }


        public static string NormalizeFitMode(string val)
        {
            if (string.IsNullOrEmpty(val)) return FitMode;
            string v = val.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "");
            switch (v)
            {
                case "fullscreen":
                case "full":
                case "cover":
                case "fill":
                    return "fullscreen";
                case "fit":
                case "contain":
                case "original":
                    return "fit";
                case "16:9":
                case "16x9":
                case "16-9":
                    return "16:9";
                case "9:16":
                case "9x16":
                case "9-16":
                    return "9:16";
                case "1:1":
                case "1x1":
                case "square":
                    return "1:1";
                case "4:3":
                case "4x3":
                case "4-3":
                    return "4:3";
                case "4:9":
                case "4x9":
                case "4-9":
                    return "4:9";
                default:
                    return FitMode;
            }
        }

        public static void SetFitMode(string mode)
        {
            FitMode = NormalizeFitMode(mode);
            Cover = FitMode == "fullscreen";
            SaveSettingsKeys();
        }

        public static float GetAspectRatio(float naturalW, float naturalH)
        {
            switch (FitMode)
            {
                case "16:9": return 16f / 9f;
                case "9:16": return 9f / 16f;
                case "1:1": return 1f;
                case "4:3": return 4f / 3f;
                case "4:9": return 4f / 9f;
                case "fit":
                case "fullscreen":
                default:
                    return naturalH > 0.01f ? naturalW / naturalH : 16f / 9f;
            }
        }

        public static bool UseCoverMode()
        {
            return FitMode == "fullscreen";
        }

        private static bool ParseBool(string s, bool fallback)
        {
            bool b;
            return bool.TryParse(s, out b) ? b : fallback;
        }

        private static int ParseInt(string s, int fallback)
        {
            int i;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out i) ? i : fallback;
        }

        private static float ParseFloat(string s, float fallback)
        {
            float f;
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f) ? f : fallback;
        }
    }

    internal static class WallpaperState
    {
        private static readonly object gate = new object();
        private static string original;
        private static string stored;
        private static string lastDir;
        private static int token;

        private static string StateFile { get { return Path.Combine(WallpaperConfig.ModFolder ?? "", "wallpaper.txt"); } }
        private static string StoreDir { get { return Path.Combine(WallpaperConfig.ModFolder ?? "", "Wallpapers"); } }

        public static string LastDirectory
        {
            get { lock (gate) { return lastDir; } }
        }

        public static void Load()
        {
            lock (gate)
            {
                try
                {
                    if (string.IsNullOrEmpty(WallpaperConfig.ModFolder) || !File.Exists(StateFile)) return;
                    foreach (string raw in File.ReadAllLines(StateFile, Encoding.UTF8))
                    {
                        int eq = raw.IndexOf('=');
                        if (eq <= 0) continue;
                        string key = raw.Substring(0, eq).Trim().ToLowerInvariant();
                        string val = raw.Substring(eq + 1).Trim();
                        if (val.Length == 0) continue;
                        if (key == "original") original = val;
                        else if (key == "stored") stored = val;
                        else if (key == "lastdir") lastDir = val;
                    }
                    CleanupStore();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[MenuWallpaper] wallpaper.txt read failed: " + e.Message);
                }
            }
        }

        public static string GetPlayablePath()
        {
            lock (gate)
            {
                if (!string.IsNullOrEmpty(stored) && File.Exists(stored)) return stored;
                if (!string.IsNullOrEmpty(original) && File.Exists(original)) return original;
                return null;
            }
        }

        public static bool HasWallpaper
        {
            get { return GetPlayablePath() != null; }
        }

        public static void SetNew(string path)
        {
            int myToken;
            string oldStored;
            lock (gate)
            {
                token++;
                myToken = token;
                oldStored = stored;
                original = path;
                stored = null;
                try { lastDir = Path.GetDirectoryName(path); } catch (Exception) { }
                Save();
            }
            TryDelete(oldStored);

            if (!WallpaperConfig.CopyFiles) return;

            Thread t = new Thread(delegate () { CopyWorker(path, myToken); });
            t.IsBackground = true;
            t.Name = "MenuWallpaper-Copy";
            t.Start();
        }

        public static void Clear()
        {
            string oldStored;
            lock (gate)
            {
                token++;
                oldStored = stored;
                original = null;
                stored = null;
                Save();
            }
            TryDelete(oldStored);
        }

        private static void CopyWorker(string src, int myToken)
        {
            string dest = null;
            try
            {
                if (string.IsNullOrEmpty(WallpaperConfig.ModFolder)) return;
                FileInfo fi = new FileInfo(src);
                if (!fi.Exists) return;
                if (fi.Length > WallpaperConfig.MaxCopyMB * 1024L * 1024L) return;

                string dir = StoreDir;
                string full = Path.GetFullPath(src);
                if (full.StartsWith(Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase)) return;

                Directory.CreateDirectory(dir);
                dest = Path.Combine(dir, "wp_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture) +
                                         fi.Extension.ToLowerInvariant());
                File.Copy(src, dest, true);

                string toDelete = null;
                lock (gate)
                {
                    if (myToken == token)
                    {
                        toDelete = stored;
                        stored = dest;
                        dest = null;
                        Save();
                    }
                }
                TryDelete(toDelete);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MenuWallpaper] Could not copy the wallpaper into the mod folder (it will keep using the original file): " + e.Message);
            }
            finally
            {
                if (dest != null) TryDelete(dest);
            }
        }

        private static void Save()
        {
            try
            {
                if (string.IsNullOrEmpty(WallpaperConfig.ModFolder)) return;
                StringBuilder sb = new StringBuilder();
                if (!string.IsNullOrEmpty(original)) sb.AppendLine("original=" + original);
                if (!string.IsNullOrEmpty(stored)) sb.AppendLine("stored=" + stored);
                if (!string.IsNullOrEmpty(lastDir)) sb.AppendLine("lastdir=" + lastDir);
                File.WriteAllText(StateFile, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MenuWallpaper] Could not save wallpaper.txt: " + e.Message);
            }
        }

        private static void CleanupStore()
        {
            try
            {
                string dir = StoreDir;
                if (!Directory.Exists(dir)) return;
                foreach (string f in Directory.GetFiles(dir))
                {
                    if (!string.IsNullOrEmpty(stored) &&
                        string.Equals(Path.GetFullPath(f), Path.GetFullPath(stored), StringComparison.OrdinalIgnoreCase))
                        continue;
                    TryDelete(f);
                }
            }
            catch (Exception) { }
        }

        private static void TryDelete(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception) { /* file may still be in use */ }
        }
    }
}

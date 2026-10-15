using System;
using System.Reflection;
using SFS.UI.ModGUI;
using UnityEngine;
using UnityEngine.UI;
using ModGuiType = SFS.UI.ModGUI.Type;

namespace MenuWallpaper
{
    internal static class WallpaperSettingsMenu
    {
        private static readonly string[] FitModes =
        {
            "fullscreen", "fit", "16:9", "9:16", "1:1", "4:3", "4:9"
        };

        private static readonly string[] FitLabels =
        {
            "Full Screen", "Fit", "16:9", "9:16", "1:1", "4:3", "4:9"
        };

        public static void Register()
        {
            try
            {
                System.Type configMenu = FindType("UITools.ConfigurationMenu");
                if (configMenu == null) return;

                MethodInfo add = configMenu.GetMethod(
                    "Add",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof((string, Func<Transform, GameObject>)[]) },
                    null);

                if (add == null)
                {
                    foreach (MethodInfo m in configMenu.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (m.Name != "Add") continue;
                        ParameterInfo[] ps = m.GetParameters();
                        if (ps.Length == 2 && ps[0].ParameterType == typeof(string))
                        {
                            add = m;
                            break;
                        }
                    }
                }

                if (add == null) return;

                var buttons = new (string name, Func<Transform, GameObject> createWindowFunc)[]
                {
                    ("Wallpaper", CreateWallpaperPanel)
                };

                add.Invoke(null, new object[] { "Better UI", buttons });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BetterUI] UITools settings not registered: " + e.Message);
            }
        }

        private static System.Type FindType(string fullName)
        {
            System.Type t = System.Type.GetType(fullName + ", UITools");
            if (t != null) return t;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = a.GetType(fullName);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }

        private static GameObject CreateWallpaperPanel(Transform parent)
        {
            Vector2Int size = GetContentSize();
            Box box = Builder.CreateBox(parent, size.x, size.y);
            box.CreateLayoutGroup(ModGuiType.Vertical, TextAnchor.UpperCenter, 8, new RectOffset(12, 12, 12, 12));

            var title = Builder.CreateLabel(box, size.x - 40, 30, text: "Menu Wallpaper");
            try { title.TextAlignment = TMPro.TextAlignmentOptions.Center; } catch { }

            Builder.CreateButton(parent: box, width: size.x - 40, height: 42, text: "Change Wallpaper", onClick: ChangeWallpaper);
            Builder.CreateButton(parent: box, width: size.x - 40, height: 42, text: "Reset Wallpaper", onClick: ResetWallpaper);

            var sizeLabel = Builder.CreateLabel(box, size.x - 40, 28, text: "Display Size");
            try { sizeLabel.TextAlignment = TMPro.TextAlignmentOptions.Center; } catch { }

            int btnW = Math.Max(72, (size.x - 90) / 4);
            for (int row = 0; row < 2; row++)
            {
                Container line = Builder.CreateContainer(box);
                line.CreateLayoutGroup(ModGuiType.Horizontal, TextAnchor.MiddleCenter, 6);
                int start = row * 4;
                int end = Math.Min(start + 4, FitModes.Length);
                for (int i = start; i < end; i++)
                {
                    string mode = FitModes[i];
                    string label = FitLabels[i];
                    Builder.CreateButton(parent: line, width: btnW, height: 36, text: label, onClick: () => SelectFit(mode));
                }
            }

            Builder.CreateToggleWithLabel(
                parent: box,
                width: size.x - 40,
                height: 36,
                labelText: "Video Sound",
                getToggleValue: () => WallpaperConfig.VideoSound,
                onChange: () =>
                {
                    WallpaperConfig.VideoSound = !WallpaperConfig.VideoSound;
                    WallpaperConfig.SaveSettingsKeys();
                    WallpaperService.ApplyVideoAudio();
                });

            return box.gameObject;
        }

        private static Vector2Int GetContentSize()
        {
            try
            {
                System.Type configMenu = FindType("UITools.ConfigurationMenu");
                PropertyInfo prop = configMenu != null
                    ? configMenu.GetProperty("ContentSize", BindingFlags.Public | BindingFlags.Static)
                    : null;
                if (prop != null && prop.GetValue(null) is Vector2Int v)
                    return v;
            }
            catch { }
            return new Vector2Int(520, 700);
        }

        private static void ChangeWallpaper()
        {
            try { WallpaperService.ChangeWallpaper(); }
            catch (Exception e) { Debug.LogWarning("[BetterUI] ChangeWallpaper: " + e.Message); }
        }

        private static void ResetWallpaper()
        {
            try { WallpaperService.ResetWallpaper(); }
            catch (Exception e) { Debug.LogWarning("[BetterUI] ResetWallpaper: " + e.Message); }
        }

        private static void SelectFit(string mode)
        {
            WallpaperConfig.SetFitMode(mode);
            WallpaperService.ApplyFitMode();
            WallpaperService.Toast("Wallpaper size: " + mode);
        }
    }
}

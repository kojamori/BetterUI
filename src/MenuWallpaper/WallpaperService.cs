// This file is code contributed by Luna Tomato
// Copyright (c) 2026 Luna Tomato
// Licensed under the MIT License


using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using GameButton = SFS.UI.Button;
using GameButtonPC = SFS.UI.ButtonPC;
using HomeManager = SFS.UI.HomeManager;
using BetterUI.Utils.MenuWallpaper;

namespace MenuWallpaper
{
    // Keeps cloned menu button labels from being overwritten
    public sealed class WallpaperButtonLabel : MonoBehaviour
    {
        public string Label;
        private GameButtonPC pc;

        private void Awake()
        {
            pc = GetComponent<GameButtonPC>();
        }

        private void LateUpdate()
        {
            if (pc == null || pc.ButtonText == null || string.IsNullOrEmpty(Label)) return;
            if (pc.ButtonText.text != Label) pc.ButtonText.text = Label;
        }
    }

    internal static class WallpaperService
    {
        private static WallpaperView view;
        private static SetupRunner runner;

        private const string FileFilter =
            "Wallpapers (PNG, JPG, GIF, MP4...)\0*.png;*.jpg;*.jpeg;*.gif;*.mp4;*.m4v;*.mov;*.webm;*.avi;*.wmv;*.mpg;*.mpeg\0" +
            "All files (*.*)\0*.*\0";

        public static void OnHomeStart(HomeManager home)
        {
            if (view != null && !view)
                view = null;

            try { WallpaperState.Load(); } catch { }

            if (view == null)
            {
                if (TryInstall(home)) return;
                EnsureRunner();
                return;
            }

            ApplySavedToExistingView();
        }

        public static void ApplySavedToExistingView()
        {
            try { WallpaperState.Load(); } catch { }
            if (view == null || !view) { view = null; return; }

            string path = WallpaperState.GetPlayablePath();
            if (path == null) return;

            if (!string.IsNullOrEmpty(view.CurrentPath) &&
                string.Equals(view.CurrentPath, path, StringComparison.OrdinalIgnoreCase))
                return;

            string error;
            if (view.Play(path, out error))
                SetStarsVisible(false);
            else
                SetStarsVisible(true);
        }

        internal static void NotifyViewDestroyed(WallpaperView v)
        {
            if (view == v) view = null;
        }

        public static void EnsureRunner()
        {
            if (view != null) return;
            if (runner != null) return;
            try
            {
                GameObject go = new GameObject("MenuWallpaper_SetupRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<SetupRunner>();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MenuWallpaper] Could not start setup runner: " + e.Message);
            }
        }

        private static bool TryInstall(HomeManager home)
        {
            try
            {
                if (view != null && !view) view = null;
                if (view != null) return true;

                GameButton settings = ResolveSettingsButton(home);
                if (settings == null) return false;

                Canvas canvas = settings.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    Debug.LogWarning("[MenuWallpaper] The menu has no Canvas - waiting...");
                    return false;
                }
                canvas = canvas.rootCanvas;

                view = CreateView(canvas);
                view.OnFailed = OnPlaybackFailed;
                CreateButtons(settings);

                string path = WallpaperState.GetPlayablePath();
                if (path != null)
                {
                    string error;
                    if (view.Play(path, out error))
                        SetStarsVisible(false);
                    else
                        SetStarsVisible(true);
                }

                if (runner != null)
                {
                    UnityEngine.Object.Destroy(runner.gameObject);
                    runner = null;
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[MenuWallpaper] Setup failed: " + e);
                return false;
            }
        }

        private static GameButton ResolveSettingsButton(HomeManager home)
        {
            if (home != null)
            {
                try
                {
                    if (home.settingsButton != null) return home.settingsButton;
                }
                catch { }

                try
                {
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                    foreach (var f in home.GetType().GetFields(flags))
                    {
                        if (!typeof(GameButton).IsAssignableFrom(f.FieldType)) continue;
                        if (f.Name.IndexOf("settings", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        GameButton b = f.GetValue(home) as GameButton;
                        if (b != null) return b;
                    }
                }
                catch { }
            }

            try
            {
                GameButton[] all = UnityEngine.Object.FindObjectsByType<GameButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (GameButton b in all)
                {
                    if (b == null || !b.gameObject.activeInHierarchy) continue;
                    string label = ReadButtonLabel(b);
                    if (label != null && label.IndexOf("Settings", StringComparison.OrdinalIgnoreCase) >= 0)
                        return b;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MenuWallpaper] Settings button search failed: " + e.Message);
            }
            return null;
        }

        private static string ReadButtonLabel(GameButton b)
        {
            try
            {
                GameButtonPC pc = b.GetComponent<GameButtonPC>();
                if (pc != null && pc.ButtonText != null && !string.IsNullOrEmpty(pc.ButtonText.text))
                    return pc.ButtonText.text;
            }
            catch { }
            try
            {
                foreach (TMPro.TMP_Text t in b.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    if (t != null && !string.IsNullOrEmpty(t.text)) return t.text;
                foreach (Text t in b.GetComponentsInChildren<Text>(true))
                    if (t != null && !string.IsNullOrEmpty(t.text)) return t.text;
            }
            catch { }
            return null;
        }

        private sealed class SetupRunner : MonoBehaviour
        {
            private float elapsed;
            private const float Timeout = 15f;
            private float nextLog = 2f;

            private void Update()
            {
                if (view != null)
                {
                    Destroy(gameObject);
                    runner = null;
                    return;
                }

                elapsed += Time.unscaledDeltaTime;
                HomeManager home = null;
                try { home = HomeManager.main; } catch { }

                if (TryInstall(home)) return;

                if (elapsed >= nextLog)
                {
                    Debug.Log("[MenuWallpaper] Waiting for Settings button... t=" + elapsed.ToString("0.0") + "s");
                    nextLog += 3f;
                }

                if (elapsed >= Timeout)
                {
                    Debug.LogWarning("[MenuWallpaper] Home menu / Settings button not found after " + Timeout + "s - giving up.");
                    Destroy(gameObject);
                    runner = null;
                }
            }
        }

        private static WallpaperView CreateView(Canvas canvas)
        {
            GameObject root = new GameObject("MenuWallpaper", typeof(RectTransform), typeof(LayoutElement));
            root.layer = canvas.gameObject.layer;
            RectTransform rt = (RectTransform)root.transform;
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();
            root.GetComponent<LayoutElement>().ignoreLayout = true;

            GameObject imgGo = new GameObject("Image", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            imgGo.layer = root.layer;
            imgGo.transform.SetParent(rt, false);

            RawImage raw = imgGo.GetComponent<RawImage>();
            raw.raycastTarget = false;

            AspectRatioFitter fit = imgGo.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;

            WallpaperView v = root.AddComponent<WallpaperView>();
            v.Init(raw, fit);
            return v;
        }

        internal static void SetStarsVisible(bool visible)
        {
            try
            {
                Stars[] stars = UnityEngine.Object.FindObjectsByType<Stars>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Stars s in stars)
                {
                    if (s != null) s.gameObject.SetActive(visible);
                }

                SFS.ShootingStarManager[] shooters =
                    UnityEngine.Object.FindObjectsByType<SFS.ShootingStarManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (SFS.ShootingStarManager m in shooters)
                {
                    if (m == null) continue;
                    m.enabled = visible;
                    if (m.starHolder != null) m.starHolder.SetActive(visible);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MenuWallpaper] Could not toggle the stars: " + e.Message);
            }
        }

        private static void CreateButtons(GameButton settings)
        {
            Transform parent = settings.transform.parent;
            int insertAt = settings.transform.GetSiblingIndex() + 1;
            bool hasLayout = parent != null && parent.GetComponent<LayoutGroup>() != null;

            List<GameButton> created = new List<GameButton>();
            created.Add(CloneButton(settings, "Change Wallpaper", OnChangeClicked));
            if (WallpaperConfig.ShowResetButton)
                created.Add(CloneButton(settings, "Reset Wallpaper", OnResetClicked));

            if (hasLayout)
            {
                for (int i = 0; i < created.Count; i++)
                    created[i].transform.SetSiblingIndex(insertAt + i);
                RectTransform prt = parent as RectTransform;
                if (prt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(prt);
            }
            else
            {
                PlaceManually(settings, created);
            }

            LogLayout(settings, created, hasLayout);
        }

        private static GameButton CloneButton(GameButton source, string label, Action onClick)
        {
            GameObject go = UnityEngine.Object.Instantiate(source.gameObject, source.transform.parent);
            go.name = "WallpaperButton_" + label.Replace(' ', '_');
            go.SetActive(true);

            GameButton b = go.GetComponent<GameButton>();

            if (b.clickEvent != null)
            {
                int n = b.clickEvent.GetPersistentEventCount();
                for (int i = 0; i < n; i++)
                    b.clickEvent.SetPersistentListenerState(i, UnityEventCallState.Off);
                b.clickEvent.RemoveAllListeners();
            }

            SetLabel(go, label);

            WallpaperButtonLabel keeper = go.AddComponent<WallpaperButtonLabel>();
            keeper.Label = label;

            b.onClick += (SFS.Input.OnInputEndData data) => onClick();
            return b;
        }

        private static void SetLabel(GameObject go, string label)
        {
            GameButtonPC pc = go.GetComponent<GameButtonPC>();
            if (pc != null && pc.ButtonText != null)
            {
                pc.ButtonText.text = label;
                return;
            }

            foreach (Text t in go.GetComponentsInChildren<Text>(true)) t.text = label;
            foreach (TMPro.TMP_Text t in go.GetComponentsInChildren<TMPro.TMP_Text>(true)) t.text = label;
        }

        private static void PlaceManually(GameButton settings, List<GameButton> created)
        {
            RectTransform sRT = settings.transform as RectTransform;
            Transform parent = settings.transform.parent;
            if (sRT == null || parent == null) return;

            float sy = sRT.localPosition.y;
            float sx = sRT.localPosition.x;
            float height = Mathf.Max(1f, sRT.rect.height);
            float width = Mathf.Max(1f, sRT.rect.width);

            List<RectTransform> below = new List<RectTransform>();
            float nearest = float.MaxValue;

            for (int i = 0; i < parent.childCount; i++)
            {
                RectTransform c = parent.GetChild(i) as RectTransform;
                if (c == null || c == sRT || !c.gameObject.activeSelf) continue;
                if (IsCreated(c, created)) continue;

                float dy = sy - c.localPosition.y;
                float dx = Mathf.Abs(c.localPosition.x - sx);
                if (dy > 1f && dy <= height * 8f && dx < width * 0.5f)
                {
                    below.Add(c);
                    if (dy < nearest) nearest = dy;
                }
            }

            float spacing = nearest < float.MaxValue ? nearest : height + 6f;
            float shift = spacing * created.Count;

            foreach (RectTransform c in below)
            {
                Vector3 lp = c.localPosition;
                lp.y -= shift;
                c.localPosition = lp;
            }

            for (int i = 0; i < created.Count; i++)
            {
                RectTransform r = (RectTransform)created[i].transform;
                Vector3 lp = sRT.localPosition;
                lp.y = sy - spacing * (i + 1);
                r.localPosition = lp;
            }
        }

        private static bool IsCreated(RectTransform c, List<GameButton> created)
        {
            foreach (GameButton b in created)
            {
                if (b != null && b.transform == c) return true;
            }
            return false;
        }

        private static void LogLayout(GameButton settings, List<GameButton> created, bool hasLayout)
        {
            try
            {
                Transform parent = settings.transform.parent;
                StringBuilder sb = new StringBuilder();
                sb.Append("[MenuWallpaper] Menu layout: parent='").Append(parent != null ? parent.name : "?")
                  .Append("' layoutGroup=").Append(hasLayout).Append(" children: ");
                if (parent != null)
                {
                    for (int i = 0; i < parent.childCount; i++)
                    {
                        Transform c = parent.GetChild(i);
                        sb.Append(c.name).Append('(').Append(c.gameObject.activeSelf ? "on" : "off").Append(' ')
                          .Append(c.localPosition.x.ToString("0")).Append(',')
                          .Append(c.localPosition.y.ToString("0")).Append(") ");
                    }
                }
                Debug.Log(sb.ToString());
            }
            catch (Exception) { }
        }

                        private static void OnChangeClicked()
        {
            try
            {
                if (view == null) return;

                if (!NativeFileDialog.IsSupported)
                {
                    Toast("The file picker only works on Windows. Put the file path into wallpaper.txt instead.");
                    return;
                }

                string picked = NativeFileDialog.ShowOpen(
                    "Choose a wallpaper (PNG, JPG, GIF, MP4)",
                    FileFilter,
                    WallpaperState.LastDirectory);

                if (string.IsNullOrEmpty(picked)) return;

                string error;
                if (view.Play(picked, out error))
                {
                    SetStarsVisible(false);
                    WallpaperState.SetNew(picked);
                    Toast("Wallpaper changed");
                }
                else
                {
                    Toast(error);
                    string current = WallpaperState.GetPlayablePath();
                    string ignored;
                    if (current != null && view.Play(current, out ignored)) SetStarsVisible(false);
                    else SetStarsVisible(true);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[MenuWallpaper] Change wallpaper failed: " + e);
                Toast("Something went wrong - check Player.log");
            }
        }

        private static void OnResetClicked()
        {
            try
            {
                if (view == null) return;
                if (!WallpaperState.HasWallpaper && view.CurrentPath == null)
                {
                    Toast("No custom wallpaper is set");
                    return;
                }
                view.Stop();
                SetStarsVisible(true);
                WallpaperState.Clear();
                Toast("Wallpaper reset - the stars are back");
            }
            catch (Exception e)
            {
                Debug.LogError("[MenuWallpaper] Reset wallpaper failed: " + e);
            }
        }

        private static void OnPlaybackFailed(string message)
        {
            Toast(message);
            SetStarsVisible(true);
            WallpaperState.Clear();
        }

        internal static void Toast(string message)
        {
            Debug.Log("[MenuWallpaper] " + message);
            try
            {
                if (SFS.UI.MsgDrawer.main != null) SFS.UI.MsgDrawer.main.Log(message);
            }
            catch (Exception) { }
        }
    }
}

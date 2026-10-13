// This file is code contributed by Luna Tomato
// Copyright (c) 2026 Luna Tomato
// Licensed under the MIT License


using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using BetterUI.Utils.MenuWallpaper;

namespace MenuWallpaper
{
    // Menu background: still image, GIF, or looping video
    public sealed class WallpaperView : MonoBehaviour
    {
        private RawImage image;
        private AspectRatioFitter fitter;

        private Texture2D texture;       
        private GifPump pump;
        private float nextFrameTime;

        private GameObject videoObject;
        private VideoPlayer video;
        private bool videoPrepared;

        private bool waitingForFirstFrame;

        public Action<string> OnFailed;

        public string CurrentPath { get; private set; }

        public void Init(RawImage rawImage, AspectRatioFitter aspectFitter)
        {
            image = rawImage;
            fitter = aspectFitter;
            image.enabled = false;
        }

        private static bool IsVideoExtension(string ext)
        {
            switch (ext)
            {
                case ".mp4":
                case ".m4v":
                case ".mov":
                case ".webm":
                case ".avi":
                case ".wmv":
                case ".mpg":
                case ".mpeg":
                    return true;
                default:
                    return false;
            }
        }

        public bool Play(string path, out string error)
        {
            error = null;
            StopInternal();

            string ext = Path.GetExtension(path).ToLowerInvariant();
            try
            {
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                {
                    PlayImage(path);
                }
                else if (ext == ".gif")
                {
                    PlayGif(path);
                }
                else if (ext == ".webp")
                {
                    error = "WebP isn't supported yet. Please use PNG, JPG, GIF or MP4.";
                    return false;
                }
                else if (IsVideoExtension(ext))
                {
                    PlayVideo(path);
                }
                else
                {
                    error = "Unsupported file type (" + ext + "). Please use PNG, JPG, GIF or MP4.";
                    return false;
                }
            }
            catch (Exception e)
            {
                StopInternal();
                image.enabled = false;
                error = "Couldn't open this file: " + e.Message;
                Debug.LogWarning("[MenuWallpaper] " + e);
                return false;
            }

            CurrentPath = path;
            return true;
        }

        public void Stop()
        {
            StopInternal();
            if (image != null) image.enabled = false;
        }

        private void PlayImage(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!TryLoadImage(tex, bytes))
            {
                Destroy(tex);
                throw new Exception("the image is damaged or in an unsupported format");
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            texture = tex;
            Show(tex, tex.width, tex.height);
        }

        // -------------------------------------------------------------------- GIF
        private void PlayGif(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            var decoder = new BetterUI.Utils.MenuWallpaper.GifDecoder(bytes);
            int w = decoder.Width, h = decoder.Height;

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            texture = tex;

            if (decoder.FrameCount <= 1)
            {
                byte[] buf = new byte[w * h * 4];
                int delay;
                decoder.NextFrame(buf, out delay);
                tex.LoadRawTextureData(buf);
                tex.Apply(false, false);
                Show(tex, w, h);
            }
            else
            {
                pump = new GifPump(decoder, (long)w * h > 4L * 1024 * 1024 ? 3 : 4);
                nextFrameTime = 0f;
                waitingForFirstFrame = true;
                Show(tex, w, h);
                image.enabled = false;
            }
        }

        private void PlayVideo(string path)
        {
            videoObject = new GameObject("WallpaperVideo");
            videoObject.transform.SetParent(transform, false);

            video = videoObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.isLooping = true;
            video.skipOnDrop = true;
            video.loopPointReached += OnVideoLoopPoint;
            video.waitForFirstFrame = true;
            video.renderMode = VideoRenderMode.APIOnly;
            video.source = VideoSource.Url;
            video.url = path;

            if (WallpaperConfig.VideoSound)
            {
                video.audioOutputMode = VideoAudioOutputMode.Direct;
                video.controlledAudioTrackCount = 1;
            }
            else
            {
                video.audioOutputMode = VideoAudioOutputMode.None;
            }

            video.prepareCompleted += OnVideoPrepared;
            video.errorReceived += OnVideoError;
            videoPrepared = false;
            waitingForFirstFrame = true;
            image.enabled = false;
            video.Prepare();
        }

        private void OnVideoPrepared(VideoPlayer vp)
        {
            if (vp != video) return;
            if (WallpaperConfig.VideoSound && vp.audioTrackCount > 0)
                vp.SetDirectAudioVolume(0, WallpaperConfig.VideoVolume);
            videoPrepared = true;
            vp.Play();
        }

        private void OnVideoError(VideoPlayer vp, string message)
        {
            if (vp != video) return;
            Fail("Can't play this video (" + message + "). H.264 MP4 works best.");
        }

        private void OnVideoLoopPoint(VideoPlayer vp)
        {
            if (vp != video) return;
            try
            {
                vp.time = 0;
                if (!vp.isPlaying) vp.Play();
            }
            catch { }
        }

        private void Show(Texture tex, float w, float h)
        {
            image.texture = tex;
            image.uvRect = new Rect(0f, 0f, 1f, 1f);
            image.color = Color.white;
            fitter.aspectMode = WallpaperConfig.Cover
                ? AspectRatioFitter.AspectMode.EnvelopeParent
                : AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = h > 0f ? w / h : 16f / 9f;
            image.enabled = true;
        }

        private void Fail(string message)
        {
            StopInternal();
            if (image != null) image.enabled = false;
            if (OnFailed != null) OnFailed(message);
        }

        private void Update()
        {
            if (pump != null)
            {
                if (pump.Error != null)
                {
                    Fail("GIF playback error: " + pump.Error);
                    return;
                }

                float now = Time.unscaledTime;
                if (now >= nextFrameTime)
                {
                    GifSlot slot = pump.TryTake();
                    if (slot != null)
                    {
                        texture.LoadRawTextureData(slot.data);
                        texture.Apply(false, false);
                        if (waitingForFirstFrame)
                        {
                            waitingForFirstFrame = false;
                            image.enabled = true;
                        }

                        float delay = slot.delayMs / 1000f;
                        if (nextFrameTime > 0f && now - nextFrameTime < 0.25f) nextFrameTime += delay;
                        else nextFrameTime = now + delay;

                        pump.Release(slot);
                    }
                }
            }

            if (video != null && videoPrepared)
            {
                if (!waitingForFirstFrame && !video.isPlaying)
                {
                    try
                    {
                        video.time = 0;
                        video.Play();
                    }
                    catch { }
                }

                Texture vt = video.texture;
                if (vt != null)
                {
                    if (waitingForFirstFrame && video.frame > 0)
                    {
                        waitingForFirstFrame = false;
                        Show(vt, vt.width, vt.height);
                    }
                    else if (!waitingForFirstFrame && image.texture != vt)
                    {
                        image.texture = vt;
                    }
                }
            }
        }

        private void StopInternal()
        {
            if (pump != null)
            {
                pump.Stop();
                pump = null;
            }

            if (video != null)
            {
                video.prepareCompleted -= OnVideoPrepared;
                video.errorReceived -= OnVideoError;
                video.loopPointReached -= OnVideoLoopPoint;
                video.Stop();
            }
            if (videoObject != null) Destroy(videoObject);
            video = null;
            videoObject = null;
            videoPrepared = false;

            if (image != null) image.texture = null;
            if (texture != null)
            {
                Destroy(texture);
                texture = null;
            }

            waitingForFirstFrame = false;
            CurrentPath = null;
        }

        private void OnDestroy()
        {
            StopInternal();
        }

        private static bool TryLoadImage(Texture2D tex, byte[] bytes)
        {
            try
            {
                var m = typeof(Texture2D).GetMethod(
                    "LoadImage",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                    null,
                    new[] { typeof(byte[]), typeof(bool) },
                    null);
                if (m != null)
                    return (bool)m.Invoke(tex, new object[] { bytes, true });

                m = typeof(Texture2D).GetMethod(
                    "LoadImage",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                    null,
                    new[] { typeof(byte[]) },
                    null);
                if (m != null)
                    return (bool)m.Invoke(tex, new object[] { bytes });

                var t = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                var sm = t?.GetMethod(
                    "LoadImage",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null,
                    new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) },
                    null);
                if (sm != null)
                    return (bool)sm.Invoke(null, new object[] { tex, bytes, true });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MenuWallpaper] LoadImage failed: " + e.Message);
            }
            return false;
        }
    }
}

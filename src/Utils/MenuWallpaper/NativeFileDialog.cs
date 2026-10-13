// This file is code contributed by Luna Tomato
// Copyright (c) 2026 Luna Tomato
// Licensed under the MIT License


using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BetterUI.Utils.MenuWallpaper;

internal static class NativeFileDialog
{
    [StructLayout(LayoutKind.Sequential)]
    private struct OPENFILENAME
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public IntPtr lpstrFilter;
        public IntPtr lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public IntPtr lpstrFile;
        public int nMaxFile;
        public IntPtr lpstrFileTitle;
        public int nMaxFileTitle;
        public IntPtr lpstrInitialDir;
        public IntPtr lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public IntPtr lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public IntPtr lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int FlagsEx;
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetOpenFileNameW")]
    private static extern bool GetOpenFileName(ref OPENFILENAME ofn);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private const int OFN_HIDEREADONLY = 0x00000004;
    private const int OFN_NOCHANGEDIR = 0x00000008;
    private const int OFN_PATHMUSTEXIST = 0x00000800;
    private const int OFN_FILEMUSTEXIST = 0x00001000;
    private const int OFN_EXPLORER = 0x00080000;

    private const int MaxFileChars = 32768;

    public static bool IsSupported
    {
        get { return Environment.OSVersion.Platform == PlatformID.Win32NT; }
    }

    public static string ShowOpen(string title, string filter, string initialDir)
    {
        if (!IsSupported) return null;

        IntPtr fileBuf = IntPtr.Zero, filterPtr = IntPtr.Zero, titlePtr = IntPtr.Zero, dirPtr = IntPtr.Zero;
        try
        {
            fileBuf = Marshal.AllocHGlobal(MaxFileChars * 2);
            Marshal.WriteInt16(fileBuf, 0, 0);

            filterPtr = Marshal.StringToHGlobalUni(filter);
            titlePtr = Marshal.StringToHGlobalUni(title ?? "");
            if (!string.IsNullOrEmpty(initialDir) && System.IO.Directory.Exists(initialDir))
                dirPtr = Marshal.StringToHGlobalUni(initialDir);

            OPENFILENAME ofn = new OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf(typeof(OPENFILENAME));
            ofn.hwndOwner = GetActiveWindow();
            ofn.lpstrFilter = filterPtr;
            ofn.nFilterIndex = 1;
            ofn.lpstrFile = fileBuf;
            ofn.nMaxFile = MaxFileChars;
            ofn.lpstrInitialDir = dirPtr;
            ofn.lpstrTitle = titlePtr;
            ofn.Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR | OFN_HIDEREADONLY;

            if (!GetOpenFileName(ref ofn)) return null;

            string result = Marshal.PtrToStringUni(fileBuf);
            return string.IsNullOrEmpty(result) ? null : result;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BetterUI] File dialog failed: " + e);
            return null;
        }
        finally
        {
            if (fileBuf != IntPtr.Zero) Marshal.FreeHGlobal(fileBuf);
            if (filterPtr != IntPtr.Zero) Marshal.FreeHGlobal(filterPtr);
            if (titlePtr != IntPtr.Zero) Marshal.FreeHGlobal(titlePtr);
            if (dirPtr != IntPtr.Zero) Marshal.FreeHGlobal(dirPtr);
        }
    }
}


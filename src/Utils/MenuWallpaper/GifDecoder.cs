// This file is code contributed by Luna Tomato
// Copyright (c) 2026 Luna Tomato
// Licensed under the MIT License


using System;
using System.Collections.Generic;
using System.Threading;

namespace BetterUI.Utils.MenuWallpaper;

internal sealed class GifFrameInfo
{
    public int Left, Top, Width, Height;
    public bool Interlaced;
    public int PalettePos = -1;
    public int PaletteCount;
    public int MinCode;
    public int DataPos;
    public int Disposal;
    public int Transparent = -1;
    public int DelayMs = 100;
}

internal sealed class GifDecoder
{
    private const int MaxPixels = 16 * 1024 * 1024; // ~4096x4096

    private readonly byte[] data;
    private readonly List<GifFrameInfo> frames = new List<GifFrameInfo>();

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int FrameCount { get { return frames.Count; } }

    private int globalPalettePos = -1;
    private int globalPaletteCount;

    private byte[] canvas;
    private byte[] saved;
    private bool hasSaved;
    private GifFrameInfo prev;
    private int index;

    private readonly short[] prefix = new short[4096];
    private readonly byte[] suffix = new byte[4096];
    private readonly byte[] stack = new byte[4097];
    private byte[] lzwBuf = new byte[0];
    private byte[] indices = new byte[0];

    public GifDecoder(byte[] file)
    {
        data = file;
        Parse();
        Reset();
    }

    private int U16(int pos)
    {
        return data[pos] | (data[pos + 1] << 8);
    }

    private int SkipBlocks(int pos)
    {
        while (pos < data.Length)
        {
            int n = data[pos++];
            if (n == 0) break;
            pos += n;
        }
        return pos;
    }

    private void Parse()
    {
        if (data == null || data.Length < 13 || data[0] != (byte)'G' || data[1] != (byte)'I' || data[2] != (byte)'F')
            throw new Exception("Not a GIF file");

        Width = U16(6);
        Height = U16(8);
        if (Width <= 0 || Height <= 0 || (long)Width * Height > MaxPixels)
            throw new Exception("GIF is too large (max ~4096x4096)");

        int packed = data[10];
        int pos = 13;
        if ((packed & 0x80) != 0)
        {
            globalPaletteCount = 2 << (packed & 7);
            globalPalettePos = pos;
            pos += 3 * globalPaletteCount;
        }

        bool hasGce = false;
        int gDisposal = 0, gDelay = 0, gTrans = -1;

        while (pos < data.Length)
        {
            int b = data[pos++];
            if (b == 0x3B) break; // trailer

            if (b == 0x21) // extension
            {
                if (pos >= data.Length) break;
                int label = data[pos++];
                if (label == 0xF9 && pos + 5 <= data.Length && data[pos] >= 4)
                {
                    int p = data[pos + 1];
                    gDelay = U16(pos + 2);
                    gTrans = (p & 1) != 0 ? data[pos + 4] : -1;
                    gDisposal = (p >> 2) & 7;
                    hasGce = true;
                }
                pos = SkipBlocks(pos);
            }
            else if (b == 0x2C) // image
            {
                if (pos + 9 > data.Length) break;
                GifFrameInfo f = new GifFrameInfo();
                f.Left = U16(pos);
                f.Top = U16(pos + 2);
                f.Width = U16(pos + 4);
                f.Height = U16(pos + 6);
                int fp = data[pos + 8];
                pos += 9;
                f.Interlaced = (fp & 0x40) != 0;
                f.PalettePos = globalPalettePos;
                f.PaletteCount = globalPaletteCount;
                if ((fp & 0x80) != 0)
                {
                    f.PaletteCount = 2 << (fp & 7);
                    f.PalettePos = pos;
                    pos += 3 * f.PaletteCount;
                }
                if (pos >= data.Length) break;
                f.MinCode = data[pos++];
                f.DataPos = pos;
                pos = SkipBlocks(pos);

                if (hasGce)
                {
                    f.Disposal = gDisposal;
                    f.Transparent = gTrans;
                    f.DelayMs = gDelay <= 1 ? 100 : gDelay * 10;
                }
                hasGce = false;

                if (f.Width > 0 && f.Height > 0 && (long)f.Width * f.Height <= MaxPixels &&
                    f.MinCode >= 1 && f.MinCode <= 8)
                {
                    frames.Add(f);
                }
            }
            else
            {
                break;
            }
        }

        if (frames.Count == 0) throw new Exception("GIF has no frames");
    }

    private void Reset()
    {
        if (canvas == null) canvas = new byte[Width * Height * 4];
        else Array.Clear(canvas, 0, canvas.Length);
        hasSaved = false;
        prev = null;
        index = 0;
    }

    private void DecodeLzw(GifFrameInfo f, int pixelCount)
    {
        int total = 0;
        int pos = f.DataPos;
        while (pos < data.Length)
        {
            int n = data[pos++];
            if (n == 0) break;
            int take = Math.Min(n, data.Length - pos);
            if (take <= 0) break;
            if (lzwBuf.Length < total + take)
                Array.Resize(ref lzwBuf, Math.Max(lzwBuf.Length * 2, total + take));
            Buffer.BlockCopy(data, pos, lzwBuf, total, take);
            total += take;
            pos += n;
        }

        if (indices.Length < pixelCount) indices = new byte[pixelCount];
        Array.Clear(indices, 0, pixelCount);

        // 2) LZW
        int minCode = f.MinCode;
        int clear = 1 << minCode;
        int eoi = clear + 1;
        for (int i = 0; i < clear; i++)
        {
            prefix[i] = 0;
            suffix[i] = (byte)i;
        }

        int codeSize = minCode + 1;
        int codeMask = (1 << codeSize) - 1;
        int available = clear + 2;
        int oldCode = -1;
        int first = 0;
        int bits = 0;
        int datum = 0;
        int p = 0;
        int top = 0;
        int pi = 0;

        while (pi < pixelCount)
        {
            if (top == 0)
            {
                if (bits < codeSize)
                {
                    if (p >= total) break;
                    datum += lzwBuf[p] << bits;
                    p++;
                    bits += 8;
                    continue;
                }

                int code = datum & codeMask;
                datum >>= codeSize;
                bits -= codeSize;

                if (code > available || code == eoi) break;

                if (code == clear)
                {
                    codeSize = minCode + 1;
                    codeMask = (1 << codeSize) - 1;
                    available = clear + 2;
                    oldCode = -1;
                    continue;
                }

                if (oldCode == -1)
                {
                    stack[top++] = suffix[code];
                    oldCode = code;
                    first = code;
                    continue;
                }

                int inCode = code;
                if (code == available)
                {
                    stack[top++] = (byte)first;
                    code = oldCode;
                }
                while (code > clear)
                {
                    stack[top++] = suffix[code];
                    code = prefix[code];
                }
                first = suffix[code];

                if (available >= 4096) break;

                stack[top++] = (byte)first;
                prefix[available] = (short)oldCode;
                suffix[available] = (byte)first;
                available++;
                if ((available & codeMask) == 0 && available < 4096)
                {
                    codeSize++;
                    codeMask += available;
                }
                oldCode = inCode;
            }

            top--;
            indices[pi++] = stack[top];
        }
    }

    public void NextFrame(byte[] dst, out int delayMs)
    {
        if (index == 0) Reset();

        GifFrameInfo f = frames[index];
        int W = Width, H = Height;
        byte[] cv = canvas;

        GifFrameInfo p = prev;
        if (p != null)
        {
            if (p.Disposal == 2)
            {
                int x0 = Math.Max(p.Left, 0), x1 = Math.Min(p.Left + p.Width, W);
                int y0 = Math.Max(p.Top, 0), y1 = Math.Min(p.Top + p.Height, H);
                if (x1 > x0)
                {
                    for (int y = y0; y < y1; y++)
                        Array.Clear(cv, (y * W + x0) * 4, (x1 - x0) * 4);
                }
            }
            else if (p.Disposal == 3 && hasSaved)
            {
                Buffer.BlockCopy(saved, 0, cv, 0, cv.Length);
            }
        }

        if (f.Disposal == 3)
        {
            if (saved == null || saved.Length != cv.Length) saved = new byte[cv.Length];
            Buffer.BlockCopy(cv, 0, saved, 0, cv.Length);
            hasSaved = true;
        }
        else
        {
            hasSaved = false;
        }

        int fw = f.Width, fh = f.Height;
        DecodeLzw(f, fw * fh);

        byte[] idx = indices;
        int pc = f.PaletteCount;
        int pp = f.PalettePos;
        int tr = f.Transparent;

        int pass1 = (fh + 7) / 8;
        int pass2 = (fh + 3) / 8;
        int pass3 = (fh + 1) / 4;

        for (int row = 0; row < fh; row++)
        {
            int y = row;
            if (f.Interlaced)
            {
                if (row < pass1)
                {
                    y = row * 8;
                }
                else
                {
                    int r2 = row - pass1;
                    if (r2 < pass2)
                    {
                        y = r2 * 8 + 4;
                    }
                    else
                    {
                        int r3 = r2 - pass2;
                        if (r3 < pass3) y = r3 * 4 + 2;
                        else y = (r3 - pass3) * 2 + 1;
                    }
                }
            }

            int cy = f.Top + y;
            if (cy < 0 || cy >= H) continue;

            int rowIdx = row * fw;
            for (int x = 0; x < fw; x++)
            {
                int cx = f.Left + x;
                if (cx < 0 || cx >= W) continue;

                int v = idx[rowIdx + x];
                if (v == tr) continue;

                int o = (cy * W + cx) * 4;
                if (v < pc && pp >= 0 && pp + v * 3 + 2 < data.Length)
                {
                    int po = pp + v * 3;
                    cv[o] = data[po];
                    cv[o + 1] = data[po + 1];
                    cv[o + 2] = data[po + 2];
                }
                else
                {
                    cv[o] = 0;
                    cv[o + 1] = 0;
                    cv[o + 2] = 0;
                }
                cv[o + 3] = 255;
            }
        }

        prev = f;
        index = (index + 1) % frames.Count;
        delayMs = f.DelayMs;

        int stride = W * 4;
        for (int y = 0; y < H; y++)
            Buffer.BlockCopy(cv, y * stride, dst, (H - 1 - y) * stride, stride);
    }
}

internal sealed class GifSlot
{
    public readonly byte[] data;
    public int delayMs;
    public GifSlot(int size) { data = new byte[size]; }
}

internal sealed class GifPump
{
    private readonly GifDecoder decoder;
    private readonly object gate = new object();
    private readonly Queue<GifSlot> ready = new Queue<GifSlot>();
    private readonly Stack<GifSlot> free = new Stack<GifSlot>();
    private volatile bool stopped;
    private volatile string error;

    public string Error { get { return error; } }

    public GifPump(GifDecoder dec, int bufferCount)
    {
        decoder = dec;
        int size = dec.Width * dec.Height * 4;
        for (int i = 0; i < bufferCount; i++) free.Push(new GifSlot(size));

        Thread t = new Thread(Run);
        t.IsBackground = true;
        t.Name = "MenuWallpaper-GIF";
        t.Start();
    }

    private void Run()
    {
        try
        {
            while (!stopped)
            {
                GifSlot slot;
                lock (gate)
                {
                    while (free.Count == 0 && !stopped) Monitor.Wait(gate);
                    if (stopped) return;
                    slot = free.Pop();
                }

                int delay;
                decoder.NextFrame(slot.data, out delay);
                slot.delayMs = delay;

                lock (gate)
                {
                    ready.Enqueue(slot);
                }
            }
        }
        catch (Exception e)
        {
            error = e.Message;
        }
    }

    public GifSlot TryTake()
    {
        lock (gate)
        {
            return ready.Count > 0 ? ready.Dequeue() : null;
        }
    }

    public void Release(GifSlot slot)
    {
        lock (gate)
        {
            free.Push(slot);
            Monitor.Pulse(gate);
        }
    }

    public void Stop()
    {
        stopped = true;
        lock (gate)
        {
            Monitor.PulseAll(gate);
        }
    }
}

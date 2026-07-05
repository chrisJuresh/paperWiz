using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PaperWiz.Services;

/// <summary>
/// A native re-implementation of pywal's default ("wal") colour-scheme generator.
///
/// pywal shrinks the image and asks ImageMagick for 16 representative colours
/// (<c>-colors 16</c>, i.e. median-cut quantisation), then assembles a 16-slot scheme:
/// a darkened background (color0), six accent colours (color1-6), a light foreground
/// (color7/15) and their bright variants (color8-15). We reproduce that here: median-cut
/// quantisation gives the same kind of well-distributed, complementary palette, and the
/// same slot assembly + adjustments give the familiar pywal look — no ImageMagick or
/// Python required.
/// </summary>
public static class PywalGenerator
{
    private const int Sample = 200;         // longest edge of the analysed image
    private const double AccentSaturate = 1.22; // gentle lift so accents pop like pywal's

    private readonly record struct Rgb(byte R, byte G, byte B);

    public static IReadOnlyList<ColorCandidate> Generate(string imagePath)
    {
        var pixels = SamplePixels(imagePath);
        if (pixels.Count == 0)
            return Array.Empty<ColorCandidate>();

        // 16 representative colours via median cut, ordered dark -> light like a terminal palette.
        var cols = MedianCut(pixels, 16).Select(Average).OrderBy(Luminance).ToList();
        while (cols.Count < 16) cols.Add(cols[^1]);
        if (cols.Count > 16) cols = cols.GetRange(0, 16);

        // pywal 'wal' backend layout: raw = cols[:1] + cols[8:16] + cols[8:15]
        var raw = new Color[16];
        raw[0] = cols[0];
        for (int i = 0; i < 8; i++) raw[1 + i] = cols[8 + i]; // color1..8  = cols8..15
        for (int i = 0; i < 7; i++) raw[9 + i] = cols[8 + i]; // color9..15 = cols8..14

        // pywal dark-theme adjustments.
        raw[0] = Darken(cols[0], 0.80);   // background
        raw[7] = cols[15];                // foreground
        raw[8] = Darken(cols[15], 0.30);  // "bright black"
        raw[15] = cols[15];               // bright foreground

        // Make the accent slots vivid (median-cut averages are a touch flatter than IM's).
        foreach (int i in new[] { 1, 2, 3, 4, 5, 6, 9, 10, 11, 12, 13, 14 })
            raw[i] = Saturate(raw[i], AccentSaturate);

        string[] roles =
        {
            "background", "accent 1", "accent 2", "accent 3", "accent 4", "accent 5",
            "accent 6", "foreground", "bright background", "accent 1 (bright)",
            "accent 2 (bright)", "accent 3 (bright)", "accent 4 (bright)",
            "accent 5 (bright)", "accent 6 (bright)", "bright foreground",
        };

        var list = new List<ColorCandidate>(16);
        for (int i = 0; i < 16; i++)
            list.Add(new ColorCandidate(raw[i], $"color{i}",
                $"pywal-style {roles[i]} — a median-cut colour scheme from your wallpaper (the Linux pywal look)."));
        return list;
    }

    // --- median cut ---------------------------------------------------------

    private static List<List<Rgb>> MedianCut(List<Rgb> pixels, int target)
    {
        var boxes = new List<List<Rgb>> { pixels };
        while (boxes.Count < target)
        {
            int splitIndex = -1, widestRange = 0, splitChannel = 0;
            for (int i = 0; i < boxes.Count; i++)
            {
                var box = boxes[i];
                if (box.Count < 2) continue;
                Range(box, out int rr, out int gr, out int br);
                int widest = Math.Max(rr, Math.Max(gr, br));
                if (widest > widestRange)
                {
                    widestRange = widest;
                    splitIndex = i;
                    splitChannel = rr >= gr && rr >= br ? 0 : (gr >= br ? 1 : 2);
                }
            }
            if (splitIndex < 0) break; // every box is a single colour

            var target1 = boxes[splitIndex];
            target1.Sort((a, b) => Channel(a, splitChannel).CompareTo(Channel(b, splitChannel)));
            int mid = target1.Count / 2;
            boxes[splitIndex] = target1.GetRange(0, mid);
            boxes.Add(target1.GetRange(mid, target1.Count - mid));
        }
        return boxes;
    }

    private static void Range(List<Rgb> box, out int r, out int g, out int b)
    {
        byte rmin = 255, gmin = 255, bmin = 255, rmax = 0, gmax = 0, bmax = 0;
        foreach (var p in box)
        {
            if (p.R < rmin) rmin = p.R; if (p.R > rmax) rmax = p.R;
            if (p.G < gmin) gmin = p.G; if (p.G > gmax) gmax = p.G;
            if (p.B < bmin) bmin = p.B; if (p.B > bmax) bmax = p.B;
        }
        r = rmax - rmin; g = gmax - gmin; b = bmax - bmin;
    }

    private static int Channel(Rgb p, int c) => c == 0 ? p.R : c == 1 ? p.G : p.B;

    private static Color Average(List<Rgb> box)
    {
        long r = 0, g = 0, b = 0;
        foreach (var p in box) { r += p.R; g += p.G; b += p.B; }
        int n = Math.Max(1, box.Count);
        return Color.FromArgb(255, (int)(r / n), (int)(g / n), (int)(b / n));
    }

    // --- colour maths (matches pywal's util operations) ---------------------

    private static double Luminance(Color c) => 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;

    private static Color Darken(Color c, double amount) => Color.FromArgb(255,
        (int)(c.R * (1 - amount)), (int)(c.G * (1 - amount)), (int)(c.B * (1 - amount)));

    private static Color Saturate(Color c, double factor)
    {
        RgbToHsl(c, out double h, out double s, out double l);
        s = Math.Clamp(s * factor, 0, 1);
        return HslToRgb(h, s, l);
    }

    private static void RgbToHsl(Color c, out double h, out double s, out double l)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
        l = (max + min) / 2;
        if (max == min) { h = s = 0; return; }
        double d = max - min;
        s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
        if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
        else if (max == g) h = (b - r) / d + 2;
        else h = (r - g) / d + 4;
        h /= 6;
    }

    private static Color HslToRgb(double h, double s, double l)
    {
        double r, g, b;
        if (s == 0) { r = g = b = l; }
        else
        {
            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;
            r = HueToRgb(p, q, h + 1.0 / 3);
            g = HueToRgb(p, q, h);
            b = HueToRgb(p, q, h - 1.0 / 3);
        }
        return Color.FromArgb(255,
            (int)Math.Round(r * 255), (int)Math.Round(g * 255), (int)Math.Round(b * 255));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }

    // --- sampling -----------------------------------------------------------

    private static List<Rgb> SamplePixels(string imagePath)
    {
        using var source = new Bitmap(imagePath);
        int w = source.Width, h = source.Height;
        double scale = Math.Min(1.0, (double)Sample / Math.Max(w, h));
        int tw = Math.Max(1, (int)Math.Round(w * scale));
        int th = Math.Max(1, (int)Math.Round(h * scale));

        using var small = new Bitmap(tw, th, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(small))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, 0, 0, tw, th);
        }

        var pixels = new List<Rgb>(tw * th);
        BitmapData data = small.LockBits(new Rectangle(0, 0, tw, th),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            int stride = data.Stride;
            byte[] buf = new byte[stride * th];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            for (int y = 0; y < th; y++)
            {
                int row = y * stride;
                for (int x = 0; x < tw; x++)
                {
                    int i = row + x * 4;
                    if (buf[i + 3] < 8) continue; // transparent
                    pixels.Add(new Rgb(buf[i + 2], buf[i + 1], buf[i]));
                }
            }
        }
        finally
        {
            small.UnlockBits(data);
        }
        return pixels;
    }
}

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PaperWiz.Services;

/// <summary>A candidate accent colour together with a human-readable reason it was suggested.</summary>
public sealed record ColorCandidate(Color Color, string Label, string Reason);

/// <summary>
/// Extracts accent colours from a wallpaper.
///
/// The <b>Most common</b> candidate uses the exact same approach as the Linux paperWiz script
/// (<c>convert -resize 500x500\> … histogram:unique-colors=true | sort -n | tail -1</c>): the
/// image is shrunk so its longest edge is ≤ 500px and the single most frequent <i>exact</i>
/// colour is chosen — no quantisation, no weighting. The remaining candidates (vibrant, average,
/// darkest, lightest, …) are extra conveniences unique to the Windows build, each labelled with
/// why it was picked.
/// </summary>
public static class ColorExtractor
{
    private const int SampleSize = 500;   // matches the Linux script's `-resize 500x500>`
    private const int ProminentPool = 250; // top-N most frequent exact colours to reason over

    /// <summary>The colour the Linux paperWiz would choose: the most frequent exact colour.</summary>
    public static Color GetDominantColor(string imagePath)
    {
        var candidates = Analyze(imagePath);
        return candidates.Count > 0 ? candidates[0].Color : Color.FromArgb(30, 30, 30);
    }

    /// <summary>Returns labelled accent suggestions, most-common first.</summary>
    public static IReadOnlyList<ColorCandidate> Analyze(string imagePath)
    {
        using Bitmap sample = LoadSample(imagePath);

        var counts = new Dictionary<int, int>();
        long sumR = 0, sumG = 0, sumB = 0, total = 0;

        BitmapData data = sample.LockBits(
            new Rectangle(0, 0, sample.Width, sample.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            int stride = data.Stride, h = sample.Height, w = sample.Width;
            byte[] buf = new byte[stride * h];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buf, 0, buf.Length);

            for (int y = 0; y < h; y++)
            {
                int row = y * stride;
                for (int x = 0; x < w; x++)
                {
                    int i = row + x * 4;
                    byte b = buf[i], g = buf[i + 1], r = buf[i + 2], a = buf[i + 3];
                    if (a < 8) continue; // ignore transparent pixels

                    int key = (r << 16) | (g << 8) | b;
                    counts.TryGetValue(key, out int c);
                    counts[key] = c + 1;
                    sumR += r; sumG += g; sumB += b; total++;
                }
            }
        }
        finally
        {
            sample.UnlockBits(data);
        }

        var result = new List<ColorCandidate>();
        if (total == 0)
        {
            result.Add(new ColorCandidate(Color.FromArgb(30, 30, 30), "Fallback",
                "No colour could be read from this image."));
            return result;
        }

        // Ordered by frequency, exactly like the Linux histogram sort.
        var ordered = counts.OrderByDescending(kv => kv.Value).ToList();
        Color FromKey(int k) => Color.FromArgb(255, (k >> 16) & 255, (k >> 8) & 255, k & 255);

        // 1) The Linux pick.
        Color mostCommon = FromKey(ordered[0].Key);
        Add(result, mostCommon, "Most common",
            "The single most frequent colour in the wallpaper — the exact colour the Linux paperWiz uses.");

        // Reason over the most frequent distinct colours so suggestions are genuinely present.
        var pool = ordered.Take(ProminentPool).Select(kv => FromKey(kv.Key)).ToList();

        // 2) Most saturated of the prominent colours (ties broken by frequency via stable sort).
        Color vibrant = pool.OrderByDescending(Chroma).First();
        Add(result, vibrant, "Most vibrant",
            "The most colourful (saturated) of the wallpaper's prominent colours.");

        // 3) Overall average of every pixel.
        Color average = Color.FromArgb(255, (int)(sumR / total), (int)(sumG / total), (int)(sumB / total));
        Add(result, average, "Average",
            "The overall average colour of the whole image.");

        // 4) Darkest / 5) lightest prominent colour.
        Add(result, pool.OrderBy(Luminance).First(), "Darkest",
            "The darkest prominent colour — a subtle, low-key border.");
        Add(result, pool.OrderByDescending(Luminance).First(), "Lightest",
            "The lightest prominent colour — a bright border.");

        // 6) A second, clearly different frequent colour.
        foreach (var kv in ordered.Skip(1))
        {
            Color c = FromKey(kv.Key);
            if (Distance(c, mostCommon) >= 45)
            {
                Add(result, c, "Also common", "Another of the wallpaper's most frequent colours.");
                break;
            }
        }

        return result;
    }

    /// <summary>Adds a candidate unless a near-identical colour is already suggested.</summary>
    private static void Add(List<ColorCandidate> list, Color c, string label, string reason)
    {
        foreach (var existing in list)
            if (Distance(existing.Color, c) < 28)
                return;
        list.Add(new ColorCandidate(c, label, reason));
    }

    private static int Chroma(Color c) =>
        Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B));

    private static double Luminance(Color c) => 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;

    private static double Distance(Color a, Color b)
    {
        int dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
        return Math.Sqrt(dr * dr + dg * dg + db * db);
    }

    private static Bitmap LoadSample(string imagePath)
    {
        using var source = new Bitmap(imagePath);
        int w = source.Width, h = source.Height;
        double scale = Math.Min(1.0, (double)SampleSize / Math.Max(w, h)); // shrink only, like `>`
        int tw = Math.Max(1, (int)Math.Round(w * scale));
        int th = Math.Max(1, (int)Math.Round(h * scale));

        var sample = new Bitmap(tw, th, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(sample))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, 0, 0, tw, th);
        }
        return sample;
    }
}

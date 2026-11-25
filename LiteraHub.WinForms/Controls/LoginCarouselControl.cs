using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>
/// Offline, theme-adaptive login illustration carousel. Uses only the existing
/// monochrome Brand PNG assets. Never changes the original image files.
/// </summary>
public sealed class LoginCarouselControl : Control
{
    private const int SlideCount = 3;
    private readonly string[] _headings =
    {
        "Organize your library",
        "Discover every story",
        "One connected library"
    };
    private readonly string[] _descriptions =
    {
        "Books, authors, categories and students — beautifully organized.",
        "Explore records and keep your catalog easy to navigate.",
        "A simpler way to manage your campus library."
    };
    private readonly Image?[] _source = new Image?[SlideCount];
    private readonly Bitmap?[] _tinted = new Bitmap?[SlideCount];
    private readonly System.Windows.Forms.Timer _rotate = new() { Interval = 6800 };
    private readonly System.Windows.Forms.Timer _transition = new() { Interval = 32 };
    private int _active;
    private int _previous;
    private float _progress = 1f;
    private bool _disposed;

    public LoginCarouselControl()
    {
        DoubleBuffered = true;
        TabStop = true;
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleName = "LiteraHub introduction, three slides. Left and right arrow keys change slides.";
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        LoadAssets();
        BuildTintedAssets();
        _rotate.Tick += (_, _) =>
        {
            if (FindForm() is Form form && form.Visible && form.ContainsFocus)
                ChangeSlide((_active + 1) % SlideCount);
        };
        _transition.Tick += (_, _) =>
        {
            _progress = Math.Min(1f, _progress + 0.15f);
            if (_progress >= 1f) _transition.Stop();
            Invalidate();
        };
        Theme.Changed += OnThemeChanged;
        VisibleChanged += (_, _) => UpdateRotation();
        HandleCreated += (_, _) => UpdateRotation();
        _rotate.Start();
    }

    private void LoadAssets()
    {
        for (int i = 0; i < SlideCount; i++)
        {
            string file = $"Slide{i + 1}.png";
            string brand = Path.Combine(AppContext.BaseDirectory, "Assets", "Brand", file);
            string legacy = Path.Combine(AppContext.BaseDirectory, "Assets", "Login", file);
            string path = File.Exists(brand) ? brand : legacy;
            if (!File.Exists(path)) continue;
            try
            {
                // Clone the decoded file to release the disk handle immediately.
                using var decoded = Image.FromFile(path);
                using var loaded = new Bitmap(decoded);
                _source[i] = TrimTransparentMargins(loaded);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException)
            {
                // Decoration must never prevent staff from signing in.
                System.Diagnostics.Debug.WriteLine($"Unable to load carousel slide {i + 1}: {ex.Message}");
            }
        }
    }

    // Many generated transparent PNGs have broad invisible margins. Trim only
    // transparent pixels so the visible artwork fills the carousel image region.
    private static Bitmap TrimTransparentMargins(Bitmap original)
    {
        using var argb = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(argb))
            g.DrawImage(original, 0, 0, original.Width, original.Height);

        Rectangle whole = new(0, 0, argb.Width, argb.Height);
        var data = argb.LockBits(whole, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        Rectangle crop;
        try
        {
            int stride = Math.Abs(data.Stride);
            var raw = new byte[stride * argb.Height];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, raw, 0, raw.Length);
            int left = argb.Width, top = argb.Height, right = -1, bottom = -1;
            for (int y = 0; y < argb.Height; y++)
            {
                int row = data.Stride >= 0 ? y * stride : (argb.Height - 1 - y) * stride;
                for (int x = 0; x < argb.Width; x++)
                {
                    if (raw[row + x * 4 + 3] <= 10) continue;
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                    top = Math.Min(top, y);
                    bottom = Math.Max(bottom, y);
                }
            }
            if (right < left || bottom < top)
                return new Bitmap(original);
            int pad = Math.Max(5, Math.Min(argb.Width, argb.Height) / 70);
            int x1 = Math.Max(0, left - pad), y1 = Math.Max(0, top - pad);
            int x2 = Math.Min(argb.Width, right + pad + 1);
            int y2 = Math.Min(argb.Height, bottom + pad + 1);
            crop = new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }
        finally
        {
            argb.UnlockBits(data);
        }
        return argb.Clone(crop, PixelFormat.Format32bppArgb);
    }

    private void BuildTintedAssets()
    {
        for (int i = 0; i < SlideCount; i++)
        {
            _tinted[i]?.Dispose();
            _tinted[i] = _source[i] is Image source ? Tint(source) : null;
        }
    }

    private static Bitmap? Tint(Image source)
    {
        var c = Theme.Colors.Accent;
        try
        {
            var output = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(output);
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            using var attributes = new ImageAttributes();
            // White maps to the current accent; gray preserves its shading and alpha.
            var matrix = new ColorMatrix(new[]
            {
                new[] { c.R / 255f, 0f, 0f, 0f, 0f },
                new[] { 0f, c.G / 255f, 0f, 0f, 0f },
                new[] { 0f, 0f, c.B / 255f, 0f, 0f },
                new[] { 0f, 0f, 0f, 1f, 0f },
                new[] { 0f, 0f, 0f, 0f, 1f }
            });
            attributes.SetColorMatrix(matrix);
            g.DrawImage(source, new Rectangle(0, 0, output.Width, output.Height),
                0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            return output;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Carousel tint unavailable: " + ex.Message);
            return null;
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_disposed || IsDisposed) return;
        BuildTintedAssets();
        Invalidate();
    }

    private void UpdateRotation()
    {
        if (_disposed) return;
        if (Visible) _rotate.Start();
        else { _rotate.Stop(); _transition.Stop(); _progress = 1f; }
    }

    private void ChangeSlide(int index)
    {
        index = (index + SlideCount) % SlideCount;
        if (index == _active) return;
        _previous = _active;
        _active = index;
        _progress = 0f;
        _transition.Start();
        _rotate.Stop();
        _rotate.Start();
        Invalidate();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Left) { ChangeSlide(_active - 1); e.Handled = true; }
        if (e.KeyCode == Keys.Right) { ChangeSlide(_active + 1); e.Handled = true; }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        int dotsY = Math.Max(0, Height - 30);
        for (int i = 0; i < SlideCount; i++)
        {
            int x = Width / 2 + (i - 1) * 25;
            if (Math.Abs(e.X - x) < 13 && Math.Abs(e.Y - dotsY) < 14)
            {
                ChangeSlide(i);
                Focus();
                break;
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var c = Theme.Colors;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(c.SurfaceAlt);
        if (Width < 140 || Height < 180) return;

        using (var glow = new LinearGradientBrush(ClientRectangle, c.AccentSoft, c.SurfaceAlt, 90f))
            g.FillRectangle(glow, ClientRectangle);

        // Restrained sizing: artwork occupies the upper 70% without pressing
        // against the captions or the edges. Caption and dots have their own
        // bottom safe area, including 28px below the indicators.
        const int sidePadding = 38;
        int artworkTop = 36;
        int artworkBottom = Math.Max(artworkTop + 120, Height - 160);
        var imageArea = new Rectangle(
            sidePadding, artworkTop,
            Math.Max(1, Width - sidePadding * 2),
            Math.Max(1, artworkBottom - artworkTop));
        DrawSlideImage(g, _tinted[_previous], imageArea, 1f - _progress);
        DrawSlideImage(g, _tinted[_active], imageArea, _progress);
        if (_tinted[_active] is null)
        {
            using var fallbackPen = new Pen(c.Accent, 3f);
            int iconSize = Math.Min(110, imageArea.Height - 12);
            int centerX = Width / 2, centerY = imageArea.Top + imageArea.Height / 2;
            if (iconSize > 20)
            {
                g.DrawRectangle(fallbackPen, centerX - iconSize / 2, centerY - iconSize / 2, iconSize, iconSize);
                g.DrawLine(fallbackPen, centerX, centerY - iconSize / 2, centerX, centerY + iconSize / 2);
            }
        }

        int titleY = Height - 133;
        using var headline = new Font("Segoe UI", Width < 430 ? 17f : 20f, FontStyle.Bold);
        TextRenderer.DrawText(g, _headings[_active], headline,
            new Rectangle(28, titleY, Width - 56, 40), c.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, _descriptions[_active], Theme.Body,
            new Rectangle(34, titleY + 41, Width - 68, 40), c.TextMuted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak);

        int dotY = Height - 30;
        for (int i = 0; i < SlideCount; i++)
        {
            using var dot = new SolidBrush(i == _active ? c.Accent : c.Border);
            int r = i == _active ? 6 : 4;
            int x = Width / 2 + (i - 1) * 25;
            g.FillEllipse(dot, x - r, dotY - r, r * 2, r * 2);
        }
    }

    private static void DrawSlideImage(Graphics g, Image? image, Rectangle region, float opacity)
    {
        if (image is null || opacity <= 0f || region.Width < 1 || region.Height < 1) return;
        float scale = Math.Min((float)region.Width / image.Width, (float)region.Height / image.Height);
        int w = Math.Max(1, (int)(image.Width * scale));
        int h = Math.Max(1, (int)(image.Height * scale));
        var bounds = new Rectangle(region.X + (region.Width - w) / 2,
            region.Y + (region.Height - h) / 2, w, h);
        using var attrs = new ImageAttributes();
        var cm = new ColorMatrix();
        cm.Matrix33 = Math.Clamp(opacity, 0f, 1f);
        attrs.SetColorMatrix(cm);
        g.DrawImage(image, bounds, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attrs);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposed = true;
            Theme.Changed -= OnThemeChanged;
            _rotate.Stop();
            _transition.Stop();
            _rotate.Dispose();
            _transition.Dispose();
            for (int i = 0; i < SlideCount; i++)
            {
                _tinted[i]?.Dispose();
                _source[i]?.Dispose();
            }
        }
        base.Dispose(disposing);
    }
}

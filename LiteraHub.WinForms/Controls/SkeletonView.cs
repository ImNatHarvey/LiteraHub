using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

public enum SkeletonStyle
{
    Rows,
    Cards
}

// Painted placeholder shapes shown while data loads. No child controls, so it stays cheap.
public sealed class SkeletonView : ThemedUserControl
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 60 };
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private SkeletonStyle _style = SkeletonStyle.Rows;

    public SkeletonView()
    {
        _timer.Tick += (_, _) => Invalidate();
        VisibleChanged += (_, _) => UpdateTimer();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SkeletonStyle Style
    {
        get => _style;
        set
        {
            if (_style == value) return;
            _style = value;
            Invalidate();
        }
    }

    private static bool AnimationOn => SystemInformation.UIEffectsEnabled;

    private void UpdateTimer() => _timer.Enabled = Visible && IsHandleCreated && AnimationOn;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateTimer();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _timer.Stop();
        base.OnHandleDestroyed(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var c = Theme.Colors;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var t = AnimationOn
            ? (float)((Math.Sin(_clock.Elapsed.TotalSeconds * 2 * Math.PI / 1.4) + 1) / 2)
            : 0.5f;
        using var brush = new SolidBrush(Gfx.Mix(c.SurfaceAlt, c.Border, t));

        var scale = DeviceDpi / 96f;
        if (_style == SkeletonStyle.Rows) PaintRows(g, brush, scale);
        else PaintCards(g, brush, scale);
    }

    private void PaintRows(Graphics g, Brush brush, float s)
    {
        var pad = Space.XL * s;
        var width = Width - pad * 2;
        if (width <= 0) return;

        var radius = Radius.Small * s;
        var y = pad;

        FillRounded(g, brush, new RectangleF(pad, y, width * 0.6f, 20 * s), radius);
        y += 20 * s + Space.L * s;

        var rowHeight = 44 * s;
        var gap = Space.S * s;
        while (y < Height)
        {
            FillRounded(g, brush, new RectangleF(pad, y, width, rowHeight), radius);
            y += rowHeight + gap;
        }
    }

    private void PaintCards(Graphics g, Brush brush, float s)
    {
        var pad = Space.XL * s;
        var gap = Space.L * s;
        var cardWidth = 150 * s;
        var coverHeight = 200 * s;
        var textHeight = 12 * s;
        var radius = Radius.Small * s;

        var perRow = Math.Max(1, (int)((Width - pad * 2 + gap) / (cardWidth + gap)));
        var cardHeight = coverHeight + gap / 2 + textHeight * 2 + gap / 2;

        var y = pad;
        while (y < Height)
        {
            for (var i = 0; i < perRow; i++)
            {
                var x = pad + i * (cardWidth + gap);
                FillRounded(g, brush, new RectangleF(x, y, cardWidth, coverHeight), radius);
                FillRounded(g, brush, new RectangleF(x, y + coverHeight + gap / 2, cardWidth * 0.85f, textHeight), radius / 2);
                FillRounded(g, brush, new RectangleF(x, y + coverHeight + gap / 2 + textHeight + 6 * s, cardWidth * 0.5f, textHeight), radius / 2);
            }

            y += cardHeight + gap;
        }
    }

    private static void FillRounded(Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using var path = Gfx.RoundedRect(rect, radius);
        g.FillPath(brush, path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
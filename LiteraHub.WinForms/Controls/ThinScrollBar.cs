using System.ComponentModel;
using System.Drawing.Drawing2D;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

// Thin custom vertical scrollbar. Value ranges from 0 to (Maximum - LargeChange), like a standard scrollbar.
public sealed class ThinScrollBar : Control
{
    private int _maximum;
    private int _largeChange = 1;
    private int _value;
    private bool _hover;
    private bool _dragging;
    private int _dragOffset;

    public event EventHandler? ValueChanged;

    public ThinScrollBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        TabStop = false;
        Theme.Changed += OnThemeChanged;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value => _value;

    private int MaxValue => Math.Max(0, _maximum - _largeChange);

    // maximum = total content height, largeChange = visible height.
    public void SetRange(int maximum, int largeChange)
    {
        _maximum = Math.Max(0, maximum);
        _largeChange = Math.Max(1, largeChange);
        var clamped = Math.Clamp(_value, 0, MaxValue);
        var changed = clamped != _value;
        _value = clamped;
        Invalidate();
        if (changed) ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetValue(int value)
    {
        var clamped = Math.Clamp(value, 0, MaxValue);
        if (clamped == _value) return;
        _value = clamped;
        Invalidate();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ScrollBy(int delta) => SetValue(_value + delta);

    private Rectangle ThumbRect()
    {
        if (_maximum <= _largeChange || Height <= 0) return Rectangle.Empty;

        var scale = DeviceDpi / 96f;
        var minThumb = (int)(24 * scale);
        var thumbHeight = Math.Max(minThumb, (int)((long)Height * _largeChange / _maximum));
        thumbHeight = Math.Min(thumbHeight, Height);
        var range = Height - thumbHeight;
        var top = MaxValue == 0 ? 0 : (int)((long)range * _value / MaxValue);
        var thumbWidth = Math.Max(4, (int)(6 * scale));
        return new Rectangle((Width - thumbWidth) / 2, top, thumbWidth, thumbHeight);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = Theme.Colors;
        e.Graphics.Clear(c.Background);

        var thumb = ThumbRect();
        if (thumb.IsEmpty) return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var color = _dragging || _hover ? c.TextMuted : Mix(c.TextMuted, c.Background, 0.5f);
        using var brush = new SolidBrush(color);
        using var path = RoundedRectangle(thumb);
        e.Graphics.FillPath(brush, path);
    }

    private static Color Mix(Color a, Color b, float t) =>
        Color.FromArgb(
            (int)(a.R * t + b.R * (1 - t)),
            (int)(a.G * t + b.G * (1 - t)),
            (int)(a.B * t + b.B * (1 - t)));

    private static GraphicsPath RoundedRectangle(Rectangle r)
    {
        var d = Math.Min(r.Width, r.Height);
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        var thumb = ThumbRect();
        if (thumb.IsEmpty) return;

        if (e.Y >= thumb.Top && e.Y <= thumb.Bottom)
        {
            _dragging = true;
            _dragOffset = e.Y - thumb.Top;
            Capture = true;
        }
        else
        {
            ScrollBy(e.Y < thumb.Top ? -_largeChange : _largeChange);
        }

        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var thumb = ThumbRect();

        if (_dragging && !thumb.IsEmpty)
        {
            var range = Height - thumb.Height;
            if (range > 0)
            {
                var top = Math.Clamp(e.Y - _dragOffset, 0, range);
                SetValue((int)((long)top * MaxValue / range));
            }

            return;
        }

        var hover = !thumb.IsEmpty && e.Y >= thumb.Top && e.Y <= thumb.Bottom;
        if (hover != _hover)
        {
            _hover = hover;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
        Capture = false;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hover)
        {
            _hover = false;
            Invalidate();
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();

    protected override void Dispose(bool disposing)
    {
        if (disposing) Theme.Changed -= OnThemeChanged;
        base.Dispose(disposing);
    }
}

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

public sealed class GridScrollBar : Control
{
    private int _contentSize = 1;
    private int _viewportSize = 1;
    private int _value;
    private bool _dragging;
    private bool _hover;
    private int _dragOffset;

    private Orientation _orientation = Orientation.Vertical;

    public event EventHandler? ValueChanged;

    [DesignerSerializationVisibility(
        DesignerSerializationVisibility.Hidden)]
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(
        DesignerSerializationVisibility.Hidden)]
    public int Value => _value;

    private int MaximumValue =>
        Math.Max(0, _contentSize - _viewportSize);

    public GridScrollBar()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        TabStop = false;
        Cursor = Cursors.Hand;

        Theme.Changed += HandleThemeChanged;
    }

    public void SetRange(int contentSize, int viewportSize)
    {
        _contentSize = Math.Max(1, contentSize);
        _viewportSize = Math.Max(1, viewportSize);

        int clamped = Math.Clamp(
            _value, 0, MaximumValue);

        bool changed = clamped != _value;
        _value = clamped;

        Invalidate();

        if (changed)
            ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetValue(int value)
    {
        int next = Math.Clamp(
            value, 0, MaximumValue);

        if (next == _value)
            return;

        _value = next;
        Invalidate();

        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ScrollBy(int amount)
    {
        SetValue(_value + amount);
    }

    private int TrackLength =>
        Orientation == Orientation.Vertical
            ? Height
            : Width;

    private Rectangle GetThumb()
    {
        int length = TrackLength;

        if (length <= 0 ||
            _contentSize <= _viewportSize)
            return Rectangle.Empty;

        int minThumb = Math.Max(
            20, (int)Math.Round(20f * DeviceDpi / 96f));

        int thumbLength = Math.Clamp(
            (int)((long)length * _viewportSize /
                  _contentSize),
            Math.Min(minThumb, length),
            length);

        int travel = length - thumbLength;

        int position = MaximumValue == 0
            ? 0
            : (int)((long)travel * _value /
                    MaximumValue);

        int thickness = Math.Max(
            5, (int)Math.Round(6f * DeviceDpi / 96f));

        if (Orientation == Orientation.Vertical)
        {
            return new Rectangle(
                (Width - thickness) / 2,
                position,
                thickness,
                thumbLength);
        }

        return new Rectangle(
            position,
            (Height - thickness) / 2,
            thumbLength,
            thickness);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var colors = Theme.Colors;

        e.Graphics.Clear(colors.SurfaceAlt);

        Rectangle thumb = GetThumb();

        if (thumb.IsEmpty)
            return;

        Color thumbColor = _dragging || _hover
            ? colors.TextMuted
            : Color.FromArgb(
                (colors.TextMuted.R +
                 colors.SurfaceAlt.R) / 2,
                (colors.TextMuted.G +
                 colors.SurfaceAlt.G) / 2,
                (colors.TextMuted.B +
                 colors.SurfaceAlt.B) / 2);

        e.Graphics.SmoothingMode =
            SmoothingMode.AntiAlias;

        using var brush = new SolidBrush(thumbColor);
        using var path = new GraphicsPath();

        int diameter = Math.Min(
            thumb.Width, thumb.Height);

        if (diameter < 2)
        {
            e.Graphics.FillRectangle(brush, thumb);
            return;
        }

        path.AddArc(
            thumb.Left,
            thumb.Top,
            diameter,
            diameter,
            180,
            90);

        path.AddArc(
            thumb.Right - diameter,
            thumb.Top,
            diameter,
            diameter,
            270,
            90);

        path.AddArc(
            thumb.Right - diameter,
            thumb.Bottom - diameter,
            diameter,
            diameter,
            0,
            90);

        path.AddArc(
            thumb.Left,
            thumb.Bottom - diameter,
            diameter,
            diameter,
            90,
            90);

        path.CloseFigure();
        e.Graphics.FillPath(brush, path);
    }

    private int GetPosition(Point point)
    {
        return Orientation == Orientation.Vertical
            ? point.Y
            : point.X;
    }

    private int GetThumbStart(Rectangle thumb)
    {
        return Orientation == Orientation.Vertical
            ? thumb.Top
            : thumb.Left;
    }

    private int GetThumbLength(Rectangle thumb)
    {
        return Orientation == Orientation.Vertical
            ? thumb.Height
            : thumb.Width;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButtons.Left)
            return;

        Rectangle thumb = GetThumb();

        if (thumb.IsEmpty)
            return;

        int position = GetPosition(e.Location);
        int thumbStart = GetThumbStart(thumb);
        int thumbEnd =
            thumbStart + GetThumbLength(thumb);

        if (position >= thumbStart &&
            position <= thumbEnd)
        {
            _dragging = true;
            _dragOffset = position - thumbStart;
            Capture = true;
        }
        else
        {
            ScrollBy(position < thumbStart
                ? -_viewportSize
                : _viewportSize);
        }

        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        Rectangle thumb = GetThumb();

        if (thumb.IsEmpty)
            return;

        int position = GetPosition(e.Location);
        int thumbStart = GetThumbStart(thumb);
        int thumbLength = GetThumbLength(thumb);

        if (_dragging)
        {
            int travel = TrackLength - thumbLength;

            if (travel > 0)
            {
                int newStart = Math.Clamp(
                    position - _dragOffset,
                    0,
                    travel);

                int newValue = (int)Math.Round(
                    (double)newStart *
                    MaximumValue / travel);

                SetValue(newValue);
            }

            return;
        }

        bool hovered =
            position >= thumbStart &&
            position <= thumbStart + thumbLength;

        if (_hover != hovered)
        {
            _hover = hovered;
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

        if (!_dragging)
        {
            _hover = false;
            Invalidate();
        }
    }

    private void HandleThemeChanged(
        object? sender, EventArgs e)
    {
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Theme.Changed -= HandleThemeChanged;

        base.Dispose(disposing);
    }
}

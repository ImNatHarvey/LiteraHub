using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>
/// Reusable desktop details header: 56px title/back row and 42px subtitle row.
/// Matches the accepted Author/Category/Book details header proportions.
/// </summary>
public sealed class DetailsPageHeader : TableLayoutPanel
{
    private readonly Label _heading = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = Padding.Empty
    };

    private readonly Label _subtitle = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = Padding.Empty
    };

    private readonly DetailsBackButton _back = new()
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(8, 4, 2, 4)
    };

    public event EventHandler? BackRequested;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string HeadingText
    {
        get => _heading.Text;
        set => _heading.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SubtitleText
    {
        get => _subtitle.Text;
        set => _subtitle.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool BackEnabled
    {
        get => _back.Enabled;
        set => _back.Enabled = value;
    }

    private bool _compactMode;

    // Book Editor uses a compact Back column and title on narrow screens.
    // The default remains unchanged for Author, Category and Student Details.
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool CompactMode
    {
        get => _compactMode;
        set
        {
            if (_compactMode == value) return;
            _compactMode = value;
            ColumnStyles[1].Width = value ? 88 : 100;
            _heading.Font = value ? Theme.BodyStrong : Theme.Title;
            PerformLayout();
        }
    }

    public DetailsPageHeader()
    {
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        ColumnCount = 2;
        RowCount = 2;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(_heading, 0, 0);
        Controls.Add(_back, 1, 0);
        Controls.Add(_subtitle, 0, 1);
        SetColumnSpan(_subtitle, 2);
        _back.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);
        Theme.Changed += OnThemeChanged;
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        if (IsDisposed) return;
        var c = Theme.Colors;
        BackColor = c.Background;
        _heading.BackColor = c.Background;
        _heading.ForeColor = c.Text;
        _heading.Font = _compactMode ? Theme.BodyStrong : Theme.Title;
        _subtitle.BackColor = c.Background;
        _subtitle.ForeColor = c.TextMuted;
        _subtitle.Font = Theme.Body;
        _back.RefreshTheme();
        Invalidate(true);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (!IsDisposed) ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Theme.Changed -= OnThemeChanged;
        base.Dispose(disposing);
    }

    // Draws the same theme-aware Back control used in accepted Author Details.
    public sealed class DetailsBackButton : Control
    {
        private bool _hovered;

        public DetailsBackButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.Selectable, true);
            Cursor = Cursors.Hand;
            TabStop = true;
            Text = "Back";
            AccessibleName = "Back";
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            AccessibleName = Text;
            Invalidate();
        }

        public void RefreshTheme()
        {
            BackColor = Theme.Colors.Surface;
            ForeColor = Theme.Colors.Text;
            Font = Theme.Body;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode is Keys.Enter or Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var c = Theme.Colors;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var background = new SolidBrush(_hovered && Enabled ? c.SurfaceAlt : c.Surface);
            g.FillRectangle(background, ClientRectangle);
            using var border = new Pen(c.Border);
            g.DrawRectangle(border, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
            int textWidth = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
            int iconX = Math.Max(10, (Width - textWidth - 26) / 2 + 7);
            int iconY = Height / 2;
            using var pen = new Pen(Enabled ? c.Accent : c.TextMuted, 2.2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            g.DrawLines(pen, new[]
            {
                new Point(iconX + 4, iconY - 6),
                new Point(iconX - 2, iconY),
                new Point(iconX + 4, iconY + 6)
            });
            TextRenderer.DrawText(g, Text, Font,
                new Rectangle(iconX + 13, 0, Math.Max(0, Width - iconX - 16), Height),
                Enabled ? c.Text : c.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            if (Focused && ShowFocusCues)
                ControlPaint.DrawFocusRectangle(g, new Rectangle(3, 3,
                    Math.Max(0, Width - 6), Math.Max(0, Height - 6)));
        }
    }
}

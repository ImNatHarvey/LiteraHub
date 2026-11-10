
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

public sealed class BookEditorPage : ThemedUserControl
{
    private readonly Book? _existing;

    private readonly TableLayoutPanel _layout = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 3,
        Margin = Padding.Empty
    };

    private readonly TableLayoutPanel _header = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Margin = Padding.Empty
    };

    private readonly Label _heading = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = Padding.Empty
    };

    private readonly BackButton _back = new()
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(8, 4, 2, 4)
    };

    private readonly Label _description = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly Panel _preview = new()
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(20)
    };

    private readonly Label _previewText = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter
    };

    public event EventHandler? BackRequested;

    public BookEditorPage(Book? existing = null)
    {
        _existing = existing;

        Dock = DockStyle.Fill;
        Padding = new Padding(Space.L);

        _heading.Text = existing is null
            ? "Add Book"
            : "Edit Book";

        _description.Text = existing is null
            ? "Create a new book record."
            : $"Editing: {existing.Title}";

        _previewText.Text =
            "Book Editor page foundation\n\n" +
            "The full editor fields, ISBN lookup, " +
            "cover preview, author selection, " +
            "category, condition, and inventory " +
            "controls will be added next.\n\n" +
            "Saving is not enabled in this checkpoint.";

        BuildLayout();

        _back.Click += (_, _) =>
            BackRequested?.Invoke(this, EventArgs.Empty);

        Resize += (_, _) => UpdateResponsiveLayout();

        ApplyTheme();
    }

    private void BuildLayout()
    {
        _layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 56));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 42));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        // Heading expands on the left.
        _header.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        // Back button stays anchored to the right.
        _header.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 100));

        _header.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _header.Controls.Add(_heading, 0, 0);
        _header.Controls.Add(_back, 1, 0);

        _preview.Controls.Add(_previewText);

        _layout.Controls.Add(_header, 0, 0);
        _layout.Controls.Add(_description, 0, 1);
        _layout.Controls.Add(_preview, 0, 2);

        Controls.Add(_layout);
    }

    private void UpdateResponsiveLayout()
    {
        bool compact = Width < 480;

        _header.ColumnStyles[1].Width =
            compact ? 88 : 100;

        _heading.Font = compact
            ? Theme.BodyStrong
            : Theme.Title;
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();

        var c = Theme.Colors;

        BackColor = c.Background;
        ForeColor = c.Text;

        _layout.BackColor = c.Background;
        _header.BackColor = c.Background;

        _heading.BackColor = c.Background;
        _heading.ForeColor = c.Text;
        _heading.Font = Theme.Title;

        _description.BackColor = c.Background;
        _description.ForeColor = c.TextMuted;
        _description.Font = Theme.Body;

        _preview.BackColor = c.Surface;
        _previewText.BackColor = c.Surface;
        _previewText.ForeColor = c.TextMuted;
        _previewText.Font = Theme.Body;

        _back.ApplyCurrentTheme();

        UpdateResponsiveLayout();
    }

    private sealed class BackButton : Control
    {
        private bool _hovered;

        public BackButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true);

            Text = "Back";
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleName = "Back to Books";
        }

        public void ApplyCurrentTheme()
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

            if (e.KeyCode == Keys.Enter ||
                e.KeyCode == Keys.Space)
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

            using var background = new SolidBrush(
                _hovered ? c.SurfaceAlt : c.Surface);

            g.FillRectangle(
                background,
                ClientRectangle);

            using var border = new Pen(c.Border);

            g.DrawRectangle(
                border,
                0,
                0,
                Math.Max(0, Width - 1),
                Math.Max(0, Height - 1));

            int iconX = 18;
            int iconY = Height / 2;

            using var iconPen = new Pen(
                c.Accent,
                2.2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            g.DrawLines(
                iconPen,
                new[]
                {
                    new Point(iconX + 4, iconY - 6),
                    new Point(iconX - 2, iconY),
                    new Point(iconX + 4, iconY + 6)
                });

            var textBounds = new Rectangle(
                31,
                0,
                Math.Max(0, Width - 34),
                Height);

            TextRenderer.DrawText(
                g,
                "Back",
                Font,
                textBounds,
                c.Text,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);

            if (Focused && ShowFocusCues)
            {
                ControlPaint.DrawFocusRectangle(
                    g,
                    new Rectangle(
                        3,
                        3,
                        Math.Max(0, Width - 6),
                        Math.Max(0, Height - 6)));
            }
        }
    }
}

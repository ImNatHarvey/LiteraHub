using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>Combined, themed catalog status filter, extra filter, and sort menu.</summary>
public sealed class ThemedCatalogFilterSort : Button
{
    private Form? _popup;
    private int _statusIndex;
    private int _sortIndex;
    private int _extraIndex;
    private readonly string _extraTitle;
    private readonly string[] _extraOptions;
    public int ExtraIndex => _extraIndex;

    public int StatusIndex => _statusIndex; // All, Active, Inactive
    public int SortIndex => _sortIndex;     // A-Z, Z-A
    public event EventHandler? SelectionChanged;

    public ThemedCatalogFilterSort(string extraTitle, params string[] extraOptions)
    {
        _extraTitle = extraTitle;
        _extraOptions = extraOptions;
        FlatStyle = FlatStyle.Flat;
        Cursor = Cursors.Hand;
        TextAlign = ContentAlignment.MiddleCenter;
        TabStop = true;
        Click += (_, _) => OpenMenu();
        Theme.Changed += ThemeChanged;
        ApplyTheme();
    }

    private void ThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed) return;
        _popup?.Close();
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        var c = Theme.Colors;
        BackColor = c.Surface;
        ForeColor = c.Text;
        Font = Theme.Body;
        FlatAppearance.BorderSize = 1;
        FlatAppearance.BorderColor = c.Border;
        FlatAppearance.MouseOverBackColor = c.SurfaceAlt;
        FlatAppearance.MouseDownBackColor = c.AccentSoft;
        Text = "Filter && Sort  ▾";
        AccessibleName = "Filter and sort categories";
        Invalidate();
    }

    private void OpenMenu()
    {
        if (_popup != null || !Enabled) return;
        var c = Theme.Colors;
        var popup = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            BackColor = c.Border,
            Padding = new Padding(1),
            Size = new Size(220, 292 + (_extraOptions.Length > 0 ? 26 + 39 * _extraOptions.Length : 0)),
            TopMost = true
        };
        _popup = popup;
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8 + (_extraOptions.Length > 0 ? 1 + _extraOptions.Length : 0),
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = c.Surface
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < panel.RowCount; i++)
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute,
                i == 0 || i == 4 || (_extraOptions.Length > 0 && i == 7) ? 26 : 39));

        AddHeading(panel, 0, "STATUS", c);
        AddOption(panel, 1, "All", 0, true, "all", c);
        AddOption(panel, 2, "Active", 1, true, "active", c);
        AddOption(panel, 3, "Inactive", 2, true, "inactive", c);
        AddHeading(panel, 4, "SORT BY NAME", c);
        AddOption(panel, 5, "A–Z", 0, false, "az", c);
        AddOption(panel, 6, "Z–A", 1, false, "za", c);
        int resetRow = 7;
        if (_extraOptions.Length > 0)
        {
            AddHeading(panel, 7, _extraTitle, c);
            for (int i = 0; i < _extraOptions.Length; i++)
                AddExtraOption(panel, 8 + i, _extraOptions[i], i, c);
            resetRow = 8 + _extraOptions.Length;
        }
        AddOption(panel, resetRow, "Reset", -1, false, "reset", c);
        popup.Controls.Add(panel);

        Point origin = PointToScreen(new Point(0, Height));
        Rectangle area = Screen.FromControl(this).WorkingArea;
        int x = Math.Max(area.Left, Math.Min(origin.X, area.Right - popup.Width));
        int y = origin.Y + popup.Height <= area.Bottom
            ? origin.Y
            : PointToScreen(Point.Empty).Y - popup.Height;
        popup.Location = new Point(x, Math.Max(area.Top, y));
        popup.Deactivate += (_, _) => popup.Close();
        popup.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_popup, popup)) _popup = null;
            popup.Dispose();
        };
        popup.Show(FindForm());
        popup.Activate();
    }

    private static void AddHeading(TableLayoutPanel panel, int row, string caption, Palette c)
    {
        var heading = new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(13, 0, 0, 0),
            ForeColor = c.TextMuted,
            BackColor = c.Surface,
            Font = Theme.Caption
        };
        panel.Controls.Add(heading, 0, row);
    }

    private void AddOption(TableLayoutPanel panel, int row, string caption,
        int index, bool isStatus, string icon, Palette c)
    {
        bool selected = index >= 0 && (isStatus ? _statusIndex == index : _sortIndex == index);
        var item = new MenuOption(caption, icon, selected)
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Cursor = Cursors.Hand,
            BackColor = selected ? c.AccentSoft : c.Surface,
            ForeColor = c.Text,
            Font = Theme.Body
        };
        item.Click += (_, _) =>
        {
            int status = index == -1 ? 0 : isStatus ? index : _statusIndex;
            int sort = index == -1 ? 0 : isStatus ? _sortIndex : index;
            int extra = index == -1 ? 0 : _extraIndex;
            bool changed = status != _statusIndex || sort != _sortIndex || extra != _extraIndex;
            _statusIndex = status;
            _sortIndex = sort;
            _extraIndex = extra;
            _popup?.Close();
            if (changed) SelectionChanged?.Invoke(this, EventArgs.Empty);
        };
        panel.Controls.Add(item, 0, row);
    }

    private void AddExtraOption(TableLayoutPanel panel, int row, string caption, int index, Palette c)
    {
        var item = new MenuOption(caption, index == 0 ? "all" : "extra", _extraIndex == index)
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Cursor = Cursors.Hand,
            BackColor = c.Surface,
            ForeColor = c.Text,
            Font = Theme.Body
        };
        item.Click += (_, _) =>
        {
            bool changed = _extraIndex != index;
            _extraIndex = index;
            _popup?.Close();
            if (changed) SelectionChanged?.Invoke(this, EventArgs.Empty);
        };
        panel.Controls.Add(item, 0, row);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= ThemeChanged;
            _popup?.Close();
        }
        base.Dispose(disposing);
    }

    private sealed class MenuOption : Control
    {
        private readonly string _caption;
        private readonly string _icon;
        private readonly bool _selected;
        private bool _hover;

        public MenuOption(string caption, string icon, bool selected)
        {
            _caption = caption;
            _icon = icon;
            _selected = selected;
            DoubleBuffered = true;
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var c = Theme.Colors;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(_hover || _selected ? c.AccentSoft : c.Surface))
                g.FillRectangle(bg, ClientRectangle);
            TextRenderer.DrawText(g, _caption, Font,
                new Rectangle(16, 0, Width - 61, Height), c.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (_selected)
                TextRenderer.DrawText(g, "•", Font,
                    new Rectangle(Width - 53, 0, 14, Height), c.Text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            int x = Width - 24, y = Height / 2;
            Color ink = _icon == "active" ? c.PillGoodText
                : _icon == "inactive" ? c.Danger : c.TextMuted;
            using var pen = new Pen(ink, 1.8f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            switch (_icon)
            {
                case "active":
                    g.DrawEllipse(pen, x - 8, y - 8, 16, 16);
                    g.DrawLines(pen, new[] { new Point(x - 4, y), new Point(x - 1, y + 3), new Point(x + 5, y - 4) });
                    break;
                case "inactive":
                    g.DrawEllipse(pen, x - 8, y - 8, 16, 16);
                    g.DrawLine(pen, x - 4, y, x + 4, y);
                    break;
                case "all":
                    g.DrawEllipse(pen, x - 7, y - 7, 14, 14);
                    g.DrawLine(pen, x - 3, y, x + 3, y);
                    g.DrawLine(pen, x, y - 3, x, y + 3);
                    break;
                case "az":
                    g.DrawLine(pen, x + 5, y - 7, x + 5, y + 7);
                    g.DrawLines(pen, new[] { new Point(x + 2, y + 4), new Point(x + 5, y + 7), new Point(x + 8, y + 4) });
                    TextRenderer.DrawText(g, "A", Font, new Point(x - 11, y - 11), ink);
                    break;
                case "za":
                    g.DrawLine(pen, x + 5, y - 7, x + 5, y + 7);
                    g.DrawLines(pen, new[] { new Point(x + 2, y + 4), new Point(x + 5, y + 7), new Point(x + 8, y + 4) });
                    TextRenderer.DrawText(g, "Z", Font, new Point(x - 11, y - 11), ink);
                    break;
                default:
                    g.DrawArc(pen, x - 7, y - 7, 14, 14, 45, 280);
                    g.DrawLines(pen, new[] { new Point(x + 7, y - 5), new Point(x + 7, y + 1), new Point(x + 1, y - 1) });
                    break;
            }
        }
    }
}

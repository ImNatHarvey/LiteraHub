using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>Combined themed Books filter/sort popup. Filters are applied together.</summary>
public sealed class ThemedCatalogFilterSort : IconButton
{
    private Form? _popup;
    private readonly string _extraTitle;
    private readonly string[] _extraOptions;
    public int StatusIndex { get; private set; }
    public int ExtraIndex { get; private set; }
    public int SortIndex { get; private set; }

    // Only Authors enables ID sorting; Categories keeps its approved A–Z/Z–A menu.
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool EnableIdSort { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool EnableCategoryIdSort { get; set; }
    public event EventHandler? SelectionChanged;

    public void SetSort(int index)
    {
        if (index < 0 || index >= SortOptions.Length) return;
        SortIndex = index;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearFilters()
    {
        StatusIndex = 0;
        ExtraIndex = 0;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetStatusIndex(int index)
    {
        if (index is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(index));
        if (StatusIndex == index) return;
        StatusIndex = index;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearStatus() { StatusIndex = 0; SelectionChanged?.Invoke(this, EventArgs.Empty); }
    public void ClearExtra() { ExtraIndex = 0; SelectionChanged?.Invoke(this, EventArgs.Empty); }
    public string[] SortOptions => EnableIdSort
        ? new[] { "Name A–Z", "Name Z–A", "Author ID ↑", "Author ID ↓" }
        : EnableCategoryIdSort
            ? new[] { "Name A–Z", "Name Z–A", "Category ID ↑", "Category ID ↓" }
            : new[] { "Name A–Z", "Name Z–A" };


    public ThemedCatalogFilterSort(string extraTitle, params string[] extraOptions)
    {
        _extraTitle = extraTitle;
        _extraOptions = extraOptions;
        FlatStyle = FlatStyle.Flat;
        Cursor = Cursors.Hand;
        TextAlign = ContentAlignment.MiddleCenter;
        IconChar = IconChar.SlidersH;
        IconSize = 18;
        TextImageRelation = TextImageRelation.Overlay;
        ImageAlign = ContentAlignment.MiddleCenter;
        Click += (_, _) => OpenMenu();
        Theme.Changed += HandleThemeChanged;
        ApplyTheme();
    }

    private void HandleThemeChanged(object? sender, EventArgs e)
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
        Text = "";
        IconColor = c.Accent;
        AccessibleName = "Filter catalog";
        Invalidate();
    }

    private Form? _submenu;
    private bool _switchingSubmenu;

    private void OpenMenu()
    {
        if (_popup is not null || !Enabled) return;
        var c = Theme.Colors;
        var popup = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            BackColor = c.Border,
            Padding = new Padding(1),
            Size = new Size(246, 454), // Adjusted to content below
            TopMost = true
        };
        _popup = popup;
        var host = new ScrollHost { Dock = DockStyle.Fill };
        host.UseSurfaceBackground();
        var menu = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = c.Surface
        };
        const int menuWidth = 242;
        int rowHeight = 36;
        void Heading(string title)
        {
            menu.Controls.Add(new Label
            {
                Text = title,
                Width = menuWidth,
                Height = 28,
                Margin = Padding.Empty,
                Padding = new Padding(12, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = c.Surface,
                ForeColor = c.TextMuted,
                Font = Theme.Caption
            });
        }
        void Option(string title, string icon, bool selected, Action select,
            bool child = false, Action<Control>? openChild = null)
        {
            var option = new MenuOption(title, icon, selected)
            {
                Width = menuWidth,
                Height = rowHeight,
                Margin = Padding.Empty,
                Font = Theme.Body,
                Cursor = Cursors.Hand
            };
            option.Click += (_, _) =>
            {
                if (child)
                {
                    openChild?.Invoke(option);
                    return;
                }
                select();
                CloseMenus();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            };
            menu.Controls.Add(option);
        }
        void OpenSubmenu(Control anchor, string title,
            IEnumerable<(string Label, string Icon, bool Selected, Action Select)> entries)
        {
            // Switching between submenu anchors must not dismiss the parent.
            _switchingSubmenu = true;
            try { _submenu?.Close(); }
            finally { _switchingSubmenu = false; }
            var items = entries.ToList();
            var sub = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                BackColor = c.Border,
                Padding = new Padding(1),
                Width = 218,
                Height = Math.Min(
                    Screen.FromControl(this).WorkingArea.Height,
                    28 + items.Count * rowHeight + 2),
                TopMost = true
            };
            _submenu = sub;
            var subHost = new ScrollHost { Dock = DockStyle.Fill };
            subHost.UseSurfaceBackground();
            var content = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                Width = 214,
                Height = 28 + items.Count * rowHeight,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = c.Surface
            };
            content.Controls.Add(new Label
            {
                Text = title,
                Width = 214,
                Height = 28,
                Padding = new Padding(12, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = c.TextMuted,
                BackColor = c.Surface,
                Font = Theme.Caption,
                Margin = Padding.Empty
            });
            foreach (var entry in items)
            {
                var item = new MenuOption(entry.Label, entry.Icon, entry.Selected)
                {
                    Width = 214,
                    Height = rowHeight,
                    Font = Theme.Body,
                    Cursor = Cursors.Hand,
                    Margin = Padding.Empty
                };
                item.Click += (_, _) =>
                {
                    entry.Select();
                    CloseMenus();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                };
                content.Controls.Add(item);
            }
            subHost.SetContent(content);
            sub.Controls.Add(subHost);
            sub.Shown += (_, _) => subHost.RefreshMetrics();
            var screen = Screen.FromControl(this).WorkingArea;
            Point at = anchor.PointToScreen(Point.Empty);
            int left = popup.Right + sub.Width <= screen.Right
                ? popup.Right - 1 : popup.Left - sub.Width + 1;
            sub.Location = new Point(
                Math.Clamp(left, screen.Left, Math.Max(screen.Left, screen.Right - sub.Width)),
                Math.Clamp(at.Y, screen.Top, Math.Max(screen.Top, screen.Bottom - sub.Height)));
            sub.FormClosed += (_, _) =>
            {
                if (ReferenceEquals(_submenu, sub)) _submenu = null;
                sub.Dispose();
            };
            sub.Deactivate += (_, _) =>
            {
                if (_switchingSubmenu || sub.IsDisposed || popup.IsDisposed) return;
                // Focus is not settled during Deactivate. Defer the decision
                // until the new control/form has received activation.
                if (!popup.IsHandleCreated) return;
                popup.BeginInvoke((Action)(() =>
                {
                    if (_switchingSubmenu || popup.IsDisposed || sub.IsDisposed) return;
                    if (popup.ContainsFocus)
                    {
                        sub.Close();
                        return;
                    }
                    if (!sub.ContainsFocus) CloseMenus();
                }));
            };
            sub.Show(popup);
            sub.Activate();
        }

        Heading("RECORD STATUS");
        string[] statuses = { "All", "Active", "Inactive" };
        string[] statusIcons = { "all", "active", "inactive" };
        for (int i = 0; i < statuses.Length; i++)
        {
            int index = i;
            Option(statuses[i], statusIcons[i], StatusIndex == i, () => StatusIndex = index);
        }

        // Categories already has a dedicated STATUS section. Its legacy
        // constructor passes ("Status", "All", "Active", "Inactive") as
        // extra options; don't render that redundant second status filter.
        bool hasExtraFilter = _extraOptions.Length > 0 &&
            !string.Equals(_extraTitle, "Status", StringComparison.OrdinalIgnoreCase);
        if (hasExtraFilter)
        {
            string displayTitle = _extraTitle.Trim();
            if (displayTitle.Length > 0)
                displayTitle = char.ToUpperInvariant(displayTitle[0]) +
                    displayTitle.Substring(1).ToLowerInvariant();
            Heading(displayTitle.ToUpperInvariant());
            string label = ExtraIndex == 0 ? "Select " + displayTitle
                : displayTitle + ": " + _extraOptions[ExtraIndex];
            Option(label, "submenu", ExtraIndex > 0, () => { },
                child: true, openChild: anchor =>
                {
                    var choices = new List<(string, string, bool, Action)>();
                    for (int i = 0; i < _extraOptions.Length; i++)
                    {
                        int index = i;
                        string optionIcon = _extraOptions[i].Trim().ToLowerInvariant() switch
                        {
                            "male" => "male",
                            "female" => "female",
                            "other" => "other",
                            "all" => "all",
                            _ => i == 0 ? "all" : "category"
                        };
                        choices.Add((_extraOptions[i], optionIcon,
                            ExtraIndex == index, () => ExtraIndex = index));
                    }
                    OpenSubmenu(anchor, _extraTitle, choices);
                });
        }

        Heading("OPTIONS");
        Option("Reset Filters", "reset", false, () =>
        {
            StatusIndex = ExtraIndex = 0;
        });

        menu.Height = menu.Controls.Cast<Control>().Sum(x => x.Height);
        menu.Width = menuWidth;
        // Fit the full main menu without scrolling whenever screen space allows.
        // Keep ScrollHost only as a fallback on unusually short displays.
        Rectangle working = Screen.FromControl(this).WorkingArea;
        popup.Height = Math.Min(menu.Height + 2, working.Height);
        host.SetContent(menu);
        popup.Controls.Add(host);
        popup.Shown += (_, _) => host.RefreshMetrics();
        Point origin = PointToScreen(new Point(0, Height));
        Rectangle area = Screen.FromControl(this).WorkingArea;
        int x = Math.Max(area.Left, Math.Min(origin.X, area.Right - popup.Width));
        int y = origin.Y + popup.Height <= area.Bottom
            ? origin.Y : PointToScreen(Point.Empty).Y - popup.Height;
        popup.Location = new Point(x, Math.Max(area.Top, y));
        popup.Deactivate += (_, _) =>
        {
            if (_switchingSubmenu || popup.IsDisposed || !popup.IsHandleCreated) return;
            popup.BeginInvoke((Action)(() =>
            {
                if (_switchingSubmenu || popup.IsDisposed) return;
                if (!popup.ContainsFocus && (_submenu is null || !_submenu.ContainsFocus))
                    CloseMenus();
            }));
        };
        popup.FormClosed += (_, _) =>
        {
            _submenu?.Close();
            if (ReferenceEquals(_popup, popup)) _popup = null;
            popup.Dispose();
        };
        popup.Show(FindForm());
        popup.Activate();
    }

    private void CloseMenus()
    {
        _submenu?.Close();
        _popup?.Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= HandleThemeChanged;
            CloseMenus();
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
            if (e.KeyCode is Keys.Enter or Keys.Space)
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
            using (var brush = new SolidBrush(_hover || _selected ? c.AccentSoft : c.Surface))
                g.FillRectangle(brush, ClientRectangle);
            TextRenderer.DrawText(g, _caption, Font,
                new Rectangle(14, 0, Width - 67, Height), c.Text,
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
                case "inactive":
                    g.DrawEllipse(pen, x - 8, y - 8, 16, 16);
                    if (_icon == "active")
                        g.DrawLines(pen, new[] { new Point(x - 4, y), new Point(x - 1, y + 3), new Point(x + 5, y - 4) });
                    else g.DrawLine(pen, x - 4, y, x + 4, y);
                    break;
                case "all":
                    g.DrawEllipse(pen, x - 7, y - 7, 14, 14);
                    g.DrawLine(pen, x - 3, y, x + 3, y);
                    g.DrawLine(pen, x, y - 3, x, y + 3);
                    break;
                case "submenu":
                    g.DrawLines(pen, new[] { new Point(x - 3, y - 5), new Point(x + 2, y), new Point(x - 3, y + 5) });
                    break;
                case "male":
                    g.DrawEllipse(pen, x - 7, y - 2, 10, 10);
                    g.DrawLine(pen, x + 1, y - 6, x + 8, y - 13);
                    g.DrawLine(pen, x + 3, y - 13, x + 8, y - 13);
                    g.DrawLine(pen, x + 8, y - 13, x + 8, y - 8);
                    break;
                case "female":
                    g.DrawEllipse(pen, x - 5, y - 9, 10, 10);
                    g.DrawLine(pen, x, y + 1, x, y + 10);
                    g.DrawLine(pen, x - 4, y + 6, x + 4, y + 6);
                    break;
                case "other":
                    g.DrawEllipse(pen, x - 7, y - 7, 14, 14);
                    g.DrawLine(pen, x - 4, y + 4, x + 4, y - 4);
                    break;
                case "category":
                    g.DrawRectangle(pen, x - 7, y - 7, 14, 14);
                    g.DrawLine(pen, x - 4, y - 2, x + 4, y - 2);
                    g.DrawLine(pen, x - 4, y + 3, x + 2, y + 3);
                    break;
                case "az":
                case "za":
                    g.DrawLine(pen, x + 6, y - 7, x + 6, y + 7);
                    g.DrawLines(pen, new[] { new Point(x + 3, y + 4), new Point(x + 6, y + 7), new Point(x + 9, y + 4) });
                    TextRenderer.DrawText(g, _icon == "az" ? "A" : "Z", Font,
                        new Point(x - 11, y - 11), ink);
                    break;
                case "date":
                    g.DrawRectangle(pen, x - 7, y - 6, 14, 13);
                    g.DrawLine(pen, x - 7, y - 2, x + 7, y - 2);
                    g.DrawLine(pen, x - 3, y - 8, x - 3, y - 4);
                    g.DrawLine(pen, x + 3, y - 8, x + 3, y - 4);
                    break;
                case "copies":
                    g.DrawRectangle(pen, x - 8, y - 6, 12, 13);
                    g.DrawRectangle(pen, x - 4, y - 9, 12, 13);
                    break;
                default:
                    g.DrawArc(pen, x - 7, y - 7, 14, 14, 45, 280);
                    g.DrawLines(pen, new[] { new Point(x + 7, y - 5), new Point(x + 7, y + 1), new Point(x + 1, y - 1) });
                    break;
            }
        }
    }
}

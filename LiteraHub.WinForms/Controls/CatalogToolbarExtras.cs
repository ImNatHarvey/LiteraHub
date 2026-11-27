using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>Compact, theme-aware sorting menu. Never changes filter selections.</summary>
public sealed class CatalogSortButton : IconButton
{
    private Form? _popup;
    private string[] _options = Array.Empty<string>();
    private Func<int> _current = () => 0;
    private Action<int> _select = _ => { };
    private readonly ToolTip _tip = new();

    public CatalogSortButton()
    {
        Dock = DockStyle.Fill;
        Margin = new Padding(0, 4, 8, 4);
        FlatStyle = FlatStyle.Flat;
        Cursor = Cursors.Hand;
        IconChar = IconChar.Sort;
        IconSize = 18;
        Text = "";
        TextImageRelation = TextImageRelation.Overlay;
        ImageAlign = ContentAlignment.MiddleCenter;
        AccessibleName = "Sort by";
        _tip.SetToolTip(this, "Sort by");
        Click += (_, _) => ShowSortMenu();
        Theme.Changed += ThemeChanged;
        ApplyTheme();
    }

    public void Configure(IEnumerable<string> choices, Func<int> current, Action<int> select)
    {
        _options = choices.ToArray();
        _current = current;
        _select = select;
    }

    private void ThemeChanged(object? sender, EventArgs e)
    {
        if (!IsDisposed)
        {
            _popup?.Close();
            ApplyTheme();
        }
    }

    public void ApplyTheme()
    {
        var c = Theme.Colors;
        BackColor = c.Surface;
        ForeColor = c.Text;
        IconColor = c.Accent;
        FlatAppearance.BorderSize = 1;
        FlatAppearance.BorderColor = c.Border;
        FlatAppearance.MouseOverBackColor = c.SurfaceAlt;
        FlatAppearance.MouseDownBackColor = c.AccentSoft;
    }

    private void ShowSortMenu()
    {
        if (_popup is not null || !Enabled || _options.Length == 0) return;
        var c = Theme.Colors;
        var form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            ShowInTaskbar = false,
            TopMost = true,
            BackColor = c.Border,
            Padding = new Padding(1),
            Size = new Size(216, Math.Min(_options.Length * 36 + 30, 290))
        };
        _popup = form;
        var contents = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = false,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = c.Surface,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        contents.Controls.Add(new Label
        {
            Text = "SORT BY",
            Width = 212,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            ForeColor = c.TextMuted,
            BackColor = c.Surface,
            Font = Theme.Caption,
            Margin = Padding.Empty
        });
        for (int i = 0; i < _options.Length; i++)
        {
            int index = i;
            var btn = new Button
            {
                Width = 212,
                Height = 36,
                Text = (_current() == i ? "✓  " : "    ") + _options[i],
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(11, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = Padding.Empty,
                BackColor = _current() == i ? c.AccentSoft : c.Surface,
                ForeColor = c.Text,
                Font = Theme.Body
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = c.AccentSoft;
            btn.Click += (_, _) => { form.Close(); _select(index); };
            contents.Controls.Add(btn);
        }
        form.Controls.Add(contents);
        var work = Screen.FromControl(this).WorkingArea;
        Point below = PointToScreen(new Point(0, Height));
        int x = Math.Clamp(below.X, work.Left, Math.Max(work.Left, work.Right - form.Width));
        int y = below.Y + form.Height <= work.Bottom ? below.Y : PointToScreen(Point.Empty).Y - form.Height;
        form.Location = new Point(x, Math.Clamp(y, work.Top, Math.Max(work.Top, work.Bottom - form.Height)));
        form.Deactivate += (_, _) => { if (!form.IsDisposed) form.Close(); };
        form.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_popup, form)) _popup = null;
            form.Dispose();
        };
        form.Show(FindForm());
        form.Activate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= ThemeChanged;
            _tip.Dispose();
            _popup?.Close();
        }
        base.Dispose(disposing);
    }
}

/// <summary>Removable active filter pills; draws nothing when filters are clear.</summary>
public sealed class CatalogFilterChips : FlowLayoutPanel
{
    public CatalogFilterChips()
    {
        Dock = DockStyle.Fill;
        AutoScroll = false;
        WrapContents = false;
        FlowDirection = FlowDirection.LeftToRight;
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        Height = 26;
        BackColor = Theme.Colors.Background;
        Theme.Changed += ThemeChanged;
    }

    private void ThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed) return;
        var c = Theme.Colors;
        BackColor = c.Background;
        if (Tag is CatalogStatusHost holder && !holder.IsDisposed)
            holder.ApplyPalette();

        foreach (Control child in Controls)
        {
            if (child is Button button)
            {
                button.BackColor = c.AccentSoft;
                button.ForeColor = c.Text;
                button.FlatAppearance.MouseOverBackColor = c.AccentSoft;
            }
            else if (child is LinkLabel link)
            {
                link.LinkColor = c.Accent;
                link.ActiveLinkColor = c.Text;
            }
            else
                child.ForeColor = c.TextMuted;
        }
        Invalidate(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Theme.Changed -= ThemeChanged;
        base.Dispose(disposing);
    }

    public void Set(IEnumerable<(string Text, Action Remove)> filters, Action clearAll)
    {
        SuspendLayout();
        try
        {
            Controls.Clear();
            var entries = filters.ToArray();
            if (entries.Length == 0) return;
            foreach (var (label, remove) in entries.Take(5))
            {
                var chip = new Button
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Text = label + "  ×",
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 1, 6, 0),
                    Padding = new Padding(5, 1, 5, 1),
                    Font = Theme.Caption,
                    ForeColor = Theme.Colors.Text,
                    BackColor = Theme.Colors.AccentSoft,
                    FlatStyle = FlatStyle.Flat,
                    AccessibleName = "Remove filter " + label
                };
                chip.FlatAppearance.BorderSize = 0;
                chip.Click += (_, _) => remove();
                Controls.Add(chip);
            }
            if (entries.Length > 5)
            {
                Controls.Add(new Label
                {
                    AutoSize = true,
                    Text = $"+{entries.Length - 5} more",
                    Font = Theme.Caption,
                    ForeColor = Theme.Colors.TextMuted,
                    Margin = new Padding(2, 5, 8, 0)
                });
            }
            var clear = new LinkLabel
            {
                AutoSize = true,
                Text = "Clear all",
                Font = Theme.Caption,
                Margin = new Padding(2, 5, 0, 0),
                LinkColor = Theme.Colors.Accent,
                ActiveLinkColor = Theme.Colors.Text
            };
            clear.LinkClicked += (_, _) => clearAll();
            Controls.Add(clear);
        }
        finally { ResumeLayout(true); }
    }
}

/// <summary>
/// The results line and optional filter chips share one theme-aware surface.
/// Unlike a plain TableLayoutPanel, this surface repaints when the palette changes.
/// </summary>
public sealed class CatalogStatusHost : TableLayoutPanel
{
    private readonly Label _status;

    public CatalogStatusHost(Label status)
    {
        _status = status;
        Dock = DockStyle.Fill;
        ColumnCount = 1;
        RowCount = 2;
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
        RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Theme.Changed += HandleThemeChanged;
        ApplyPalette();
    }

    public void ApplyPalette()
    {
        if (IsDisposed) return;
        var background = Theme.Colors.Background;
        BackColor = background;
        _status.BackColor = background;
        _status.ForeColor = Theme.Colors.TextMuted;
        Invalidate(true);
    }

    private void HandleThemeChanged(object? sender, EventArgs e) => ApplyPalette();

    // Painting directly from the current palette prevents stale strips when
    // WinForms invalidates only the exposed portion during a theme switch.
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var brush = new SolidBrush(Theme.Colors.Background);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Theme.Changed -= HandleThemeChanged;
        base.Dispose(disposing);
    }
}

/// <summary>One layout contract shared by Books, Authors, Categories and Students.</summary>
public static class CatalogToolbarChrome
{
    public static void Install(TableLayoutPanel root, TableLayoutPanel toolbar,
        Label status, Control search, Control filter, Control add,
        CatalogSortButton sort, CatalogFilterChips chips, bool compact, bool canManage)
    {
        toolbar.SuspendLayout();
        root.SuspendLayout();
        try
        {
            toolbar.Controls.Clear();
            toolbar.ColumnStyles.Clear();
            toolbar.ColumnCount = 4;
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, compact ? 46 : 56));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, compact ? 46 : 56));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, compact ? 44 : 160));
            toolbar.RowCount = 1;
            if (toolbar.RowStyles.Count == 0)
                toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            filter.Dock = DockStyle.Fill;
            filter.Margin = new Padding(0, 4, 8, 4);
            sort.Margin = new Padding(0, 4, 8, 4);
            toolbar.Controls.Add(search, 0, 0);
            toolbar.Controls.Add(filter, 1, 0);
            toolbar.Controls.Add(sort, 2, 0);
            toolbar.Controls.Add(add, 3, 0);
            filter.Visible = canManage;
            sort.Visible = canManage;

            if (status.Parent == root)
            {
                var position = root.GetPositionFromControl(status);
                root.Controls.Remove(status);
                var holder = new CatalogStatusHost(status);
                status.Dock = DockStyle.Fill;
                status.Margin = Padding.Empty;
                holder.Controls.Add(status, 0, 0);
                holder.Controls.Add(chips, 0, 1);
                root.Controls.Add(holder, position.Column, position.Row);
                chips.Tag = holder;
                holder.ApplyPalette();
            }
            else if (chips.Tag is CatalogStatusHost holder)
                holder.ApplyPalette();
        }
        finally
        {
            root.ResumeLayout(true);
            toolbar.ResumeLayout(true);
        }
    }

    public static void ShowChips(TableLayoutPanel root, CatalogFilterChips chips,
        IEnumerable<(string Text, Action Remove)> filters, Action clear)
    {
        var items = filters.ToArray();
        bool hasFilters = items.Length > 0;
        chips.Visible = hasFilters;
        if (chips.Tag is CatalogStatusHost holder && holder.Parent == root)
        {
            int row = root.GetPositionFromControl(holder).Row;
            if (row >= 0 && row < root.RowStyles.Count)
            {
                root.RowStyles[row].SizeType = SizeType.Absolute;
                root.RowStyles[row].Height = hasFilters ? 61 : 36;
            }
        }
        chips.Set(items, clear);
    }
}

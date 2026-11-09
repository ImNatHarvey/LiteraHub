using FontAwesome.Sharp;
using LiteraHub.WinForms.Theming;
using System.ComponentModel;

namespace LiteraHub.WinForms.Controls;

public sealed record NavItem(string Key, string Title, IconChar Icon);

public sealed class SidebarNav : ThemedUserControl
{
    private readonly TableLayoutPanel _root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
    private readonly Label _brand = new() { Dock = DockStyle.Fill, Text = "LiteraHub" };
    private readonly TableLayoutPanel _items = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        ColumnCount = 1,
        Padding = new Padding(Space.S, Space.S, Space.S, 0)
    };
    private readonly TableLayoutPanel _footer = new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        ColumnCount = 1,
        Padding = new Padding(Space.S)
    };
    private readonly Label _user = new() { Dock = DockStyle.Fill, Height = 44, TextAlign = ContentAlignment.MiddleLeft };
    private readonly IconButton _logout = new();
    private readonly ToolTip _tip = new();
    private readonly List<(NavItem Item, IconButton Button)> _buttons = new();

    private string? _selectedKey;
    private bool _compact;

    public event EventHandler<string>? ItemSelected;

    public event EventHandler? LogoutRequested;

    public SidebarNav()
    {
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _items.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _user.Padding = new Padding(Space.M, 0, 0, 0);
        _logout.Dock = DockStyle.Fill;
        _logout.Height = 40;
        _logout.IconChar = AppIcons.Logout;
        _logout.IconSize = 20;
        _logout.Cursor = Cursors.Hand;
        _logout.TextImageRelation = TextImageRelation.ImageBeforeText;
        _logout.Click += (_, _) => LogoutRequested?.Invoke(this, EventArgs.Empty);

        _footer.Controls.Add(_user);
        _footer.Controls.Add(_logout);

        _root.Controls.Add(_brand, 0, 0);
        _root.Controls.Add(_items, 0, 1);
        _root.Controls.Add(_footer, 0, 2);
        Controls.Add(_root);

        ApplyCompact();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Compact
    {
        get => _compact;
        set
        {
            if (_compact == value) return;
            _compact = value;
            ApplyCompact();
        }
    }

    public void SetUser(string username, string role)
    {
        _user.Text = $"{username}{Environment.NewLine}{role}";
    }

    public void SetItems(IEnumerable<NavItem> items)
    {
        _items.SuspendLayout();

        foreach (var (_, existing) in _buttons)
        {
            _items.Controls.Remove(existing);
            existing.Dispose();
        }
        _buttons.Clear();

        foreach (var item in items)
        {
            var button = new IconButton
            {
                Dock = DockStyle.Fill,
                Height = 40,
                Margin = new Padding(0, 0, 0, Space.XS),
                IconChar = item.Icon,
                IconSize = 20,
                Cursor = Cursors.Hand
            };
            button.Click += (_, _) =>
            {
                SelectItem(item.Key);
                ItemSelected?.Invoke(this, item.Key);
            };
            _items.Controls.Add(button);
            _buttons.Add((item, button));
        }

        ApplyCompact();
        if (IsHandleCreated) ApplyTheme();
        _items.ResumeLayout(true);
    }

    public void SelectItem(string key)
    {
        _selectedKey = key;
        foreach (var (item, button) in _buttons)
            StyleButton(button, item.Key == key);
    }

    private void ApplyCompact()
    {
        _brand.Text = _compact ? "LH" : "LiteraHub";
        _brand.TextAlign = _compact ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
        _brand.Padding = _compact ? new Padding(0) : new Padding(Space.L, 0, 0, 0);

        foreach (var (item, button) in _buttons)
        {
            button.Text = _compact ? "" : "  " + item.Title;
            button.TextImageRelation = _compact ? TextImageRelation.Overlay : TextImageRelation.ImageBeforeText;
            button.ImageAlign = _compact ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = _compact ? new Padding(0) : new Padding(Space.M, 0, 0, 0);
            _tip.SetToolTip(button, _compact ? item.Title : "");
        }

        _logout.Text = _compact ? "" : "  Log out";
        _logout.ImageAlign = _compact ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
        _logout.TextAlign = ContentAlignment.MiddleLeft;
        _logout.Padding = _compact ? new Padding(0) : new Padding(Space.M, 0, 0, 0);
        _tip.SetToolTip(_logout, _compact ? "Log out" : "");
        _user.Visible = !_compact;
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;

        BackColor = c.Surface;
        _root.BackColor = c.Surface;
        _items.BackColor = c.Surface;
        _footer.BackColor = c.Surface;

        _brand.BackColor = c.Surface;
        _brand.ForeColor = c.Accent;
        _brand.Font = Theme.Title;

        _user.BackColor = c.Surface;
        _user.ForeColor = c.TextMuted;
        _user.Font = Theme.Caption;

        StyleButton(_logout, selected: false);
        foreach (var (item, button) in _buttons)
            StyleButton(button, item.Key == _selectedKey);
    }

    private static void StyleButton(IconButton button, bool selected)
    {
        var c = Theme.Colors;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = c.SurfaceAlt;
        button.FlatAppearance.MouseDownBackColor = c.SurfaceAlt;
        button.BackColor = selected ? c.AccentSoft : c.Surface;
        button.ForeColor = selected ? c.Text : c.TextMuted;
        button.IconColor = selected ? c.Accent : c.TextMuted;
        button.Font = selected ? Theme.BodyStrong : Theme.Body;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tip.Dispose();
        base.Dispose(disposing);
    }
}
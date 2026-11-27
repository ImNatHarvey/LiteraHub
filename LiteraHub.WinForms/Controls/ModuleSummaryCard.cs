using FontAwesome.Sharp;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

public sealed class ModuleSummaryCard : ThemedUserControl
{
    private readonly Label _label = new() { AutoEllipsis = true, UseMnemonic = false };
    private readonly Label _value = new() { AutoEllipsis = true, UseMnemonic = false };
    private readonly IconPictureBox _icon = new() { IconSize = 18, SizeMode = PictureBoxSizeMode.CenterImage, TabStop = false };
    private readonly Action? _action;
    private readonly ToolTip _tip = new();
    private bool _selected, _hover, _ready;
    private string _display = "…";

    public ModuleSummaryCard(string title, IconChar icon, Action? action)
    {
        _action = action;
        _label.Text = title;
        _icon.IconChar = icon;
        AccessibleName = title;
        AccessibleRole = action is null ? AccessibleRole.StaticText : AccessibleRole.PushButton;
        SetStyle(ControlStyles.Selectable | ControlStyles.ResizeRedraw, true);
        TabStop = action is not null;
        Margin = Padding.Empty;
        Controls.AddRange(new Control[] { _label, _value, _icon });
        foreach (Control c in new Control[] { this, _label, _value, _icon })
        {
            c.MouseEnter += (_, _) => UpdateHover();
            c.MouseLeave += (_, _) => UpdateHover();
            c.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) Activate(); };
            _tip.SetToolTip(c, action is null ? title : title + " — filter records");
        }
        SetValue(null, false);
        ApplyTheme();
    }

    private void Activate()
    {
        if (!_ready || _action is null) return;
        Focus();
        _action();
    }

    private void UpdateHover()
    {
        if (IsDisposed) return;
        bool hover = _ready && _action is not null && ClientRectangle.Contains(PointToClient(System.Windows.Forms.Cursor.Position));
        if (hover == _hover) return;
        _hover = hover;
        ApplyTheme();
    }

    public void SetValue(long? value, bool unavailable)
    {
        _ready = value.HasValue;
        _display = value?.ToString("N0") ?? (unavailable ? "—" : "…");
        _value.Text = _display;
        AccessibleDescription = value.HasValue ? _display : unavailable ? "Unavailable" : "Loading";
        Cursor = _ready && _action is not null ? Cursors.Hand : Cursors.Default;
        TabStop = _ready && _action is not null;
        if (!_ready) _hover = false;
        ApplyTheme();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        ApplyTheme();
    }

    protected override bool IsInputKey(Keys keyData) =>
        (keyData & Keys.KeyCode) is Keys.Enter or Keys.Space || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Enter or Keys.Space) { e.Handled = true; e.SuppressKeyPress = true; Activate(); }
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_label is null || _value is null || _icon is null) return;
        int p = 12;
        _label.SetBounds(p, 6, Math.Max(1, Width - p * 2 - 24), 23);
        _icon.SetBounds(Math.Max(p, Width - p - 20), 8, 20, 20);
        _value.SetBounds(p, 29, Math.Max(1, Width - p * 2), Math.Max(1, Height - 35));
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        if (_label is null || _value is null || _icon is null) return;
        var c = Theme.Colors;
        BackColor = _selected ? c.AccentSoft : _hover ? c.SurfaceAlt : c.Surface;
        _label.BackColor = _value.BackColor = _icon.BackColor = BackColor;
        _label.ForeColor = c.TextMuted;
        _value.ForeColor = c.Text;
        _icon.IconColor = c.Accent;
        _label.Font = Theme.Body;
        _value.Font = Theme.Display;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width < 2 || Height < 2) return;
        using var pen = new Pen(_selected || Focused ? Theme.Colors.Accent : Theme.Colors.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tip.Dispose();
        base.Dispose(disposing);
    }
}

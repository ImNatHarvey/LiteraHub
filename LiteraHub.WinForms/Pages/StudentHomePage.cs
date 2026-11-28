using FontAwesome.Sharp;
using LiteraHub.Data.Repositories;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

public sealed class StudentHomePage : ThemedUserControl
{
    private readonly Label _title = new() { Text = "Home", Dock = DockStyle.Top, Height = 46, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _subtitle = new() { Dock = DockStyle.Top, Height = 38, UseMnemonic = false, AutoEllipsis = true };
    private readonly ScrollHost _scroll = new() { Dock = DockStyle.Fill };
    private readonly Panel _content = new();
    private readonly ThemedSectionCard _identity, _help;
    private readonly Label _numberLabel = new() { Text = "Student ID" }, _nameLabel = new() { Text = "Full Name" }, _usernameLabel = new() { Text = "Username" }, _statusLabel = new() { Text = "Account Status" };
    private readonly Label _number = Value(), _name = Value(), _username = Value(), _status = Value("Active");
    private static Label Value(string text = "")
    {
        var value = new Label { Text = text, AutoSize = false, TabStop = false, UseMnemonic = false, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 3, 12, 3) };
        value.Paint += (_, e) => { using var pen = new Pen(Theme.Colors.Border); e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, value.Width - 1), Math.Max(0, value.Height - 1)); };
        return value;
    }
    private readonly Label _helpText = new() { Text = "Your student account is active. Use Profile & Settings to change your username, password, recovery passkey or appearance. For changes to your enrollment or personal student record, please visit the library.", UseMnemonic = false };
    private bool _arranging;
    public StudentHomePage(StudentPortalInfo info)
    {
        Padding = new Padding(Space.L, Space.L, 0, Space.L);
        _identity = new ThemedSectionCard(new Label { Text = "Student Information" }, IconChar.UserGraduate);
        _help = new ThemedSectionCard(new Label { Text = "Account & Security" }, IconChar.Lock);
        _identity.AddFields(_numberLabel, _number, _nameLabel, _name, _usernameLabel, _username, _statusLabel, _status); _help.AddFields(_helpText);
        _content.Controls.AddRange(new Control[] { _identity, _help }); _scroll.ReservedRightGutter = Space.L; _scroll.SetContent(_content);
        Controls.Add(_scroll); Controls.Add(_subtitle); Controls.Add(_title); _scroll.Resize += (_, _) => Arrange();
        WireWheel(_content); UpdateInfo(info); ApplyTheme(); Arrange();
    }
    private void WireWheel(Control control)
    {
        control.MouseWheel += (_, e) => { _scroll.ScrollByPixels(e.Delta > 0 ? -60 : 60); if (e is HandledMouseEventArgs handled) handled.Handled = true; };
        foreach (Control child in control.Controls) WireWheel(child);
    }
    public void UpdateInfo(StudentPortalInfo info) { _subtitle.Text = $"Welcome, {info.FullName}."; _name.Text = info.FullName; _number.Text = info.StudentNo; _username.Text = info.Username; }
    private void Arrange()
    {
        if (_arranging || _scroll.ClientSize.Width <= Space.L) return; _arranging = true;
        try
        {
            int w = Math.Max(1, _scroll.ClientSize.Width - Space.L); var layout = new ResponsiveSectionLayout(w); int gap = layout.Gap, pad = layout.Padding;
            bool two = w >= 620; int left = two ? (w - gap) / 2 : w; int right = two ? w - gap - left : w;
            int y = _identity.ArrangeHeading(left, pad); var labels = new[] { _numberLabel, _nameLabel, _usernameLabel, _statusLabel }; var fields = new[] { _number, _name, _username, _status };
            for (int i = 0; i < labels.Length; i++) { labels[i].SetBounds(pad, y, left - 2 * pad, 22); y += 24; fields[i].SetBounds(pad, y, left - 2 * pad, 40); y += 50; }
            _identity.SetBounds(0, 0, left, y + pad); int h = _help.ArrangeHeading(right, pad);
            int inner = Math.Max(1, right - 2 * pad); int textHeight = Math.Max(64, TextRenderer.MeasureText(_helpText.Text, Theme.Body, new Size(inner, int.MaxValue), TextFormatFlags.WordBreak).Height + 8);
            _helpText.SetBounds(pad, h, inner, textHeight); _help.SetBounds(two ? left + gap : 0, two ? 0 : _identity.Bottom + gap, right, h + textHeight + pad);
            _content.Height = Math.Max(_identity.Bottom, _help.Bottom) + 2; _scroll.RefreshMetrics();
        }
        finally { _arranging = false; }
    }
    protected override void ApplyTheme() { base.ApplyTheme(); var c = Theme.Colors; _title.ForeColor = c.Text; _title.Font = Theme.Title; _subtitle.ForeColor = c.TextMuted; _subtitle.Font = Theme.Body; _content.BackColor = c.Background; foreach (var l in new[] { _numberLabel, _nameLabel, _usernameLabel, _statusLabel, _helpText }) { l.Font = Theme.Body; l.ForeColor = c.Text; l.BackColor = c.Surface; } foreach (var v in new[] { _number, _name, _username, _status }) { v.BackColor = c.SurfaceAlt; v.ForeColor = c.Text; v.Font = Theme.BodyStrong; v.Invalidate(); } Arrange(); }
}

using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

// Temporary page used until each module is built.
public sealed class PlaceholderPage : ThemedUserControl
{
    private readonly FlowLayoutPanel _flow = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false
    };

    private readonly Label _title = new() { AutoSize = true };
    private readonly Label _note = new() { AutoSize = true };

    public PlaceholderPage(string title, string note)
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(Space.XL);

        _title.Text = title;
        _title.Margin = new Padding(0, 0, 0, Space.S);
        _note.Text = note;

        _flow.Controls.Add(_title);
        _flow.Controls.Add(_note);
        Controls.Add(_flow);
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        _flow.BackColor = c.Background;
        _title.BackColor = c.Background;
        _title.ForeColor = c.Text;
        _title.Font = Theme.Title;
        _note.BackColor = c.Background;
        _note.ForeColor = c.TextMuted;
        _note.Font = Theme.Body;
    }
}
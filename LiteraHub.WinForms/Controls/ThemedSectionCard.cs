using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>Theme-aware section container. The owning page arranges its existing fields.</summary>
public sealed class ThemedSectionCard : ThemedUserControl
{
    private readonly Label _heading;
    private readonly IconPictureBox _icon;

    public ThemedSectionCard(Label heading, IconChar icon)
    {
        _heading = heading;
        _icon = new IconPictureBox { IconChar = icon, IconSize = 16, SizeMode = PictureBoxSizeMode.CenterImage, TabStop = false };
        Margin = Padding.Empty;
        Controls.Add(_heading);
        Controls.Add(_icon);
        SetStyle(ControlStyles.ResizeRedraw, true);
        ApplyTheme();
    }

    public void AddFields(params Control[] fields) => Controls.AddRange(fields);

    // Returns the first field's local Y coordinate.
    public int ArrangeHeading(int width, int padding)
    {
        _icon.SetBounds(padding, padding + 4, 18, 18);
        _heading.SetBounds(padding + 25, padding, Math.Max(1, width - padding * 2 - 25), 26);
        return padding + 32;
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        BackColor = c.Surface;
        if (_heading is null || _icon is null) return;
        _heading.BackColor = c.Surface;
        _heading.ForeColor = c.Text;
        _heading.Font = Theme.BodyStrong;
        _icon.BackColor = c.Surface;
        _icon.IconColor = c.TextMuted;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Theme.Colors.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
    }
}

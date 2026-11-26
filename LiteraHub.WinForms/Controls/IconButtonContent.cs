using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

public static class IconButtonContent
{
    public const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
        TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;

    public static (Rectangle Icon, Rectangle Text) Layout(Graphics g, Size size, Font font,
        string text, int iconWidth, int gap = 6)
    {
        int available = Math.Max(0, size.Width - 8);
        iconWidth = Math.Clamp(iconWidth, 0, available);
        if (iconWidth == 0 || string.IsNullOrEmpty(text)) gap = 0;
        gap = Math.Min(gap, Math.Max(0, available - iconWidth));
        int textWidth = string.IsNullOrEmpty(text) ? 0 : Math.Min(
            TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), Flags).Width,
            Math.Max(0, available - iconWidth - gap));
        int left = (size.Width - iconWidth - gap - textWidth) / 2;
        return (new Rectangle(left, (size.Height - iconWidth) / 2, iconWidth, iconWidth),
            new Rectangle(left + iconWidth + gap, 0, textWidth, size.Height));
    }

    public static void DrawAction(PaintEventArgs e, Button button, int kind, string label, bool restoring)
    {
        if (button.Width < 90 || button.Height < 24) return;
        var c = Theme.Colors;
        var g = e.Graphics;
        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Color iconColor = !button.Enabled ? c.TextMuted : kind == 2 ? Color.White :
            kind == 0 || restoring ? Color.FromArgb(118, 235, 173) : c.TextMuted;
        var layout = Layout(g, button.ClientSize, button.Font, label, 22);
        int cx = layout.Icon.Left + layout.Icon.Width / 2;
        int cy = button.Height / 2;
        using var pen = new Pen(iconColor, 2f);
        if (kind == 0)
        {
            g.DrawLine(pen, cx - 8, cy - 7, cx + 1, cy - 7);
            g.DrawLine(pen, cx - 8, cy - 7, cx - 8, cy + 8);
            g.DrawLine(pen, cx - 8, cy + 8, cx + 7, cy + 8);
            g.DrawLine(pen, cx + 7, cy + 8, cx + 7, cy + 1);
            g.DrawLine(pen, cx - 2, cy + 3, cx + 8, cy - 7);
            g.DrawLine(pen, cx - 3, cy + 5, cx - 2, cy + 3);
        }
        else if (kind == 1)
        {
            g.DrawRectangle(pen, cx - 8, cy - 4, 16, 12);
            g.DrawLine(pen, cx - 10, cy - 8, cx + 10, cy - 8);
            g.DrawLine(pen, cx, restoring ? cy + 5 : cy - 2, cx, restoring ? cy - 2 : cy + 5);
            if (restoring)
            {
                g.DrawLine(pen, cx - 4, cy + 1, cx, cy - 3);
                g.DrawLine(pen, cx, cy - 3, cx + 4, cy + 1);
            }
            else
            {
                g.DrawLine(pen, cx - 4, cy + 1, cx, cy + 5);
                g.DrawLine(pen, cx, cy + 5, cx + 4, cy + 1);
            }
        }
        else
        {
            g.DrawLine(pen, cx - 9, cy - 6, cx + 9, cy - 6);
            g.DrawLine(pen, cx - 4, cy - 9, cx + 4, cy - 9);
            g.DrawRectangle(pen, cx - 7, cy - 3, 14, 12);
            g.DrawLine(pen, cx - 3, cy, cx - 3, cy + 6);
            g.DrawLine(pen, cx + 3, cy, cx + 3, cy + 6);
        }
        TextRenderer.DrawText(g, label, button.Font, layout.Text,
            button.Enabled ? button.ForeColor : c.TextMuted, Flags | TextFormatFlags.EndEllipsis);
        g.SmoothingMode = previous;
    }
}

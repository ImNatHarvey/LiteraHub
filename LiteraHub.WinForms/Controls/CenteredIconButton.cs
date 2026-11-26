using System;
using FontAwesome.Sharp;
using LiteraHub.WinForms.Theming;
using System.Drawing;
using System.Windows.Forms;

namespace LiteraHub.WinForms.Controls;

/// <summary>Flat themed icon button whose icon and text share one centered content rectangle.</summary>
public sealed class CenteredIconButton : IconButton
{
    private bool _hovered, _pressed;

    protected override void OnMouseEnter(EventArgs e)
    { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e)
    { base.OnMouseLeave(e); _hovered = false; _pressed = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e)
    { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) _pressed = true; Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs e)
    { base.OnMouseUp(e); _pressed = false; Invalidate(); }
    protected override void OnMouseCaptureChanged(EventArgs e)
    { base.OnMouseCaptureChanged(e); if (!Capture) _pressed = false; Invalidate(); }
    protected override void OnKeyDown(KeyEventArgs e)
    { base.OnKeyDown(e); if (e.KeyCode == Keys.Space) { _pressed = true; Invalidate(); } }
    protected override void OnKeyUp(KeyEventArgs e)
    { base.OnKeyUp(e); _pressed = false; Invalidate(); }
    protected override void OnLostFocus(EventArgs e)
    { base.OnLostFocus(e); _pressed = false; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var colors = Theme.Colors;
        Color background = !Enabled ? BackColor : _pressed && (_hovered || Focused)
            ? FlatAppearance.MouseDownBackColor : _hovered ? FlatAppearance.MouseOverBackColor : BackColor;
        if (background.IsEmpty) background = BackColor;
        using (var brush = new SolidBrush(background)) g.FillRectangle(brush, ClientRectangle);
        if (FlatAppearance.BorderSize > 0 && Width > 1 && Height > 1)
        {
            using var pen = new Pen(FlatAppearance.BorderColor.IsEmpty ? colors.Border : FlatAppearance.BorderColor);
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        var icon = Image;
        int iconWidth = icon is null ? 0 : Math.Min(IconSize, Math.Max(0, Height - 8));
        var layout = IconButtonContent.Layout(g, ClientSize, Font, Text, iconWidth);
        if (icon is not null && layout.Icon.Width > 0)
        {
            if (Enabled) g.DrawImage(icon, layout.Icon);
            else ControlPaint.DrawImageDisabled(g, icon, layout.Icon.X, layout.Icon.Y, background);
        }
        if (layout.Text.Width > 0)
            TextRenderer.DrawText(g, Text, Font, layout.Text,
                Enabled ? ForeColor : colors.TextMuted, IconButtonContent.Flags | TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues && Width > 8 && Height > 8)
            ControlPaint.DrawFocusRectangle(g, new Rectangle(3, 3, Width - 6, Height - 6));
    }
}

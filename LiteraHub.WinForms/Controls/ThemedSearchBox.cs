
using System;
using System.Drawing;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>
/// A themed, padded search field with vertically centered text.
/// Search is intentionally read-only until implemented.
/// </summary>
public sealed class ThemedSearchBox : ThemedUserControl
{
    private readonly TextBox _input = new()
    {
        BorderStyle = BorderStyle.None,
        Multiline = false,
        ReadOnly = true,
        TabStop = false,
        Font = Theme.Body
    };

    private readonly Label _placeholder = new()
    {
        Text = "Search categories...",
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
        Cursor = Cursors.Default,
        Font = Theme.Body
    };

    public ThemedSearchBox()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);

        Height = 38;
        MinimumSize = new Size(100, 38);
        Margin = Padding.Empty;
        TabStop = false;

        Controls.Add(_input);
        Controls.Add(_placeholder);

        _placeholder.BringToFront();

        Resize += (_, _) => PositionContents();

        _input.TextChanged += (_, _) =>
        {
            _placeholder.Visible =
                string.IsNullOrEmpty(_input.Text);
        };

        _input.GotFocus += (_, _) =>
            _placeholder.Visible =
                string.IsNullOrEmpty(_input.Text);

        PositionContents();
    }

    private void PositionContents()
    {
        if (IsDisposed || Disposing)
            return;

        const int horizontalPadding = 12;

        int contentWidth = Math.Max(
            1,
            ClientSize.Width - horizontalPadding * 2);

        int textHeight = _input.PreferredHeight;

        int textY = Math.Max(
            0,
            (ClientSize.Height - textHeight) / 2);

        _input.SetBounds(
            horizontalPadding,
            textY,
            contentWidth,
            textHeight);

        _placeholder.SetBounds(
            horizontalPadding,
            1,
            contentWidth,
            Math.Max(1, ClientSize.Height - 2));
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();

        var c = Theme.Colors;

        BackColor = c.Surface;

        _input.BackColor = c.Surface;
        _input.ForeColor = c.Text;
        _input.Font = Theme.Body;

        _placeholder.BackColor = c.Surface;
        _placeholder.ForeColor = c.TextMuted;
        _placeholder.Font = Theme.Body;

        PositionContents();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var pen = new Pen(Theme.Colors.Border);

        var rect = ClientRectangle;

        rect.Width--;
        rect.Height--;

        if (rect.Width > 0 && rect.Height > 0)
            e.Graphics.DrawRectangle(pen, rect);
    }
}

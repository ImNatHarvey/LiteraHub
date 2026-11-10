
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

public sealed class ThemedSearchBox : ThemedUserControl
{
    private readonly TextBox _input = new()
    {
        BorderStyle = BorderStyle.None,
        Multiline = false,
        TabStop = true
    };

    private readonly Label _placeholder = new()
    {
        Text = "Search categories...",
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
        Cursor = Cursors.IBeam
    };

    private bool _focused;

    public new event EventHandler? TextChanged;

    [AllowNull]
    [DesignerSerializationVisibility(
        DesignerSerializationVisibility.Hidden)]
    public override string Text
    {
        get => _input?.Text ?? string.Empty;
        set
        {
            if (_input != null)
                _input.Text = value ?? string.Empty;
        }
    }

    [DesignerSerializationVisibility(
        DesignerSerializationVisibility.Hidden)]
    public string Placeholder
    {
        get => _placeholder.Text;
        set => _placeholder.Text = value ?? string.Empty;
    }

    public ThemedSearchBox()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);

        Height = 38;
        MinimumSize = new Size(80, 38);
        Margin = Padding.Empty;
        Cursor = Cursors.IBeam;

        Controls.Add(_input);
        Controls.Add(_placeholder);

        _input.TextChanged += (_, _) =>
        {
            UpdatePlaceholder();
            TextChanged?.Invoke(this, EventArgs.Empty);
        };

        _input.GotFocus += (_, _) =>
        {
            _focused = true;
            UpdatePlaceholder();
            Invalidate();
        };

        _input.LostFocus += (_, _) =>
        {
            _focused = false;
            UpdatePlaceholder();
            Invalidate();
        };

        _placeholder.Click += (_, _) => _input.Focus();
        Click += (_, _) => _input.Focus();
        Resize += (_, _) => PositionContents();

        PositionContents();
        UpdatePlaceholder();
    }

    public void FocusInput() => _input.Focus();

    private void UpdatePlaceholder()
    {
        _placeholder.Visible =
            !_focused && string.IsNullOrEmpty(_input.Text);
    }

    private void PositionContents()
    {
        if (IsDisposed || Disposing) return;

        int left = 39;
        int width = Math.Max(1, ClientSize.Width - left - 12);
        int height = _input.PreferredHeight;
        int top = Math.Max(0, (ClientSize.Height - height) / 2);

        _input.SetBounds(left, top, width, height);
        _placeholder.SetBounds(
            left, 1, width, Math.Max(1, Height - 2));
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

        var c = Theme.Colors;

        using var border = new Pen(
            _focused ? c.Accent : c.Border);

        var rect = ClientRectangle;
        rect.Width--;
        rect.Height--;

        if (rect.Width > 0 && rect.Height > 0)
            e.Graphics.DrawRectangle(border, rect);

        // Draw a magnifying glass with theme-aware GDI+.
        e.Graphics.SmoothingMode =
            System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var iconPen = new Pen(c.TextMuted, 1.8f);

        e.Graphics.DrawEllipse(iconPen, 13, 12, 11, 11);
        e.Graphics.DrawLine(iconPen, 22, 21, 28, 27);
    }
}

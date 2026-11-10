
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        get => _input?.Text ?? "";
        set
        {
            if (_input != null)
                _input.Text = value ?? "";
        }
    }

    [DesignerSerializationVisibility(
        DesignerSerializationVisibility.Hidden)]
    public string Placeholder
    {
        get => _placeholder.Text;
        set => _placeholder.Text = value ?? "";
    }

    public ThemedSearchBox()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);

        Height = 40;
        MinimumSize = new Size(80, 40);
        Cursor = Cursors.IBeam;

        Controls.Add(_placeholder);
        Controls.Add(_input);

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
        // The placeholder must never cover the input caret.
        _placeholder.Visible =
            !_focused && string.IsNullOrEmpty(_input.Text);
    }

    private void PositionContents()
    {
        if (IsDisposed || Disposing) return;

        const int left = 40;
        int width = Math.Max(1, ClientSize.Width - left - 12);
        int height = _input.PreferredHeight;
        int y = Math.Max(0, (ClientSize.Height - height) / 2);

        _input.SetBounds(left, y, width, height);
        _placeholder.SetBounds(
            left, y, width, height);
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
        UpdatePlaceholder();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var c = Theme.Colors;
        var g = e.Graphics;

        using var border = new Pen(
            _focused ? c.Accent : c.Border);

        var rect = ClientRectangle;
        rect.Width--;
        rect.Height--;

        if (rect.Width > 0 && rect.Height > 0)
            g.DrawRectangle(border, rect);

        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int cx = 20;
        int cy = ClientSize.Height / 2;

        using var pen = new Pen(c.TextMuted, 1.8f);

        g.DrawEllipse(pen, cx - 7, cy - 8, 12, 12);
        g.DrawLine(pen, cx + 3, cy + 3, cx + 9, cy + 9);

        g.SmoothingMode = old;
    }
}

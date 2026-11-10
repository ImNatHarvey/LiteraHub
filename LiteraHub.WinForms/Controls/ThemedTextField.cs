
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

public sealed class ThemedTextField : ThemedUserControl
{
    private readonly TextBox _input = new()
    {
        BorderStyle = BorderStyle.None,
        Multiline = false,
        ScrollBars = ScrollBars.None,
        WordWrap = true,
        Margin = Padding.Empty
    };

    private bool _focused;
    private bool _multiline;

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
    public int MaxLength
    {
        get => _input.MaxLength;
        set => _input.MaxLength = value;
    }

    [DesignerSerializationVisibility(
        DesignerSerializationVisibility.Hidden)]
    public bool Multiline
    {
        get => _multiline;
        set
        {
            _multiline = value;
            _input.Multiline = value;
            _input.ScrollBars = ScrollBars.None;
            LayoutInput();
        }
    }

    public ThemedTextField()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);

        Height = 40;
        MinimumSize = new Size(80, 40);
        Cursor = Cursors.IBeam;

        Controls.Add(_input);

        _input.TextChanged += (_, _) =>
            TextChanged?.Invoke(this, EventArgs.Empty);

        _input.GotFocus += (_, _) =>
        {
            _focused = true;
            Invalidate();
        };

        _input.LostFocus += (_, _) =>
        {
            _focused = false;
            Invalidate();
        };

        Click += (_, _) => _input.Focus();
        Resize += (_, _) => LayoutInput();

        LayoutInput();
    }

    public void FocusInput() => _input.Focus();

    private void LayoutInput()
    {
        if (IsDisposed || Disposing) return;

        const int left = 12;
        const int right = 12;

        int width = Math.Max(
            1, ClientSize.Width - left - right);

        if (_multiline)
        {
            _input.SetBounds(
                left,
                10,
                width,
                Math.Max(1, ClientSize.Height - 20));
        }
        else
        {
            int height = _input.PreferredHeight;

            _input.SetBounds(
                left,
                Math.Max(0, (ClientSize.Height - height) / 2),
                width,
                height);
        }
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();

        var c = Theme.Colors;

        BackColor = c.Surface;

        _input.BackColor = c.Surface;
        _input.ForeColor = c.Text;
        _input.Font = Theme.Body;

        LayoutInput();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var c = Theme.Colors;

        using var pen = new Pen(
            _focused ? c.Accent : c.Border);

        var rect = ClientRectangle;
        rect.Width--;
        rect.Height--;

        if (rect.Width > 0 && rect.Height > 0)
            e.Graphics.DrawRectangle(pen, rect);
    }
}


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
        Dock = DockStyle.Fill,
        Margin = Padding.Empty
    };

    private readonly Panel _inner = new()
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(12, 9, 12, 9)
    };

    private bool _focused;
    private bool _multiline;

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
            _input.ScrollBars = value
                ? ScrollBars.Vertical
                : ScrollBars.None;

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

        Controls.Add(_inner);
        _inner.Controls.Add(_input);

        Padding = new Padding(1);

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
        _inner.Click += (_, _) => _input.Focus();

        Resize += (_, _) => LayoutInput();

        LayoutInput();
    }

    public void FocusInput() => _input.Focus();

    private void LayoutInput()
    {
        if (IsDisposed || Disposing) return;

        if (_multiline)
        {
            _inner.Padding = new Padding(12, 9, 12, 9);
            _input.Multiline = true;
            _input.Dock = DockStyle.Fill;
        }
        else
        {
            _inner.Padding = new Padding(12, 0, 12, 0);
            _input.Multiline = false;
            _input.Dock = DockStyle.None;

            int height = _input.PreferredHeight;
            _input.SetBounds(
                12,
                Math.Max(0, (Height - height) / 2),
                Math.Max(1, Width - 26),
                height);
        }
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();

        var c = Theme.Colors;

        BackColor = c.Surface;
        _inner.BackColor = c.Surface;

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

using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>
/// Theme-aware window caption buttons without a separate title-bar strip.
/// Anchor this control at the upper-right corner of a borderless Form.
/// This component is reusable on LoginForm and, later, ShellForm.
/// It intentionally does not alter a Form's border style or maximize behavior.
/// </summary>
public sealed class ThemedWindowControls : UserControl
{
    private const int ButtonWidth = 43;
    private const int ButtonHeight = 34;

    private readonly IconButton _minimize = MakeButton(IconChar.WindowMinimize, "Minimize window");
    private readonly IconButton _maximize = MakeButton(IconChar.WindowMaximize, "Maximize window");
    private readonly IconButton _close = MakeButton(IconChar.Times, "Close window");

    private bool _showMaximizeButton;
    private bool _showMinimizeButton = true;
    private Form? _targetForm;
    private Color _surface;
    private Color _hover;
    private Color _danger;
    private Color? _surfaceOverride;
    private bool? _maximizedOverride;

    /// <summary>Raised when a host wants custom fullscreen behavior.</summary>
    public event EventHandler? MaximizeRequested;

    /// <summary>Update the glyph when host handles maximize outside WindowState.</summary>
    public void SetMaximizedState(bool maximized)
    {
        _maximizedOverride = maximized;
        SyncMaximizeIcon();
    }

    [System.ComponentModel.DesignerSerializationVisibility(
        System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool ShowMaximizeButton
    {
        get => _showMaximizeButton;
        set
        {
            if (_showMaximizeButton == value) return;
            _showMaximizeButton = value;
            LayoutButtons();
        }
    }

    [System.ComponentModel.DesignerSerializationVisibility(
        System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool ShowMinimizeButton
    {
        get => _showMinimizeButton;
        set
        {
            if (_showMinimizeButton == value) return;
            _showMinimizeButton = value;
            LayoutButtons();
        }
    }

    public ThemedWindowControls()
    {
        DoubleBuffered = true;
        TabStop = false;
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        Height = ButtonHeight;
        Controls.AddRange(new Control[] { _minimize, _maximize, _close });

        _minimize.Click += (_, _) =>
        {
            if (FindTarget() is { } form) form.WindowState = FormWindowState.Minimized;
        };
        _maximize.Click += (_, _) => ToggleMaximize();
        _close.Click += (_, _) => FindTarget()?.Close();

        _minimize.MouseEnter += (_, _) => HoverButton(_minimize, true);
        _minimize.MouseLeave += (_, _) => HoverButton(_minimize, false);
        _maximize.MouseEnter += (_, _) => HoverButton(_maximize, true);
        _maximize.MouseLeave += (_, _) => HoverButton(_maximize, false);
        _close.MouseEnter += (_, _) => HoverButton(_close, true);
        _close.MouseLeave += (_, _) => HoverButton(_close, false);

        Theme.Changed += OnThemeChanged;
        LayoutButtons();
        ApplyTheme();
    }

    public void Bind(Form form)
    {
        _targetForm = form ?? throw new ArgumentNullException(nameof(form));
        form.Resize += TargetResized;
        SyncMaximizeIcon();
    }

    private static IconButton MakeButton(IconChar icon, string name) => new()
    {
        IconChar = icon,
        IconSize = 16,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        TabStop = false,
        AccessibleName = name,
        Text = string.Empty,
        Margin = Padding.Empty,
        Size = new Size(ButtonWidth, ButtonHeight)
    };

    private Form? FindTarget() => _targetForm ?? FindForm();

    private void LayoutButtons()
    {
        _minimize.Visible = _showMinimizeButton;
        _maximize.Visible = _showMaximizeButton;
        Width = ButtonWidth * (1 + (_showMinimizeButton ? 1 : 0) + (_showMaximizeButton ? 1 : 0));
        if (_showMinimizeButton) _minimize.SetBounds(0, 0, ButtonWidth, ButtonHeight);
        if (_showMaximizeButton)
            _maximize.SetBounds(_showMinimizeButton ? ButtonWidth : 0, 0, ButtonWidth, ButtonHeight);
        _close.SetBounds(Width - ButtonWidth, 0, ButtonWidth, ButtonHeight);
        Invalidate();
    }

    private void ToggleMaximize()
    {
        if (!_showMaximizeButton || FindTarget() is not { } form) return;
        if (MaximizeRequested is not null)
        {
            MaximizeRequested.Invoke(this, EventArgs.Empty);
            return;
        }
        form.WindowState = form.WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal : FormWindowState.Maximized;
        SyncMaximizeIcon();
    }

    private void TargetResized(object? sender, EventArgs e) => SyncMaximizeIcon();

    private void SyncMaximizeIcon()
    {
        if (_targetForm is null || _targetForm.IsDisposed) return;
        bool maximized = _maximizedOverride ?? (_targetForm.WindowState == FormWindowState.Maximized);
        _maximize.IconChar = maximized ? IconChar.WindowRestore : IconChar.WindowMaximize;
        _maximize.AccessibleName = maximized ? "Restore window" : "Maximize window";
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (!IsDisposed && !Disposing) ApplyTheme();
    }

    /// <summary>Match the surface under a floating caption control.</summary>
    public void SetSurfaceColor(Color surface)
    {
        if (_surfaceOverride == surface) return;
        _surfaceOverride = surface;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var c = Theme.Colors;
        _surface = _surfaceOverride ?? c.Surface;
        _hover = c.SurfaceAlt;
        _danger = c.Danger;
        BackColor = _surface;
        foreach (var b in new[] { _minimize, _maximize, _close })
        {
            b.BackColor = _surface;
            b.IconColor = c.TextMuted;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = _surface;
            b.FlatAppearance.MouseDownBackColor = _surface;
        }
        _close.FlatAppearance.MouseOverBackColor = _surface;
    }

    private void HoverButton(IconButton button, bool entering)
    {
        if (IsDisposed) return;
        button.BackColor = _surface;
        button.IconColor = entering ? (button == _close ? _danger : Theme.Colors.Text) : Theme.Colors.TextMuted;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= OnThemeChanged;
            if (_targetForm is not null && !_targetForm.IsDisposed)
                _targetForm.Resize -= TargetResized;
        }
        base.Dispose(disposing);
    }
}

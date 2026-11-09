using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

// Base class for every screen and custom control: applies the theme on creation and when it changes.
public abstract class ThemedUserControl : UserControl
{
    protected ThemedUserControl()
    {
        DoubleBuffered = true;
        Theme.Changed += OnThemeChanged;
    }

    protected virtual void ApplyTheme()
    {
        BackColor = Theme.Colors.Background;
        ForeColor = Theme.Colors.Text;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTheme();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsHandleCreated) ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Theme.Changed -= OnThemeChanged;
        base.Dispose(disposing);
    }
}
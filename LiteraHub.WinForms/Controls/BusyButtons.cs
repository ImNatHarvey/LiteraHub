using System.ComponentModel;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

// Accent button that shows a spinner while an async action runs and ignores clicks until it ends.
public sealed class BusyButton : Button
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 50 };
    private float _angle;
    private bool _busy;

    public BusyButton()
    {
        FlatStyle = FlatStyle.Flat;
        Cursor = Cursors.Hand;
        TextAlign = ContentAlignment.MiddleCenter;
        _timer.Tick += (_, _) =>
        {
            _angle = (_angle + 30f) % 360f;
            Invalidate();
        };
        Theme.Changed += OnThemeChanged;
        ApplyTheme();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsBusy => _busy;

    public async Task RunAsync(Func<Task> action, string busyText = "Working...")
    {
        if (_busy) return;

        var idleText = Text;
        _busy = true;
        Text = busyText;
        Cursor = Cursors.AppStarting;
        _timer.Start();

        try
        {
            await action();
        }
        finally
        {
            _timer.Stop();
            if (!IsDisposed)
            {
                _busy = false;
                Text = idleText;
                Cursor = Cursors.Hand;
                Invalidate();
            }
        }
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        base.OnPaint(pevent);
        if (!_busy) return;

        var scale = DeviceDpi / 96f;
        var size = (int)(16 * scale);
        var bounds = new Rectangle((int)(Space.M * scale), (Height - size) / 2, size, size);
        Gfx.DrawSpinner(pevent.Graphics, bounds, _angle, ForeColor, 2f * scale);
    }

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTheme();

    private void ApplyTheme()
    {
        var c = Theme.Colors;
        BackColor = c.Accent;
        ForeColor = c.OnAccent;
        Font = Theme.BodyStrong;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Gfx.Mix(c.Accent, c.Text, 0.85f);
        FlatAppearance.MouseDownBackColor = Gfx.Mix(c.Accent, c.Text, 0.75f);
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= OnThemeChanged;
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }
}
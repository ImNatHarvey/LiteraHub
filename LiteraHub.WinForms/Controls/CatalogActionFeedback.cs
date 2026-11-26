using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>
/// Non-modal, theme-aware success feedback. Attaches to the shell Form so
/// messages survive a details page closing (e.g. after a successful delete).
/// No messages are shown for failed or cancelled operations.
/// </summary>
public sealed class CatalogActionFeedback : ThemedUserControl
{
    private const int ToastHeight = 54;
    private const int ToastMaxWidth = 350;
    private readonly System.Windows.Forms.Timer _hideTimer = new() { Interval = 3200 };
    private readonly Label _symbol = new()
    {
        Text = "✓",
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleCenter,
        Margin = Padding.Empty
    };
    private readonly Label _message = new()
    {
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true,
        Margin = Padding.Empty
    };

    private Form? _owner;

    private CatalogActionFeedback()
    {
        Size = new Size(ToastMaxWidth, ToastHeight);
        Visible = false;
        TabStop = false;
        AccessibleRole = AccessibleRole.StatusBar;
        Controls.Add(_symbol);
        Controls.Add(_message);
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            Visible = false;
        };
        Resize += (_, _) => PositionChildren();
        PositionChildren();
    }

    /// <summary>Show or replace the current success message in the owning window.</summary>
    public static void Notify(Control source, string message)
    {
        if (source.IsDisposed || source.Disposing || string.IsNullOrWhiteSpace(message))
            return;

        var form = source.FindForm();
        if (form is null || form.IsDisposed || form.Disposing)
            return;

        var toast = form.Controls.OfType<CatalogActionFeedback>().FirstOrDefault();
        if (toast is null)
        {
            toast = new CatalogActionFeedback();
            toast._owner = form;
            form.Controls.Add(toast);
            form.Resize += toast.HandleOwnerResize;
        }

        toast._hideTimer.Stop();
        toast._message.Text = message;
        toast.AccessibleName = message;
        toast.ApplyTheme();
        toast.PlaceInOwner();
        toast.Visible = true;
        toast.BringToFront();
        toast._hideTimer.Start();
    }

    private void HandleOwnerResize(object? sender, EventArgs e) => PlaceInOwner();

    private void PlaceInOwner()
    {
        if (_owner is null || _owner.IsDisposed) return;
        int width = Math.Min(ToastMaxWidth, Math.Max(180, _owner.ClientSize.Width - 32));
        Bounds = new Rectangle(
            Math.Max(0, _owner.ClientSize.Width - width - 20),
            72,
            width,
            ToastHeight);
    }

    private void PositionChildren()
    {
        _symbol.SetBounds(12, 8, 38, Height - 16);
        _message.SetBounds(55, 6, Math.Max(0, Width - 68), Height - 12);
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        BackColor = c.Surface;
        _symbol.BackColor = c.Surface;
        _symbol.ForeColor = Color.FromArgb(46, 172, 111);
        _symbol.Font = Theme.Title;
        _message.BackColor = c.Surface;
        _message.ForeColor = c.Text;
        _message.Font = Theme.BodyStrong;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        ThemedDialogChrome.DrawFrame(e.Graphics, ClientRectangle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hideTimer.Stop();
            _hideTimer.Dispose();
            if (_owner is not null && !_owner.IsDisposed)
                _owner.Resize -= HandleOwnerResize;
            _owner = null;
        }
        base.Dispose(disposing);
    }
}

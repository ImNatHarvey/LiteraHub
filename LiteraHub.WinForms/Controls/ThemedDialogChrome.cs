using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>Shared border, accent stripe and draggable title treatment for dialogs and feedback.</summary>
public static class ThemedDialogChrome
{
    public const int AccentWidth = 4;

    public static void DrawFrame(Graphics graphics, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var border = new SolidBrush(Theme.Colors.Border);
        using var accent = new SolidBrush(Theme.Colors.Accent);
        graphics.FillRectangle(border, bounds.Left, bounds.Top, bounds.Width, 1);
        graphics.FillRectangle(border, bounds.Left, bounds.Bottom - 1, bounds.Width, 1);
        graphics.FillRectangle(border, bounds.Right - 1, bounds.Top, 1, bounds.Height);
        graphics.FillRectangle(accent, bounds.Left, bounds.Top, Math.Min(AccentWidth, bounds.Width), bounds.Height);
    }

    public static void Attach(Form form, Control title, Control? extraDragSurface = null,
        bool addCloseButton = false)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(title);
        form.FormBorderStyle = FormBorderStyle.None;
        // Reserve the painted frame so dock-filled children cannot obscure it.
        var p = form.Padding;
        form.Padding = new Padding(Math.Max(AccentWidth, p.Left), Math.Max(1, p.Top),
            Math.Max(1, p.Right), Math.Max(1, p.Bottom));
        form.Paint += (_, e) => DrawFrame(e.Graphics, form.ClientRectangle);
        form.Resize += (_, _) => form.Invalidate();
        void Drag(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || !form.IsHandleCreated) return;
            ReleaseCapture();
            SendMessage(form.Handle, 0x00A1, (IntPtr)2, IntPtr.Zero);
        }
        title.MouseDown += Drag;
        if (extraDragSurface is not null && extraDragSurface != title)
            extraDragSurface.MouseDown += Drag;
        ThemedWindowControls? close = null;
        if (addCloseButton)
        {
            close = new ThemedWindowControls { ShowMinimizeButton = false, ShowMaximizeButton = false };
            close.Bind(form);
            close.SetSurfaceColor(Theme.Colors.Background);
            form.Controls.Add(close);
            void Place()
            {
                close.Location = new Point(Math.Max(AccentWidth, form.ClientSize.Width - close.Width - 12), 10);
                close.BringToFront();
            }
            Place();
            form.Resize += (_, _) => Place();
            form.Shown += (_, _) => Place();
        }
        void OnThemeChanged(object? sender, EventArgs e)
        {
            if (form.IsDisposed || form.Disposing) return;
            close?.SetSurfaceColor(Theme.Colors.Background);
            form.Invalidate();
        }
        Theme.Changed += OnThemeChanged;
        form.Disposed += (_, _) => Theme.Changed -= OnThemeChanged;
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}

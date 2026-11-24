using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>
/// Reusable geometry and theme surface for a details-page action footer.
/// The hosting page owns button click handlers, visibility, permissions,
/// accessible names, icon painting, and button color policies.
/// </summary>
public sealed class DetailsActionFooter : Panel
{
    private Button? _delete;
    private Button? _archive;
    private Button? _primary;
    private Button? _cancel;
    private Func<bool>? _newRecord;
    private Func<bool>? _showCancel;
    private bool _arranging;

    // Student Details has a narrower three-action layout below 690px.
    // Default false preserves the accepted Author/Category geometry.
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool UseStudentSizing { get; set; }

    // Book Editor stacks Cancel on a dedicated row at widths below 700px.
    // This mode is opt-in and does not alter accepted Author/Category/Student layouts.
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool UseBookSizing { get; set; }

    public DetailsActionFooter()
    {
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        BackColor = Theme.Colors.Background;
        Theme.Changed += HandleThemeChanged;
    }

    public void Configure(Button delete, Button archive, Button primary,
        Button cancel, Func<bool> newRecord, Func<bool>? showCancel = null)
    {
        _delete = delete ?? throw new ArgumentNullException(nameof(delete));
        _archive = archive ?? throw new ArgumentNullException(nameof(archive));
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
        _newRecord = newRecord ?? throw new ArgumentNullException(nameof(newRecord));
        _showCancel = showCancel;
        ArrangeActions();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ArrangeActions();
    }

    public void ArrangeActions()
    {
        if (_arranging || IsDisposed || _delete is null || _archive is null ||
            _primary is null || _cancel is null || _newRecord is null)
            return;

        int width = ClientSize.Width;
        if (width < 1) return;

        _arranging = true;
        try
        {
            if (UseBookSizing)
            {
                ArrangeBookActions(width);
                return;
            }

            const int gap = 8;
            const int height = 40;
            const int top = 8;
            int actionWidth = UseStudentSizing && width < 690
                ? Math.Max(70, (width - 2 * gap) / 3)
                : Math.Min(210, Math.Max(110,
                    (width * 2 / 3 - 2 * gap) / 3));
            int left = Math.Max(0,
                width - (actionWidth * 3 + gap * 2));

            // Keep original slots even if an action is hidden by permissions.
            _delete.SetBounds(left, top, actionWidth, height);
            _archive.SetBounds(left + actionWidth + gap, top, actionWidth, height);
            _primary.SetBounds(left + (actionWidth + gap) * 2, top,
                actionWidth, height);
            _cancel.SetBounds(0, top,
                Math.Min(115, Math.Max(80, left - gap)), height);

            if (_newRecord())
                _primary.SetBounds(Math.Max(0, width - 160), top,
                    Math.Min(160, width), height);
        }
        finally
        {
            _arranging = false;
        }
    }

    private void ArrangeBookActions(int fullWidth)
    {
        // Exactly the original BookEditorPage footer geometry.
        // Permission, busy, and save state continue to belong to the page.
        if (_delete is null || _archive is null || _primary is null ||
            _cancel is null || _newRecord is null)
            return;

        const int gap = 8;
        const int height = 40;
        bool portrait = fullWidth < 700;
        bool showCancel = _showCancel?.Invoke() ?? false;
        int actionY = portrait && showCancel ? 54 : 8;

        if (_newRecord())
        {
            _cancel.Visible = false;
            _primary.SetBounds(
                Math.Max(0, fullWidth - (portrait ? fullWidth : 160)),
                actionY, portrait ? fullWidth : 160, height);
            return;
        }

        int actionWidth = portrait
            ? Math.Max(70, (fullWidth - 2 * gap) / 3)
            : Math.Min(210, Math.Max(110,
                (fullWidth * 2 / 3 - 2 * gap) / 3));
        int left = Math.Max(0, fullWidth - actionWidth * 3 - 2 * gap);

        _delete.SetBounds(left, actionY, actionWidth, height);
        _archive.SetBounds(left + actionWidth + gap, actionY, actionWidth, height);
        _primary.SetBounds(left + (actionWidth + gap) * 2,
            actionY, actionWidth, height);

        int cancelWidth = portrait
            ? Math.Min(115, fullWidth)
            : Math.Min(115, Math.Max(80, left - gap));
        _cancel.SetBounds(0, portrait ? 4 : 8, cancelWidth, height);
        _cancel.Visible = showCancel;
    }

    private void HandleThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || Disposing) return;
        BackColor = Theme.Colors.Background;
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Theme.Changed -= HandleThemeChanged;
        base.Dispose(disposing);
    }
}

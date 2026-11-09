
using System;
using System.Drawing;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;
using System.ComponentModel;

namespace LiteraHub.WinForms.Controls;

public sealed class ThemedDataGridView : DataGridView
{
    private bool _readOnlyDisplayMode;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ReadOnlyDisplayMode
    {
        get => _readOnlyDisplayMode;
        set
        {
            _readOnlyDisplayMode = value;
            TabStop = !value;
            ApplyTheme();

            if (value)
                ClearRowSelection();
        }
    }

    public ThemedDataGridView()
    {
        DoubleBuffered = true;
        ReadOnly = true;
        MultiSelect = false;
        SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;

        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        RowHeadersVisible = false;
        AutoGenerateColumns = false;

        Dock = DockStyle.Fill;
        BorderStyle = BorderStyle.None;
        CellBorderStyle =
            DataGridViewCellBorderStyle.SingleHorizontal;
        ColumnHeadersBorderStyle =
            DataGridViewHeaderBorderStyle.None;
        EnableHeadersVisualStyles = false;

        AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;

        ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        ColumnHeadersHeight = 44;
        RowTemplate.Height = 42;

        EditMode = DataGridViewEditMode.EditProgrammatically;
        ClipboardCopyMode =
            DataGridViewClipboardCopyMode.EnableWithoutHeaderText;

        ShowCellToolTips = true;
        ScrollBars = ScrollBars.Both;
        Font = Theme.Body;

        Theme.Changed += HandleThemeChanged;

        ApplyTheme();
    }

    public T? GetSelectedItem<T>() where T : class
    {
        return CurrentRow?.DataBoundItem as T;
    }

    public void ClearRowSelection()
    {
        ClearSelection();
        CurrentCell = null;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeTheme.ApplyScrollBarTheme(this);
    }

    protected override void OnScroll(ScrollEventArgs e)
    {
        base.OnScroll(e);

        if (_readOnlyDisplayMode && SelectedCells.Count > 0)
            ClearSelection();
    }

    protected override void OnSelectionChanged(EventArgs e)
    {
        base.OnSelectionChanged(e);

        if (_readOnlyDisplayMode && SelectedCells.Count > 0)
            ClearSelection();
    }

    protected override void OnCellMouseDown(
        DataGridViewCellMouseEventArgs e)
    {
        if (_readOnlyDisplayMode && e.RowIndex >= 0)
        {
            ClearRowSelection();
            return;
        }

        base.OnCellMouseDown(e);
    }

    public void ApplyTheme()
    {
        if (IsDisposed || Disposing)
            return;

        var c = Theme.Colors;

        SuspendLayout();

        try
        {
            BackgroundColor = c.Surface;
            BackColor = c.Surface;
            ForeColor = c.Text;
            GridColor = c.Border;

            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = c.Surface,
                ForeColor = c.Text,
                SelectionBackColor = _readOnlyDisplayMode
                    ? c.Surface
                    : c.AccentSoft,
                SelectionForeColor = c.Text,
                Font = Theme.Body,
                Padding = new Padding(10, 4, 10, 4),
                Alignment =
                    DataGridViewContentAlignment.MiddleLeft
            };

            AlternatingRowsDefaultCellStyle =
                new DataGridViewCellStyle
                {
                    BackColor = c.SurfaceAlt,
                    ForeColor = c.Text,
                    SelectionBackColor = _readOnlyDisplayMode
                        ? c.SurfaceAlt
                        : c.AccentSoft,
                    SelectionForeColor = c.Text,
                    Font = Theme.Body
                };

            ColumnHeadersDefaultCellStyle =
                new DataGridViewCellStyle
                {
                    BackColor = c.SurfaceAlt,
                    ForeColor = c.Text,
                    SelectionBackColor = c.SurfaceAlt,
                    SelectionForeColor = c.Text,
                    Font = Theme.BodyStrong,
                    Padding = new Padding(10, 4, 10, 4),
                    Alignment =
                        DataGridViewContentAlignment.MiddleLeft
                };

            RowHeadersDefaultCellStyle =
                new DataGridViewCellStyle
                {
                    BackColor = c.SurfaceAlt,
                    ForeColor = c.Text,
                    SelectionBackColor = c.SurfaceAlt,
                    SelectionForeColor = c.Text
                };

            RowsDefaultCellStyle.BackColor = c.Surface;
            RowsDefaultCellStyle.ForeColor = c.Text;

            if (_readOnlyDisplayMode)
                ClearRowSelection();

            NativeTheme.ApplyScrollBarTheme(this);
            Invalidate();
        }
        finally
        {
            ResumeLayout(true);
        }
    }

    private void HandleThemeChanged(object? sender, EventArgs e)
    {
        ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Theme.Changed -= HandleThemeChanged;

        base.Dispose(disposing);
    }
}

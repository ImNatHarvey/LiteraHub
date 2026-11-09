
using System;
using System.Drawing;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>
/// Reusable, theme-aware table for LiteraHub management screens.
/// Uses native WinForms DataGridView rendering.
/// </summary>
public sealed class ThemedDataGridView : DataGridView
{
    public ThemedDataGridView()
    {
        // Rendering and interaction
        DoubleBuffered = true;
        ReadOnly = true;
        MultiSelect = false;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        RowHeadersVisible = false;
        AutoGenerateColumns = false;

        // Layout
        Dock = DockStyle.Fill;
        BorderStyle = BorderStyle.None;
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        EnableHeadersVisualStyles = false;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        ColumnHeadersHeight = 44;
        RowTemplate.Height = 42;

        // Behavior
        EditMode = DataGridViewEditMode.EditProgrammatically;
        ClipboardCopyMode =
            DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
        ShowCellToolTips = true;
        BackgroundColor = Theme.Colors.Surface;

        Font = Theme.Body;

        Theme.Changed += HandleThemeChanged;
        ApplyTheme();
    }

    /// <summary>
    /// Returns the currently selected data-bound object,
    /// or null when no row is selected.
    /// </summary>
    public T? GetSelectedItem<T>() where T : class
    {
        return CurrentRow?.DataBoundItem as T;
    }

    /// <summary>
    /// Clears any automatic row selection after binding data.
    /// </summary>
    public void ClearRowSelection()
    {
        ClearSelection();
        CurrentCell = null;
    }

    /// <summary>
    /// Applies the current LiteraHub palette to the grid.
    /// </summary>
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
                SelectionBackColor = c.AccentSoft,
                SelectionForeColor = c.Text,
                Font = Theme.Body,
                Padding = new Padding(10, 4, 10, 4),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = c.SurfaceAlt,
                ForeColor = c.Text,
                SelectionBackColor = c.AccentSoft,
                SelectionForeColor = c.Text,
                Font = Theme.Body
            };

            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = c.SurfaceAlt,
                ForeColor = c.Text,
                SelectionBackColor = c.SurfaceAlt,
                SelectionForeColor = c.Text,
                Font = Theme.BodyStrong,
                Padding = new Padding(10, 4, 10, 4),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            RowHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = c.SurfaceAlt,
                ForeColor = c.Text,
                SelectionBackColor = c.AccentSoft,
                SelectionForeColor = c.Text
            };

            RowsDefaultCellStyle.BackColor = c.Surface;
            RowsDefaultCellStyle.ForeColor = c.Text;

            Invalidate();
        }
        finally
        {
            ResumeLayout(performLayout: true);
        }
    }

    private void HandleThemeChanged(object? sender, EventArgs e)
    {
        ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= HandleThemeChanged;
        }

        base.Dispose(disposing);
    }
}

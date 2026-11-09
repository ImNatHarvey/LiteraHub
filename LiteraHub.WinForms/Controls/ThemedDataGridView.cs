
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

public sealed class ThemedDataGridView : DataGridView
{
    private bool _readOnlyDisplayMode;
    private bool _syncingScrollbars;
    private bool _refreshPending;

    private readonly GridScrollBar _verticalBar = new()
    {
        Orientation = Orientation.Vertical,
        Visible = false
    };

    private readonly GridScrollBar _horizontalBar = new()
    {
        Orientation = Orientation.Horizontal,
        Visible = false
    };

    private readonly Panel _corner = new()
    {
        Visible = false
    };

    [DesignerSerializationVisibility(
        DesignerSerializationVisibility.Hidden)]
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

        EditMode =
            DataGridViewEditMode.EditProgrammatically;

        ClipboardCopyMode =
            DataGridViewClipboardCopyMode
                .EnableWithoutHeaderText;

        ShowCellToolTips = true;

        // Keep native scrolling mechanics enabled.
        ScrollBars = ScrollBars.Both;

        Font = Theme.Body;

        Controls.Add(_verticalBar);
        Controls.Add(_horizontalBar);
        Controls.Add(_corner);

        _verticalBar.ValueChanged +=
            HandleVerticalValueChanged;

        _horizontalBar.ValueChanged +=
            HandleHorizontalValueChanged;

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
        QueueScrollbarRefresh();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        QueueScrollbarRefresh();
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        QueueScrollbarRefresh();
    }

    protected override void OnDataBindingComplete(
        DataGridViewBindingCompleteEventArgs e)
    {
        base.OnDataBindingComplete(e);
        QueueScrollbarRefresh();
    }

    protected override void OnRowsAdded(
        DataGridViewRowsAddedEventArgs e)
    {
        base.OnRowsAdded(e);
        QueueScrollbarRefresh();
    }

    protected override void OnRowsRemoved(
        DataGridViewRowsRemovedEventArgs e)
    {
        base.OnRowsRemoved(e);
        QueueScrollbarRefresh();
    }

    protected override void OnColumnAdded(
        DataGridViewColumnEventArgs e)
    {
        base.OnColumnAdded(e);
        QueueScrollbarRefresh();
    }

    protected override void OnColumnRemoved(
        DataGridViewColumnEventArgs e)
    {
        base.OnColumnRemoved(e);
        QueueScrollbarRefresh();
    }

    protected override void OnColumnWidthChanged(
        DataGridViewColumnEventArgs e)
    {
        base.OnColumnWidthChanged(e);
        QueueScrollbarRefresh();
    }

    protected override void OnColumnStateChanged(
        DataGridViewColumnStateChangedEventArgs e)
    {
        base.OnColumnStateChanged(e);
        QueueScrollbarRefresh();
    }

    protected override void OnScroll(ScrollEventArgs e)
    {
        base.OnScroll(e);

        if (_readOnlyDisplayMode &&
            SelectedCells.Count > 0)
        {
            ClearSelection();
        }

        QueueScrollbarRefresh();
    }

    protected override void OnSelectionChanged(EventArgs e)
    {
        base.OnSelectionChanged(e);

        if (_readOnlyDisplayMode &&
            SelectedCells.Count > 0)
        {
            ClearSelection();
        }
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

    private void HandleVerticalValueChanged(
        object? sender, EventArgs e)
    {
        if (_syncingScrollbars || Rows.Count == 0)
            return;

        int target = Math.Clamp(
            _verticalBar.Value,
            0,
            Rows.Count - 1);

        while (target < Rows.Count &&
               !Rows[target].Visible)
        {
            target++;
        }

        if (target >= Rows.Count)
            return;

        try
        {
            FirstDisplayedScrollingRowIndex = target;
        }
        catch (InvalidOperationException)
        {
            // Grid may be resizing or rebinding.
        }
        catch (ArgumentOutOfRangeException)
        {
            // Row positions may have changed.
        }

        QueueScrollbarRefresh();
    }

    private void HandleHorizontalValueChanged(
        object? sender, EventArgs e)
    {
        if (_syncingScrollbars)
            return;

        try
        {
            HorizontalScrollingOffset =
                _horizontalBar.Value;
        }
        catch (ArgumentOutOfRangeException)
        {
            // Columns may have changed.
        }

        QueueScrollbarRefresh();
    }

    private void QueueScrollbarRefresh()
    {
        if (IsDisposed ||
            Disposing ||
            !IsHandleCreated ||
            _refreshPending)
        {
            return;
        }

        _refreshPending = true;

        try
        {
            BeginInvoke((MethodInvoker)(() =>
            {
                _refreshPending = false;

                if (!IsDisposed && !Disposing)
                    RefreshScrollbars();
            }));
        }
        catch (InvalidOperationException)
        {
            _refreshPending = false;
        }
    }

    private void RefreshScrollbars()
    {
        if (IsDisposed ||
            Disposing ||
            !IsHandleCreated ||
            _syncingScrollbars)
        {
            return;
        }

        _syncingScrollbars = true;

        try
        {
            // Locate the real Windows scrollbar controls.
            // Their bounds already account for the grid's
            // headers, viewport, and opposite scrollbar.
            HScrollBar? nativeHorizontal =
                Controls.OfType<HScrollBar>()
                    .FirstOrDefault();

            VScrollBar? nativeVertical =
                Controls.OfType<VScrollBar>()
                    .FirstOrDefault();

            bool needsHorizontal =
                nativeHorizontal is not null &&
                nativeHorizontal.Visible &&
                nativeHorizontal.Width > 0;

            bool needsVertical =
                nativeVertical is not null &&
                nativeVertical.Visible &&
                nativeVertical.Height > 0;

            _horizontalBar.Visible = needsHorizontal;
            _verticalBar.Visible = needsVertical;

            if (needsHorizontal &&
                nativeHorizontal is not null)
            {
                _horizontalBar.Bounds =
                    nativeHorizontal.Bounds;

                int totalWidth = Columns
                    .Cast<DataGridViewColumn>()
                    .Where(column => column.Visible)
                    .Sum(column => column.Width);

                int viewportWidth = Math.Max(
                    1,
                    nativeHorizontal.Width);

                _horizontalBar.SetRange(
                    Math.Max(totalWidth, viewportWidth),
                    viewportWidth);

                _horizontalBar.SetValue(
                    HorizontalScrollingOffset);
            }

            if (needsVertical &&
                nativeVertical is not null)
            {
                _verticalBar.Bounds =
                    nativeVertical.Bounds;

                int totalRows = Rows
                    .Cast<DataGridViewRow>()
                    .Count(row => row.Visible);

                int visibleRows = Math.Max(
                    1,
                    DisplayedRowCount(false));

                _verticalBar.SetRange(
                    Math.Max(totalRows, visibleRows),
                    visibleRows);

                int firstRow =
                    FirstDisplayedScrollingRowIndex;

                _verticalBar.SetValue(
                    Math.Max(0, firstRow));
            }

            _corner.Visible =
                needsHorizontal && needsVertical;

            if (_corner.Visible &&
                nativeHorizontal is not null &&
                nativeVertical is not null)
            {
                _corner.SetBounds(
                    nativeVertical.Left,
                    nativeHorizontal.Top,
                    nativeVertical.Width,
                    nativeHorizontal.Height);
            }

            _corner.BackColor =
                Theme.Colors.SurfaceAlt;

            // Overlay the native scrollbars.
            if (needsHorizontal)
                _horizontalBar.BringToFront();

            if (needsVertical)
                _verticalBar.BringToFront();

            if (_corner.Visible)
                _corner.BringToFront();
        }
        finally
        {
            _syncingScrollbars = false;
        }
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

                SelectionBackColor =
                    _readOnlyDisplayMode
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

                    SelectionBackColor =
                        _readOnlyDisplayMode
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

            _corner.BackColor = c.SurfaceAlt;

            _verticalBar.Invalidate();
            _horizontalBar.Invalidate();

            if (_readOnlyDisplayMode)
                ClearRowSelection();

            Invalidate();
        }
        finally
        {
            ResumeLayout(true);
        }

        QueueScrollbarRefresh();
    }

    private void HandleThemeChanged(
        object? sender, EventArgs e)
    {
        ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= HandleThemeChanged;

            _verticalBar.ValueChanged -=
                HandleVerticalValueChanged;

            _horizontalBar.ValueChanged -=
                HandleHorizontalValueChanged;
        }

        base.Dispose(disposing);
    }
}

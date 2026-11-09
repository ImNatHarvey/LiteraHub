
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

    private const int ScrollbarThickness = 14;

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
            DataGridViewColumnHeadersHeightSizeMode
                .DisableResizing;

        ColumnHeadersHeight = 44;
        RowTemplate.Height = 42;

        EditMode =
            DataGridViewEditMode.EditProgrammatically;

        ClipboardCopyMode =
            DataGridViewClipboardCopyMode
                .EnableWithoutHeaderText;

        ShowCellToolTips = true;

        // Custom bars replace native scrollbar UI.
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

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        if (Rows.Count > 0)
        {
            int steps = Math.Max(
                1, Math.Abs(e.Delta) / 120);

            int direction = e.Delta > 0 ? -1 : 1;

            ScrollRows(direction * steps * 3);
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

    private void ScrollRows(int amount)
    {
        if (Rows.Count == 0)
            return;

        int first = FirstDisplayedScrollingRowIndex;

        if (first < 0)
            first = 0;

        int target = Math.Clamp(
            first + amount,
            0,
            Math.Max(0, Rows.Count - 1));

        SetFirstVisibleRow(target);
    }

    private void SetFirstVisibleRow(int target)
    {
        if (Rows.Count == 0)
            return;

        int index = Math.Clamp(
            target, 0, Rows.Count - 1);

        // Skip rows that cannot be displayed.
        while (index < Rows.Count &&
               !Rows[index].Visible)
        {
            index++;
        }

        if (index >= Rows.Count)
            return;

        try
        {
            FirstDisplayedScrollingRowIndex = index;
        }
        catch (InvalidOperationException)
        {
            // Grid is not ready to scroll.
        }
        catch (ArgumentOutOfRangeException)
        {
            // Data may have changed during scrolling.
        }
    }

    private void HandleVerticalValueChanged(
        object? sender, EventArgs e)
    {
        if (_syncingScrollbars)
            return;

        SetFirstVisibleRow(_verticalBar.Value);
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
            // Column sizes may have changed.
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
            int totalRows = Rows
                .Cast<DataGridViewRow>()
                .Count(row => row.Visible);

            int visibleRows = Math.Max(
                1, DisplayedRowCount(false));

            int totalColumnWidth = Columns
                .Cast<DataGridViewColumn>()
                .Where(column => column.Visible)
                .Sum(column => column.Width);

            int availableWidth = Math.Max(
                1, ClientSize.Width);

            int availableHeight = Math.Max(
                1, ClientSize.Height -
                   ColumnHeadersHeight);

            bool needsVertical =
                totalRows > visibleRows;

            bool needsHorizontal =
                totalColumnWidth > availableWidth;

            // A scrollbar consumes space that may
            // cause the other scrollbar to be needed.
            for (int i = 0; i < 2; i++)
            {
                int effectiveWidth =
                    availableWidth -
                    (needsVertical
                        ? ScrollbarThickness : 0);

                int effectiveHeight =
                    availableHeight -
                    (needsHorizontal
                        ? ScrollbarThickness : 0);

                needsHorizontal =
                    totalColumnWidth > effectiveWidth;

                int rowHeight = Math.Max(
                    1, RowTemplate.Height);

                needsVertical =
                    totalRows * rowHeight >
                    effectiveHeight;
            }

            int barWidth =
                needsVertical
                    ? ScrollbarThickness : 0;

            int barHeight =
                needsHorizontal
                    ? ScrollbarThickness : 0;

            _verticalBar.SetBounds(
                Math.Max(
                    0,
                    ClientSize.Width - ScrollbarThickness),
                0,
                ScrollbarThickness,
                Math.Max(
                    0,
                    ClientSize.Height - barHeight));

            _horizontalBar.SetBounds(
                0,
                Math.Max(
                    0,
                    ClientSize.Height - ScrollbarThickness),
                Math.Max(
                    0,
                    ClientSize.Width - barWidth),
                ScrollbarThickness);

            _corner.SetBounds(
                Math.Max(
                    0,
                    ClientSize.Width - ScrollbarThickness),
                Math.Max(
                    0,
                    ClientSize.Height - ScrollbarThickness),
                ScrollbarThickness,
                ScrollbarThickness);

            _verticalBar.Visible = needsVertical;
            _horizontalBar.Visible = needsHorizontal;

            _corner.Visible =
                needsVertical && needsHorizontal;

            int horizontalViewport = Math.Max(
                1,
                ClientSize.Width - barWidth);

            _horizontalBar.SetRange(
                Math.Max(
                    horizontalViewport,
                    totalColumnWidth),
                horizontalViewport);

            _horizontalBar.SetValue(
                HorizontalScrollingOffset);

            int verticalViewport = Math.Max(
                1,
                DisplayedRowCount(false));

            _verticalBar.SetRange(
                Math.Max(
                    verticalViewport,
                    totalRows),
                verticalViewport);

            int firstRow =
                FirstDisplayedScrollingRowIndex;

            _verticalBar.SetValue(
                Math.Max(0, firstRow));

            _verticalBar.BringToFront();
            _horizontalBar.BringToFront();
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

                Padding = new Padding(
                    10, 4, 10, 4),

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

                    Padding = new Padding(
                        10, 4, 10, 4),

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

            RowsDefaultCellStyle.BackColor =
                c.Surface;

            RowsDefaultCellStyle.ForeColor =
                c.Text;

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

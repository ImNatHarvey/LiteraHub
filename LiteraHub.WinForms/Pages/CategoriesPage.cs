
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

public sealed class CategoriesPage : ThemedUserControl
{
    private readonly ICategoryService _categories;
    private readonly IPermissionService _permissions;

    private readonly TableLayoutPanel _layout = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 5,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };

    private readonly TableLayoutPanel _toolbar = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 3,
        RowCount = 1,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };

    private readonly Label _heading = new()
    {
        Text = "Book Categories",
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly ThemedSearchBox _search = new()
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 4, 8, 4)
    };

    private readonly ThemedFilterDropdown _filter = new()
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 4, 8, 4)
    };

    private readonly Button _addButton = new()
    {
        Text = "+ Add Category",
        Dock = DockStyle.Fill,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 4, 0, 4)
    };

    private readonly Label _status = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };

    private readonly ThemedDataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        Margin = Padding.Empty
    };

    private readonly LoadingHost _loading = new()
    {
        Dock = DockStyle.Fill,
        SkeletonStyle = SkeletonStyle.Rows,
        Margin = Padding.Empty
    };

    private readonly TableLayoutPanel _pagination = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };

    private readonly Label _pageInfo = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };

    private readonly FlowLayoutPanel _pageControls = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.RightToLeft,
        WrapContents = false,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };

    private readonly Button _previous = new()
    {
        Text = "‹",
        Size = new Size(36, 32),
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand
    };

    private readonly Button _next = new()
    {
        Text = "›",
        Size = new Size(36, 32),
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand
    };

    private readonly Label _pageNumber = new()
    {
        Size = new Size(64, 32),
        TextAlign = ContentAlignment.MiddleCenter
    };

    private readonly Label _rowsLabel = new()
    {
        Text = "Rows:",
        AutoSize = false,
        Size = new Size(42, 32),
        TextAlign = ContentAlignment.MiddleCenter
    };

    private readonly ThemedFilterDropdown _pageSizeFilter =
        new(new[] { "10", "25", "50", "100" })
        {
            Size = new Size(68, 32)
        };

    private readonly ToolTip _actionTooltip = new()
    {
        InitialDelay = 400,
        ReshowDelay = 100,
        AutoPopDelay = 3000,
        ShowAlways = true
    };

    private readonly System.Windows.Forms.Timer _hoverTimer = new()
    {
        Interval = 450
    };

    private List<Category> _allCategories = new();

    private int _page = 1;
    private int _pageSize = 10;
    private int _filteredCount;

    private int _hoverRow = -1;
    private int _hoverSlot = -1;
    private int _hoverColumn = -1;

    private bool _loadStarted;
    private bool _loadingData;
    private bool _working;
    private bool _compact;
    private bool _layoutInitialized;
    private bool _hasLoadedData;
    private bool _loadFailed;

    private bool CanManage =>
        _permissions.Can(Permission.ManageLookups);

    private bool CanDelete =>
        CanManage &&
        _permissions.Can(Permission.HardDelete);

    private int ActionCount => CanDelete ? 3 : 2;

    private int TotalPages =>
        Math.Max(
            1,
            (_filteredCount + _pageSize - 1) / _pageSize);

    private static Color SuccessColor =>
        Theme.Colors.PillGoodText;

    public CategoriesPage(
        ICategoryService categories,
        IPermissionService permissions)
    {
        _categories = categories ??
            throw new ArgumentNullException(
                nameof(categories));

        _permissions = permissions ??
            throw new ArgumentNullException(
                nameof(permissions));

        Dock = DockStyle.Fill;
        Padding = new Padding(Space.L);

        BuildLayout();
        ConfigureGrid();

        _loading.SetContent(_grid);

        _filter.Visible = CanManage;

        _addButton.Visible =
            _permissions.Can(Permission.CreateLookups);

        _search.Placeholder = "Search categories...";

        _search.TextChanged += (_, _) =>
        {
            _page = 1;
            FilterCategories();
        };

        _filter.SelectedIndexChanged += (_, _) =>
        {
            _page = 1;
            FilterCategories();
        };

        _pageSizeFilter.SelectedIndexChanged += (_, _) =>
        {
            _pageSize = _pageSizeFilter.SelectedIndex switch
            {
                1 => 25,
                2 => 50,
                3 => 100,
                _ => 10
            };

            _page = 1;
            FilterCategories();
        };

        _previous.Click += (_, _) =>
        {
            if (_loadingData || _working || _page <= 1)
                return;

            _page--;
            FilterCategories();
        };

        _next.Click += (_, _) =>
        {
            if (_loadingData ||
                _working ||
                _page >= TotalPages)
                return;

            _page++;
            FilterCategories();
        };

        _addButton.Click += async (_, _) =>
            await OpenDialogAsync(null);

        _grid.CellPainting += PaintSpecialCells;
        _grid.CellMouseUp += HandleActionMouseUp;

        // Use actual mouse coordinates for each icon.
        _grid.MouseMove += HandleGridMouseMove;
        _grid.MouseLeave += (_, _) => ClearActionHover();
        _grid.Scroll += (_, _) => ClearActionHover();

        _grid.ShowCellToolTips = false;

        _hoverTimer.Tick += HandleHoverTimerTick;

        _status.Text = "Waiting to load categories...";

        Resize += (_, _) => UpdateResponsiveLayout();

        UpdateResponsiveLayout();
    }

    private void BuildLayout()
    {
        _layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 50));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 56));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 36));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _pagination.Padding = new Padding(0, 12, 0, 0);

        _pageInfo.TextAlign = ContentAlignment.MiddleLeft;
        _pageInfo.Dock = DockStyle.Fill;

        _pageControls.Dock = DockStyle.Fill;
        _pageControls.Padding = Padding.Empty;

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 60));

        _toolbar.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _pagination.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _pagination.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 300));

        _pagination.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _previous.Margin = new Padding(4, 8, 0, 0);
        _next.Margin = new Padding(4, 8, 0, 0);
        _pageNumber.Margin = new Padding(4, 8, 0, 0);
        _rowsLabel.Margin = new Padding(4, 8, 0, 0);
        _pageSizeFilter.Margin = new Padding(4, 8, 0, 0);

        _pageControls.Controls.Add(_next);
        _pageControls.Controls.Add(_pageNumber);
        _pageControls.Controls.Add(_previous);
        _pageControls.Controls.Add(_pageSizeFilter);
        _pageControls.Controls.Add(_rowsLabel);

        _pagination.Controls.Add(_pageInfo, 0, 0);
        _pagination.Controls.Add(_pageControls, 1, 0);

        _layout.Controls.Add(_heading, 0, 0);
        _layout.Controls.Add(_toolbar, 0, 1);
        _layout.Controls.Add(_status, 0, 2);
        _layout.Controls.Add(_loading, 0, 3);
        _layout.Controls.Add(_pagination, 0, 4);

        Controls.Add(_layout);
    }

    private void ConfigureGrid()
    {
        _grid.ReadOnlyDisplayMode = true;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;
        _grid.AutoGenerateColumns = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Name",
            HeaderText = "Category Name",
            DataPropertyName = nameof(Category.Name),
            FillWeight = 28,
            MinimumWidth = 115
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Description",
            HeaderText = "Description",
            DataPropertyName = nameof(Category.Description),
            FillWeight = 46,
            MinimumWidth = 150
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "IsActive",
            HeaderText = "Active",
            DataPropertyName = nameof(Category.IsActive),
            FillWeight = 10,
            MinimumWidth = 75,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Actions",
            HeaderText = "Actions",
            FillWeight = 16,
            MinimumWidth = CanDelete ? 132 : 105,
            Visible = CanManage,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        foreach (string name in new[] { "IsActive", "Actions" })
        {
            var column = _grid.Columns[name]!;

            column.HeaderCell.Style.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            column.DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            column.HeaderCell.Style.Padding = Padding.Empty;
            column.DefaultCellStyle.Padding = Padding.Empty;
        }

        _grid.DataBindingComplete += (_, _) =>
            _grid.ClearRowSelection();
    }

    private void FilterCategories()
    {
        if (IsDisposed || Disposing || _loadingData)
            return;

        string query = _search.Text.Trim();

        int status = CanManage
            ? _filter.SelectedIndex
            : 1;

        var filtered = _allCategories.Where(c =>
            (status == 0 ||
             (status == 1 && c.IsActive) ||
             (status == 2 && !c.IsActive)) &&
            (query.Length == 0 ||
             c.Name.Contains(
                 query,
                 StringComparison.OrdinalIgnoreCase) ||
             (c.Description?.Contains(
                 query,
                 StringComparison.OrdinalIgnoreCase)
                 ?? false)))
            .ToList();

        _filteredCount = filtered.Count;

        _page = Math.Clamp(
            _page,
            1,
            TotalPages);

        var pageRows = filtered
            .Skip((_page - 1) * _pageSize)
            .Take(_pageSize)
            .ToList();

        ClearActionHover();

        _grid.DataSource = pageRows;
        _grid.ClearRowSelection();

        _status.Text = _filteredCount == 0
            ? "No matching categories."
            : $"{_filteredCount} of " +
              $"{_allCategories.Count} categories match.";

        int start = _filteredCount == 0
            ? 0
            : (_page - 1) * _pageSize + 1;

        int end = Math.Min(
            _page * _pageSize,
            _filteredCount);

        _pageInfo.Text =
            $"Showing {start}–{end} of {_filteredCount}";

        UpdatePaginationButtons();
    }

    private void UpdatePaginationButtons()
    {
        if (IsDisposed || Disposing)
            return;

        _pageNumber.Text = $"{_page} / {TotalPages}";

        bool ready =
            _hasLoadedData &&
            !_loadFailed &&
            !_working &&
            !_loadingData;

        _previous.Enabled = ready && _page > 1;
        _next.Enabled = ready && _page < TotalPages;

        _pageSizeFilter.Enabled = ready;
    }

    private async Task OpenDialogAsync(Category? existing)
    {
        if (_working || _loadingData)
            return;

        if (existing is null &&
            !_permissions.Can(Permission.CreateLookups))
            return;

        if (existing is not null && !CanManage)
            return;

        _working = true;
        _addButton.Enabled = false;

        UpdatePaginationButtons();

        try
        {
            using var dialog = new CategoryDialog(
                _categories,
                existing);

            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                await LoadCategoriesAsync();
        }
        finally
        {
            _working = false;

            if (!IsDisposed && !Disposing)
            {
                _addButton.Enabled =
                    _permissions.Can(Permission.CreateLookups);

                UpdatePaginationButtons();
            }
        }
    }

    private async Task ChangeStatusAsync(Category category)
    {
        if (_working || _loadingData || !CanManage)
            return;

        bool activate = !category.IsActive;

        string title = activate
            ? "Reactivate Category"
            : "Deactivate Category";

        string action = activate
            ? "reactivate"
            : "deactivate";

        bool confirmed = ThemedMessageDialog.Confirm(
            FindForm(),
            title,
            $"Do you want to {action} '{category.Name}'?",
            danger: !activate,
            confirmText: activate
                ? "Reactivate"
                : "Deactivate");

        if (!confirmed)
            return;

        await ExecuteMutationAsync(
            () => _categories.SetActiveAsync(
                category.CategoryId,
                activate),
            title);
    }

    private async Task DeleteCategoryAsync(Category category)
    {
        if (_working || _loadingData || !CanDelete)
            return;

        bool confirmed = ThemedMessageDialog.Confirm(
            FindForm(),
            "Permanently Delete Category",
            $"Permanently delete '{category.Name}'?\n\n" +
            "This cannot be undone. Categories referenced " +
            "by books cannot be deleted.",
            danger: true,
            confirmText: "Delete");

        if (!confirmed)
            return;

        await ExecuteMutationAsync(
            () => _categories.DeleteAsync(
                category.CategoryId),
            "Delete Category");
    }

    private async Task ExecuteMutationAsync(
        Func<Task<ServiceResult>> action,
        string title)
    {
        _working = true;

        _grid.Enabled = false;
        _addButton.Enabled = false;
        _status.Text = "Saving changes...";

        UpdatePaginationButtons();

        try
        {
            var result = await action();

            if (IsDisposed || Disposing)
                return;

            if (result.Success)
            {
                await LoadCategoriesAsync();
            }
            else
            {
                ThemedMessageDialog.ShowAlert(
                    FindForm(),
                    title,
                    result.Error ?? "Operation failed.",
                    danger: true);
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
            {
                ThemedMessageDialog.ShowAlert(
                    FindForm(),
                    title,
                    ex.Message,
                    danger: true);
            }
        }
        finally
        {
            _working = false;

            if (!IsDisposed && !Disposing)
            {
                _grid.Enabled = true;

                _addButton.Enabled =
                    _permissions.Can(Permission.CreateLookups);

                // Important: update after _working becomes false.
                if (!_loadFailed)
                    FilterCategories();

                UpdatePaginationButtons();
            }
        }
    }

    private int GetActionSlot(
        int cellX,
        int columnWidth)
    {
        if (columnWidth <= 0 ||
            cellX < 0 ||
            cellX >= columnWidth)
            return -1;

        return Math.Clamp(
            cellX * ActionCount / columnWidth,
            0,
            ActionCount - 1);
    }

    private async void HandleActionMouseUp(
        object? sender,
        DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left ||
            e.RowIndex < 0 ||
            e.ColumnIndex < 0 ||
            _working ||
            _loadingData ||
            !CanManage)
            return;

        if (_grid.Columns[e.ColumnIndex]?.Name != "Actions")
            return;

        if (_grid.Rows[e.RowIndex].DataBoundItem
            is not Category category)
            return;

        int slot = GetActionSlot(
            e.X,
            _grid.Columns[e.ColumnIndex]!.Width);

        if (slot < 0)
            return;

        ClearActionHover();

        await RunActionAsync(category, slot);
    }

    private async Task RunActionAsync(
        Category category,
        int slot)
    {
        switch (slot)
        {
            case 0:
                await OpenDialogAsync(category);
                break;

            case 1:
                await ChangeStatusAsync(category);
                break;

            case 2 when CanDelete:
                await DeleteCategoryAsync(category);
                break;
        }
    }

    private void HandleGridMouseMove(
        object? sender,
        MouseEventArgs e)
    {
        if (_working ||
            _loadingData ||
            !CanManage)
        {
            ClearActionHover();
            return;
        }

        var hit = _grid.HitTest(e.X, e.Y);

        if (hit.Type != DataGridViewHitTestType.Cell ||
            hit.RowIndex < 0 ||
            hit.ColumnIndex < 0 ||
            _grid.Columns[hit.ColumnIndex]?.Name != "Actions")
        {
            ClearActionHover();
            return;
        }

        if (_grid.Rows[hit.RowIndex].DataBoundItem
            is not Category)
        {
            ClearActionHover();
            return;
        }

        Rectangle rect = _grid.GetCellDisplayRectangle(
            hit.ColumnIndex,
            hit.RowIndex,
            false);

        int slot = GetActionSlot(
            e.X - rect.Left,
            rect.Width);

        if (slot < 0)
        {
            ClearActionHover();
            return;
        }

        _grid.Cursor = Cursors.Hand;

        if (_hoverRow == hit.RowIndex &&
            _hoverColumn == hit.ColumnIndex &&
            _hoverSlot == slot)
            return;

        _actionTooltip.Hide(_grid);
        _hoverTimer.Stop();

        _hoverRow = hit.RowIndex;
        _hoverColumn = hit.ColumnIndex;
        _hoverSlot = slot;

        _hoverTimer.Start();
    }

    private void HandleHoverTimerTick(
        object? sender,
        EventArgs e)
    {
        _hoverTimer.Stop();

        if (_hoverRow < 0 ||
            _hoverColumn < 0 ||
            _hoverSlot < 0 ||
            _working ||
            _loadingData ||
            IsDisposed ||
            Disposing)
            return;

        Point mouse = _grid.PointToClient(
            Control.MousePosition);

        var hit = _grid.HitTest(mouse.X, mouse.Y);

        if (hit.RowIndex != _hoverRow ||
            hit.ColumnIndex != _hoverColumn)
            return;

        if (_grid.Rows[_hoverRow].DataBoundItem
            is not Category category)
            return;

        Rectangle rect = _grid.GetCellDisplayRectangle(
            _hoverColumn,
            _hoverRow,
            false);

        int currentSlot = GetActionSlot(
            mouse.X - rect.Left,
            rect.Width);

        if (currentSlot != _hoverSlot)
            return;

        string text = _hoverSlot switch
        {
            0 => "Edit",
            1 => category.IsActive
                ? "Archive"
                : "Restore",
            2 when CanDelete => "Delete",
            _ => ""
        };

        if (text.Length == 0)
            return;

        int centerX = rect.Left +
            (int)((_hoverSlot + 0.5) *
                rect.Width / ActionCount);

        _actionTooltip.Show(
            text,
            _grid,
            centerX,
            rect.Bottom + 3,
            2500);
    }

    private void ClearActionHover()
    {
        _hoverTimer.Stop();

        _actionTooltip.Hide(_grid);

        _hoverRow = -1;
        _hoverColumn = -1;
        _hoverSlot = -1;

        if (!IsDisposed && !Disposing)
            _grid.Cursor = Cursors.Default;
    }

    private void PaintSpecialCells(
        object? sender,
        DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 ||
            e.ColumnIndex < 0)
            return;

        string? column =
            _grid.Columns[e.ColumnIndex]?.Name;

        if (column != "IsActive" &&
            column != "Actions")
            return;

        if (_grid.Rows[e.RowIndex].DataBoundItem
            is not Category category)
            return;

        e.PaintBackground(e.CellBounds, false);

        var g = e.Graphics;

        if (g is null)
            return;

        var c = Theme.Colors;
        var oldMode = g.SmoothingMode;

        g.SmoothingMode = SmoothingMode.AntiAlias;

        int cy = e.CellBounds.Top +
            e.CellBounds.Height / 2;

        if (column == "IsActive")
        {
            int cx = e.CellBounds.Left +
                e.CellBounds.Width / 2;

            using var pen = new Pen(
                category.IsActive
                    ? SuccessColor
                    : c.Danger,
                2f);

            g.DrawEllipse(
                pen,
                cx - 9,
                cy - 9,
                18,
                18);

            if (category.IsActive)
            {
                g.DrawLines(pen, new[]
                {
                    new Point(cx - 5, cy),
                    new Point(cx - 1, cy + 4),
                    new Point(cx + 6, cy - 4)
                });
            }
            else
            {
                g.DrawLine(
                    pen,
                    cx - 4,
                    cy,
                    cx + 4,
                    cy);
            }
        }
        else
        {
            for (int slot = 0;
                 slot < ActionCount;
                 slot++)
            {
                int cx = e.CellBounds.Left +
                    (int)((slot + 0.5) *
                    e.CellBounds.Width / ActionCount);

                if (slot == 0)
                {
                    using var pen =
                        new Pen(SuccessColor, 2f);

                    // Edit icon.
                    g.DrawLine(
                        pen,
                        cx - 8, cy - 7,
                        cx + 1, cy - 7);

                    g.DrawLine(
                        pen,
                        cx - 8, cy - 7,
                        cx - 8, cy + 8);

                    g.DrawLine(
                        pen,
                        cx - 8, cy + 8,
                        cx + 7, cy + 8);

                    g.DrawLine(
                        pen,
                        cx + 7, cy + 8,
                        cx + 7, cy + 1);

                    g.DrawLine(
                        pen,
                        cx - 2, cy + 3,
                        cx + 8, cy - 7);

                    g.DrawLine(
                        pen,
                        cx - 3, cy + 5,
                        cx - 2, cy + 3);
                }
                else if (slot == 1)
                {
                    using var pen = new Pen(
                        category.IsActive
                            ? c.TextMuted
                            : SuccessColor,
                        2f);

                    // Archive / restore icon.
                    g.DrawRectangle(
                        pen,
                        cx - 8,
                        cy - 4,
                        16,
                        12);

                    g.DrawLine(
                        pen,
                        cx - 10, cy - 8,
                        cx + 10, cy - 8);

                    if (category.IsActive)
                    {
                        g.DrawLine(
                            pen,
                            cx, cy - 2,
                            cx, cy + 4);

                        g.DrawLine(
                            pen,
                            cx - 4, cy + 1,
                            cx, cy + 5);

                        g.DrawLine(
                            pen,
                            cx, cy + 5,
                            cx + 4, cy + 1);
                    }
                    else
                    {
                        g.DrawLine(
                            pen,
                            cx, cy + 5,
                            cx, cy - 2);

                        g.DrawLine(
                            pen,
                            cx - 4, cy + 1,
                            cx, cy - 3);

                        g.DrawLine(
                            pen,
                            cx, cy - 3,
                            cx + 4, cy + 1);
                    }
                }
                else
                {
                    using var pen =
                        new Pen(c.Danger, 2f);

                    // Permanent delete icon.
                    g.DrawLine(
                        pen,
                        cx - 9, cy - 6,
                        cx + 9, cy - 6);

                    g.DrawLine(
                        pen,
                        cx - 4, cy - 9,
                        cx + 4, cy - 9);

                    g.DrawRectangle(
                        pen,
                        cx - 7,
                        cy - 3,
                        14,
                        12);

                    g.DrawLine(
                        pen,
                        cx - 3, cy,
                        cx - 3, cy + 6);

                    g.DrawLine(
                        pen,
                        cx + 3, cy,
                        cx + 3, cy + 6);
                }
            }
        }

        g.SmoothingMode = oldMode;
        e.Handled = true;
    }

    private void UpdateResponsiveLayout()
    {
        if (IsDisposed || Disposing)
            return;

        int available = Math.Max(
            100,
            ClientSize.Width - Padding.Horizontal);

        bool compact = available < 550;

        if (_layoutInitialized &&
            _compact == compact)
            return;

        _compact = compact;
        _layoutInitialized = true;

        _toolbar.SuspendLayout();
        _pagination.SuspendLayout();

        try
        {
            _toolbar.Controls.Clear();
            _toolbar.ColumnStyles.Clear();
            _toolbar.RowStyles.Clear();

            _toolbar.ColumnCount = 3;
            _toolbar.RowCount = 1;

            _toolbar.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            _toolbar.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute,
                    compact ? 78 : 125));

            _toolbar.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute,
                    compact ? 44 : 160));

            _toolbar.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            _toolbar.Controls.Add(_search, 0, 0);
            _toolbar.Controls.Add(_filter, 1, 0);
            _toolbar.Controls.Add(_addButton, 2, 0);

            _filter.Visible = CanManage;

            _addButton.Text = compact
                ? "+"
                : "+ Add Category";

            _addButton.AccessibleName = "Add Category";

            _addButton.Font = compact
                ? new Font("Segoe UI", 18f)
                : Theme.BodyStrong;

            _grid.AutoSizeColumnsMode = compact
                ? DataGridViewAutoSizeColumnsMode.None
                : DataGridViewAutoSizeColumnsMode.Fill;

            if (compact)
            {
                _grid.Columns["Name"]!.Width = 140;
                _grid.Columns["Description"]!.Width = 210;
                _grid.Columns["IsActive"]!.Width = 80;
                _grid.Columns["Actions"]!.Width =
                    CanDelete ? 144 : 110;
            }

            _pagination.ColumnStyles[1].Width =
                compact ? 245 : 300;

            _pageInfo.Font = compact
                ? Theme.Caption
                : Theme.Body;

            // All three regions use the same right edge:
            // toolbar, loading/grid, and pagination.
            _toolbar.Margin = Padding.Empty;
            _loading.Margin = Padding.Empty;
            _grid.Margin = Padding.Empty;
            _pagination.Margin = Padding.Empty;
            _pageControls.Margin = Padding.Empty;
            _addButton.Margin = new Padding(0, 4, 0, 4);
        }
        finally
        {
            _pagination.ResumeLayout(true);
            _toolbar.ResumeLayout(true);
        }
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        UpdateResponsiveLayout();

        if (_loadStarted)
            return;

        _loadStarted = true;

        await LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        if (IsDisposed || Disposing || _loadingData)
            return;

        _loadingData = true;
        _loadFailed = false;

        _status.Text = "Loading categories...";
        _grid.Enabled = false;
        _addButton.Enabled = false;

        UpdatePaginationButtons();

        try
        {
            var categories = await _loading.RunAsync(
                () => _categories.GetAllAsync(CanManage));

            if (IsDisposed || Disposing)
                return;

            _allCategories = categories.ToList();

            _hasLoadedData = true;
        }
        catch (Exception ex)
        {
            if (IsDisposed || Disposing)
                return;

            _loadFailed = true;

            _status.Text =
                "Unable to load categories: " + ex.Message;
        }
        finally
        {
            _loadingData = false;

            if (!IsDisposed && !Disposing)
            {
                _grid.Enabled = true;

                _addButton.Enabled =
                    _permissions.Can(Permission.CreateLookups);

                // Populate the table after loading is complete.
                // Pagination is updated again after _working
                // becomes false in CRUD operations.
                if (!_loadFailed)
                    FilterCategories();

                UpdatePaginationButtons();

                _grid.ClearRowSelection();
            }
        }
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();

        var c = Theme.Colors;

        _layout.BackColor = c.Background;
        _toolbar.BackColor = c.Background;
        _pagination.BackColor = c.Background;
        _pageControls.BackColor = c.Background;

        _heading.BackColor = c.Background;
        _heading.ForeColor = c.Text;
        _heading.Font = Theme.Title;

        _status.BackColor = c.Background;
        _status.ForeColor = c.TextMuted;
        _status.Font = Theme.Caption;

        _pageInfo.BackColor = c.Background;
        _pageInfo.ForeColor = c.TextMuted;

        _pageNumber.BackColor = c.Background;
        _pageNumber.ForeColor = c.Text;

        _rowsLabel.BackColor = c.Background;
        _rowsLabel.ForeColor = c.TextMuted;

        foreach (var button in new[]
                 { _previous, _next })
        {
            button.BackColor = c.Surface;
            button.ForeColor = c.Text;
            button.Font = Theme.BodyStrong;
            button.FlatAppearance.BorderColor = c.Border;
            button.FlatAppearance.BorderSize = 1;
        }

        _filter.ApplyTheme();
        _pageSizeFilter.ApplyTheme();

        _addButton.BackColor = c.Accent;
        _addButton.ForeColor = c.OnAccent;
        _addButton.FlatAppearance.BorderSize = 0;

        _grid.ApplyTheme();

        foreach (string name in new[]
                 { "IsActive", "Actions" })
        {
            var column = _grid.Columns[name];

            if (column is null)
                continue;

            column.HeaderCell.Style.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            column.DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            column.HeaderCell.Style.Padding = Padding.Empty;
            column.DefaultCellStyle.Padding = Padding.Empty;
        }

        _grid.ClearRowSelection();
        _grid.Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hoverTimer.Stop();
            _hoverTimer.Dispose();
            _actionTooltip.Dispose();
        }

        base.Dispose(disposing);
    }
}

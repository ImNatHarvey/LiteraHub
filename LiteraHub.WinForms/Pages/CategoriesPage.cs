
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
    private readonly IBookService _books;
    private readonly Panel _host = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private CategoryDetailsPage? _details;
    public event EventHandler<int>? BookRequested;
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

    private readonly ThemedCatalogFilterSort _filterSort =
        new("STATUS")
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

    private readonly CatalogSortButton _sortControl = new();
    private readonly CatalogFilterChips _activeChips = new();

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

    private readonly CatalogEmptyState _emptyState = new();

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
        IBookService books,
        IPermissionService permissions)
    {
        _categories = categories ??
            throw new ArgumentNullException(
                nameof(categories));

        _books = books ?? throw new ArgumentNullException(nameof(books));

        _permissions = permissions ??
            throw new ArgumentNullException(
                nameof(permissions));

        Dock = DockStyle.Fill;
        Padding = new Padding(Space.L);

        BuildLayout();
        ConfigureGrid();

        _loading.SetContent(_grid);
        _loading.Controls.Add(_emptyState);
        _emptyState.BringToFront();
        _emptyState.ActionRequested += (_, _) =>
        {
            if (_loadingData || _loadFailed || !_hasLoadedData) return;

            if (_allCategories.Count == 0)
            {
                if (_permissions.Can(Permission.CreateLookups))
                    OpenCategoryDetails(null);
            }
            else if (!string.IsNullOrWhiteSpace(_search.Text))
            {
                _search.Text = string.Empty;
            }
            else
            {
                _filterSort.ClearFilters();
            }
        };

        _filterSort.Visible = CanManage;

        _addButton.Visible =
            _permissions.Can(Permission.CreateLookups);

        _search.Placeholder = "Search categories...";

        _search.TextChanged += (_, _) =>
        {
            _page = 1;
            FilterCategories();
        };

        _filterSort.SelectionChanged += (_, _) =>
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

        _addButton.Click += (_, _) => OpenCategoryDetails(null);

        _grid.CellPainting += PaintSpecialCells;
        // Native DataGridView tooltips are used for BOTH icon columns.
        // Do not overlay a ToolTip.Show() popup on a grid-managed tooltip:
        // it can linger over the chevron and display the wrong status.
        _grid.ShowCellToolTips = true;
        _grid.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                e.RowIndex >= _grid.Rows.Count ||
                e.ColumnIndex >= _grid.Columns.Count ||
                _grid.Rows[e.RowIndex].DataBoundItem is not Category category)
                return;

            e.ToolTipText = _grid.Columns[e.ColumnIndex].Name switch
            {
                "IsActive" => category.IsActive ? "Active" : "Inactive",
                "Actions" => "View Details",
                _ => string.Empty
            };
        };
        _grid.CellMouseUp += HandleViewClick;

        // Use actual mouse coordinates for each icon.
        _grid.MouseMove += (_, e) =>
        {
            var hit = _grid.HitTest(e.X, e.Y);
            _grid.Cursor = hit.Type == DataGridViewHitTestType.Cell &&
                hit.ColumnIndex >= 0 && _grid.Columns[hit.ColumnIndex].Name == "Actions"
                ? Cursors.Hand : Cursors.Default;
        };
        _grid.MouseLeave += (_, _) => ClearActionHover();
        _grid.Scroll += (_, _) => ClearActionHover();


        _status.Text = "Waiting to load categories...";

        Resize += (_, _) => UpdateResponsiveLayout();
        InitializeUnifiedFilters();

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

        _host.Controls.Add(_layout);
        Controls.Add(_host);
    }

    private void ConfigureGrid()
    {
        _grid.ReadOnlyDisplayMode = true;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;
        _grid.AutoGenerateColumns = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CategoryId",
            HeaderText = "Category ID",
            DataPropertyName = nameof(Category.CategoryId),
            // Reserve enough width for the complete header even with the sidebar expanded.
            FillWeight = 16,
            MinimumWidth = 138,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Name",
            HeaderText = "Category Name",
            DataPropertyName = nameof(Category.Name),
            FillWeight = 26,
            MinimumWidth = 115
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Description",
            HeaderText = "Description",
            DataPropertyName = nameof(Category.Description),
            FillWeight = 45,
            MinimumWidth = 150
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "IsActive",
            HeaderText = "Status",
            DataPropertyName = nameof(Category.IsActive),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 90,
            MinimumWidth = 90,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Actions",
            HeaderText = "",
            // Same fixed-width rightmost View Details column across modules.
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 55,
            MinimumWidth = 55,
            Visible = true,
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
            ? _filterSort.StatusIndex
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

        filtered = _filterSort.SortIndex switch
        {
            1 => filtered.OrderByDescending(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            2 => filtered.OrderBy(c => c.CategoryId).ToList(),
            3 => filtered.OrderByDescending(c => c.CategoryId).ToList(),
            _ => filtered.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList()
        };

        RefreshActiveFilterChips();
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

        if (_hasLoadedData && !_loadFailed && _filteredCount == 0)
        {
            var kind = _allCategories.Count == 0
                ? CatalogEmptyKind.NoRecords
                : !string.IsNullOrWhiteSpace(query)
                    ? CatalogEmptyKind.NoSearchResults
                    : CatalogEmptyKind.NoFilterResults;

            _emptyState.Configure(kind,
                _permissions.Can(Permission.CreateLookups),
                "category", "categories", "category name or description");
            _emptyState.Visible = true;
            _emptyState.BringToFront();
        }
        else
        {
            _emptyState.Visible = false;
        }

        _status.Text = _filteredCount == 0
            ? (_allCategories.Count == 0
                ? "The catalog has no categories yet."
                : "No matching categories.")
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

    private void HandleViewClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || e.RowIndex < 0 || e.ColumnIndex < 0 ||
            _working || _loadingData || _details is not null ||
            _grid.Columns[e.ColumnIndex].Name != "Actions") return;
        if (_grid.Rows[e.RowIndex].DataBoundItem is Category category)
            OpenCategoryDetails(category);
    }

    private void OpenCategoryDetails(Category? category)
    {
        if (_details is not null || IsDisposed || Disposing) return;
        if (category is null && !_permissions.Can(Permission.CreateLookups)) return;
        var details = new CategoryDetailsPage(_categories, _books, _permissions, category);
        _details = details;
        details.BackRequested += (_, _) => CloseCategoryDetails();
        details.Saved += async (_, _) =>
        {
            if (!IsDisposed && !Disposing) await LoadCategoriesAsync();
        };
        details.BookRequested += (_, bookId) => BookRequested?.Invoke(this, bookId);
        _layout.Visible = false;
        _host.Controls.Add(details);
        details.BringToFront();
    }

    public Task RefreshCategoryDetailsAsync() =>
        _details?.RefreshBooksAsync() ?? Task.CompletedTask;

    private void CloseCategoryDetails()
    {
        if (_details is null) return;
        _host.Controls.Remove(_details);
        _details.Dispose();
        _details = null;
        _layout.Visible = true;
        _layout.BringToFront();

        BeginInvoke(new Action(() =>
        {
            if (!IsDisposed && !Disposing &&
                _details is null && _grid.CanFocus)
            {
                _grid.Focus();
                _grid.ClearRowSelection();
            }
        }));

    }

    private void ClearActionHover()
    {
        _hoverTimer.Stop();
        _actionTooltip.Hide(_grid);
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
            int cx = e.CellBounds.Left + e.CellBounds.Width / 2;
            using var pen = new Pen(c.Accent, 2.2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            g.DrawLines(pen, new[]
            {
                new Point(cx - 4, cy - 7),
                new Point(cx + 3, cy),
                new Point(cx - 4, cy + 7)
            });
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
                    compact ? 108 : 145));

            _toolbar.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute,
                    compact ? 44 : 160));

            _toolbar.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            _toolbar.Controls.Add(_search, 0, 0);
            _toolbar.Controls.Add(_filterSort, 1, 0);
            _toolbar.Controls.Add(_addButton, 2, 0);

            _filterSort.Visible = CanManage;

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
                _grid.Columns["CategoryId"]!.Width = 138;
                _grid.Columns["Name"]!.Width = 140;
                _grid.Columns["Description"]!.Width = 210;
                _grid.Columns["IsActive"]!.Width = 90;
                _grid.Columns["Actions"]!.Width = 55;
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
        ApplyUnifiedToolbar();
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
        _emptyState.Visible = false;

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
            _emptyState.Visible = false;

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

    private void InitializeUnifiedFilters()
    {
        _filterSort.EnableCategoryIdSort = true;
        _sortControl.Configure(_filterSort.SortOptions,
            () => _filterSort.SortIndex, i => _filterSort.SetSort(i));
        ApplyUnifiedToolbar();
        _filterSort.SelectionChanged += (_, _) => RefreshActiveFilterChips();
        RefreshActiveFilterChips();
    }

    private void ApplyUnifiedToolbar()
    {
        bool compactToolbar = ClientSize.Width < 590;
        CatalogToolbarChrome.Install(_layout, _toolbar, _status,
            _search, _filterSort, _addButton, _sortControl, _activeChips,
            compactToolbar, CanManage);
    }

    private void RefreshActiveFilterChips()
    {
        var chips = new List<(string Text, Action Remove)>();
        if (_filterSort.StatusIndex != 0)
            chips.Add((_filterSort.StatusIndex == 1 ? "Record: Active" : "Record: Archived", _filterSort.ClearStatus));
        CatalogToolbarChrome.ShowChips(_layout, _activeChips, chips, _filterSort.ClearFilters);
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
        _pageInfo.Margin = Padding.Empty;
        _pageInfo.Padding = Padding.Empty;
        _pageInfo.TextAlign = ContentAlignment.MiddleLeft;

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

        _filterSort.ApplyTheme();
        _sortControl.ApplyTheme();
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

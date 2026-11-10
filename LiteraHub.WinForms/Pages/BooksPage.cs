
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

public sealed class BooksPage : ThemedUserControl
{
    private readonly IBookService _books;
    private readonly IPermissionService _permissions;

    private readonly TableLayoutPanel _layout = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 5,
        Margin = Padding.Empty
    };

    private readonly Label _heading = new()
    {
        Text = "Books",
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly TableLayoutPanel _toolbar = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Margin = Padding.Empty
    };

    private readonly ThemedSearchBox _search = new()
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 4, 8, 4)
    };

    private readonly ThemedFilterDropdown _filter =
        new(new[] { "All", "Active", "Inactive" })
        {
            Dock = DockStyle.Fill,
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
        Padding = new Padding(0, 12, 0, 0)
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
        Margin = Padding.Empty
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
        Size = new Size(42, 32),
        TextAlign = ContentAlignment.MiddleCenter
    };

    private readonly ThemedFilterDropdown _pageSizeFilter =
        new(new[] { "10", "25", "50", "100" })
        {
            Size = new Size(68, 32)
        };

    private List<Book> _allBooks = new();

    private int _page = 1;
    private int _pageSize = 10;
    private int _filteredCount;

    private bool _loadStarted;
    private bool _loadingData;
    private bool _hasLoadedData;
    private bool _loadFailed;
    private bool _compact;

    private bool CanManage =>
        _permissions.Can(Permission.ManageBooks);

    private int TotalPages =>
        Math.Max(1,
            (_filteredCount + _pageSize - 1) / _pageSize);

    public BooksPage(
        IBookService books,
        IPermissionService permissions)
    {
        _books = books ??
            throw new ArgumentNullException(nameof(books));

        _permissions = permissions ??
            throw new ArgumentNullException(nameof(permissions));

        Dock = DockStyle.Fill;
        Padding = new Padding(Space.L);

        BuildLayout();
        ConfigureGrid();

        _loading.SetContent(_grid);

        _search.Placeholder =
            "Search ISBN, title, author, category, status...";

        _filter.Visible = CanManage;

        _search.TextChanged += (_, _) =>
        {
            _page = 1;
            FilterBooks();
        };

        _filter.SelectedIndexChanged += (_, _) =>
        {
            _page = 1;
            FilterBooks();
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
            FilterBooks();
        };

        _previous.Click += (_, _) =>
        {
            if (_loadingData || _page <= 1)
                return;

            _page--;
            FilterBooks();
        };

        _next.Click += (_, _) =>
        {
            if (_loadingData || _page >= TotalPages)
                return;

            _page++;
            FilterBooks();
        };

        _status.Text = "Waiting to load books...";

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

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 60));

        _toolbar.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _toolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _toolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 150));

        _toolbar.Controls.Add(_search, 0, 0);
        _toolbar.Controls.Add(_filter, 1, 0);

        _pagination.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _pagination.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 300));

        _pagination.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        foreach (Control control in new Control[]
                 {
                     _previous, _next, _pageNumber,
                     _rowsLabel, _pageSizeFilter
                 })
        {
            control.Margin = new Padding(4, 8, 0, 0);
        }

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

        AddColumn("Title", "Title", nameof(Book.Title), 28, 160);
        AddColumn("Authors", "Author", nameof(Book.Authors), 22, 145);
        AddColumn("Category", "Category", nameof(Book.CategoryName), 14, 110);
        AddColumn("ISBN", "ISBN", nameof(Book.Isbn), 16, 125);
        AddColumn("Copies", "Available", nameof(Book.AvailableCopies), 10, 85);
        AddColumn("Condition", "Condition", nameof(Book.StatusName), 14, 120);

        if (CanManage)
        {
            AddColumn(
                "Active", "Active", nameof(Book.IsActive),
                8, 75);
        }

        foreach (string name in new[] { "Copies", "Active" })
        {
            var column = _grid.Columns[name];

            if (column is null)
                continue;

            column.HeaderCell.Style.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            column.DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;
        }

        _grid.DataBindingComplete += (_, _) =>
            _grid.ClearRowSelection();
    }

    private void AddColumn(
        string name,
        string header,
        string property,
        float weight,
        int minimumWidth)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            DataPropertyName = property,
            FillWeight = weight,
            MinimumWidth = minimumWidth,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (_loadStarted)
            return;

        _loadStarted = true;
        await LoadBooksAsync();
    }

    private async Task LoadBooksAsync()
    {
        if (IsDisposed || Disposing || _loadingData)
            return;

        _loadingData = true;
        _loadFailed = false;

        _status.Text = "Loading books...";
        _grid.Enabled = false;

        UpdatePaginationButtons();

        try
        {
            var books = await _loading.RunAsync(
                () => _books.GetAllAsync(CanManage));

            if (IsDisposed || Disposing)
                return;

            _allBooks = books.ToList();
            _hasLoadedData = true;
        }
        catch (Exception ex)
        {
            if (IsDisposed || Disposing)
                return;

            _loadFailed = true;
            _status.Text =
                "Unable to load books: " + ex.Message;
        }
        finally
        {
            _loadingData = false;

            if (!IsDisposed && !Disposing)
            {
                _grid.Enabled = true;

                if (!_loadFailed)
                    FilterBooks();

                UpdatePaginationButtons();
                _grid.ClearRowSelection();
            }
        }
    }

    private void FilterBooks()
    {
        if (IsDisposed || Disposing || _loadingData)
            return;

        string query = _search.Text.Trim();

        int activeFilter = CanManage
            ? _filter.SelectedIndex
            : 1;

        var filtered = _allBooks.Where(book =>
            (activeFilter == 0 ||
             (activeFilter == 1 && book.IsActive) ||
             (activeFilter == 2 && !book.IsActive)) &&
            (query.Length == 0 ||
             Contains(book.Isbn, query) ||
             Contains(book.Title, query) ||
             Contains(book.Authors, query) ||
             Contains(book.CategoryName, query) ||
             Contains(book.StatusName, query)))
            .ToList();

        _filteredCount = filtered.Count;
        _page = Math.Clamp(_page, 1, TotalPages);

        _grid.DataSource = filtered
            .Skip((_page - 1) * _pageSize)
            .Take(_pageSize)
            .ToList();

        _grid.ClearRowSelection();

        _status.Text = _filteredCount == 0
            ? "No matching books."
            : $"{_filteredCount} of {_allBooks.Count} books match.";

        int start = _filteredCount == 0
            ? 0
            : (_page - 1) * _pageSize + 1;

        int end = Math.Min(
            _page * _pageSize, _filteredCount);

        _pageInfo.Text =
            $"Showing {start}–{end} of {_filteredCount}";

        UpdatePaginationButtons();
    }

    private static bool Contains(
        string? source, string query)
    {
        return source?.Contains(
            query,
            StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private void UpdatePaginationButtons()
    {
        if (IsDisposed || Disposing)
            return;

        bool ready =
            _hasLoadedData &&
            !_loadFailed &&
            !_loadingData;

        _pageNumber.Text = $"{_page} / {TotalPages}";
        _previous.Enabled = ready && _page > 1;
        _next.Enabled = ready && _page < TotalPages;
        _pageSizeFilter.Enabled = ready;
    }

    private void UpdateResponsiveLayout()
    {
        if (Width <= 0)
            return;

        bool compact = Width < 620;

        if (_compact == compact)
            return;

        _compact = compact;

        _toolbar.ColumnStyles[1].Width =
            compact ? 112 : 150;

        _pagination.ColumnStyles[1].Width =
            compact ? 160 : 300;

        _rowsLabel.Visible = !compact;
        _pageSizeFilter.Visible = !compact;
        _pageInfo.Visible = !compact;

        _search.Margin = compact
            ? new Padding(0, 4, 4, 4)
            : new Padding(0, 4, 8, 4);
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

        foreach (var button in new[] { _previous, _next })
        {
            button.BackColor = c.Surface;
            button.ForeColor = c.Text;
            button.Font = Theme.BodyStrong;
            button.FlatAppearance.BorderColor = c.Border;
            button.FlatAppearance.BorderSize = 1;
        }

        _filter.ApplyTheme();
        _pageSizeFilter.ApplyTheme();
        _grid.ApplyTheme();

        _grid.ClearRowSelection();
        _grid.Invalidate();
    }
}

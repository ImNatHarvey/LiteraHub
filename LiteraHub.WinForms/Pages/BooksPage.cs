
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
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

public sealed class BooksPage : ThemedUserControl
{
    private readonly IBookService _books;
    private readonly IPermissionService _permissions;

    // Retained for the upcoming editor implementation.
    private readonly ICategoryService _categories;
    private readonly IAuthorCreationService _authors;
    private readonly IBookMetadataService _metadata;

    private readonly Panel _host = new()
    {
        Dock = DockStyle.Fill,
        Margin = Padding.Empty
    };

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
        Text = "Books",
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
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
            Margin = new Padding(0, 4, 8, 4)
        };

    private readonly Button _addButton = new()
    {
        Text = "+ Add Book",
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

    private List<Book> _allBooks = new();
    private BookEditorPage? _editor;

    private int _page = 1;
    private int _pageSize = 10;
    private int _filteredCount;
    private int _hoverRow = -1;
    private int _hoverColumn = -1;

    private bool _loadStarted;
    private bool _loadingData;
    private bool _hasLoadedData;
    private bool _loadFailed;
    private bool _compact;
    private bool _layoutInitialized;
    private bool _openingEditor;

    private bool CanManage =>
        _permissions.Can(Permission.ManageBooks);

    private int TotalPages =>
        Math.Max(
            1,
            (_filteredCount + _pageSize - 1) / _pageSize);

    public BooksPage(
        IBookService books,
        ICategoryService categories,
        IAuthorCreationService authors,
        IBookMetadataService metadata,
        IPermissionService permissions)
    {
        _books = books ??
            throw new ArgumentNullException(nameof(books));

        _categories = categories ??
            throw new ArgumentNullException(nameof(categories));

        _authors = authors ??
            throw new ArgumentNullException(nameof(authors));

        _metadata = metadata ??
            throw new ArgumentNullException(nameof(metadata));

        _permissions = permissions ??
            throw new ArgumentNullException(nameof(permissions));

        Dock = DockStyle.Fill;

        BuildLayout();
        ConfigureGrid();

        _loading.SetContent(_grid);

        _search.Placeholder =
            "Search ISBN, title, author, category, status...";

        _filter.Visible = CanManage;
        _addButton.Visible = CanManage;
        _addButton.AccessibleName = "Add Book";

        _addButton.Click += async (_, _) =>
            await OpenEditorAsync();

        _grid.CellPainting += PaintActionCell;

        // CellMouseUp is used instead of CellContentClick:
        // custom-painted text cells have no button content.
        _grid.CellMouseUp += async (_, e) =>
        {
            if (e.Button != MouseButtons.Left ||
                e.RowIndex < 0 ||
                e.ColumnIndex < 0 ||
                _grid.Columns[e.ColumnIndex].Name != "Actions")
                return;

            await OpenRowEditorAsync(e.RowIndex);
        };

        _grid.MouseMove += HandleGridMouseMove;
        _grid.MouseLeave += (_, _) => ClearActionHover();
        _grid.Scroll += (_, _) => ClearActionHover();

        _grid.ShowCellToolTips = false;
        _hoverTimer.Tick += HandleHoverTimerTick;

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
        _layout.Padding = new Padding(Space.L);

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
            new ColumnStyle(SizeType.Absolute, 125));

        _toolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 160));

        _toolbar.Controls.Add(_search, 0, 0);
        _toolbar.Controls.Add(_filter, 1, 0);
        _toolbar.Controls.Add(_addButton, 2, 0);

        _pagination.Padding = new Padding(0, 12, 0, 0);

        _pagination.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _pagination.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 300));

        _pagination.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        foreach (Control control in new Control[]
                 {
                     _previous,
                     _next,
                     _pageNumber,
                     _rowsLabel,
                     _pageSizeFilter
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

        _host.Controls.Add(_layout);
        Controls.Add(_host);
    }

    private void ConfigureGrid()
    {
        _grid.ReadOnlyDisplayMode = true;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;
        _grid.AutoGenerateColumns = false;

        AddColumn(
            "Title", "Title", nameof(Book.Title), 28, 160);

        AddColumn(
            "Authors", "Author", nameof(Book.Authors), 22, 145);

        AddColumn(
            "Category", "Category",
            nameof(Book.CategoryName), 14, 110);

        AddColumn(
            "ISBN", "ISBN", nameof(Book.Isbn), 16, 125);

        AddColumn(
            "Copies", "Available",
            nameof(Book.AvailableCopies), 12, 105);

        AddColumn(
            "Condition", "Condition",
            nameof(Book.StatusName), 14, 120);

        if (CanManage)
        {
            AddColumn(
                "Active", "Active",
                nameof(Book.IsActive), 8, 75);

            _grid.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "Actions",
                    HeaderText = "",
                    FillWeight = 7,
                    MinimumWidth = 55,
                    SortMode =
                        DataGridViewColumnSortMode.NotSortable
                });
        }

        foreach (string name in new[]
                 { "Copies", "Active", "Actions" })
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

    private void PaintActionCell(
        object? sender,
        DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        if (_grid.Columns[e.ColumnIndex].Name != "Actions")
            return;

        if (_grid.Rows[e.RowIndex].DataBoundItem is not Book)
            return;

        e.PaintBackground(e.CellBounds, false);

        var graphics = e.Graphics;

        if (graphics is null)
            return;

        var previousMode = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        int cx = e.CellBounds.Left + e.CellBounds.Width / 2;
        int cy = e.CellBounds.Top + e.CellBounds.Height / 2;

        using var pen = new Pen(
            Theme.Colors.Accent, 2.2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        // Custom chevron icon. No native button or gray highlight.
        graphics.DrawLines(
            pen,
            new[]
            {
                new Point(cx - 4, cy - 7),
                new Point(cx + 3, cy),
                new Point(cx - 4, cy + 7)
            });

        graphics.SmoothingMode = previousMode;
        e.Handled = true;
    }

    private async Task OpenRowEditorAsync(int rowIndex)
    {
        if (!CanManage ||
            _openingEditor ||
            _loadingData ||
            _editor is not null)
            return;

        if (rowIndex < 0 || rowIndex >= _grid.Rows.Count)
            return;

        if (_grid.Rows[rowIndex].DataBoundItem is Book book)
            await OpenEditorAsync(book);
    }

    private void HandleGridMouseMove(
        object? sender,
        MouseEventArgs e)
    {
        if (!CanManage ||
            _loadingData ||
            _openingEditor ||
            _editor is not null)
        {
            ClearActionHover();
            return;
        }

        var hit = _grid.HitTest(e.X, e.Y);

        if (hit.Type != DataGridViewHitTestType.Cell ||
            hit.RowIndex < 0 ||
            hit.ColumnIndex < 0 ||
            _grid.Columns[hit.ColumnIndex].Name != "Actions")
        {
            ClearActionHover();
            return;
        }

        _grid.Cursor = Cursors.Hand;

        if (_hoverRow == hit.RowIndex &&
            _hoverColumn == hit.ColumnIndex)
            return;

        _actionTooltip.Hide(_grid);
        _hoverTimer.Stop();

        _hoverRow = hit.RowIndex;
        _hoverColumn = hit.ColumnIndex;

        _hoverTimer.Start();
    }

    private void HandleHoverTimerTick(
        object? sender,
        EventArgs e)
    {
        _hoverTimer.Stop();

        if (_hoverRow < 0 ||
            _hoverColumn < 0 ||
            IsDisposed ||
            Disposing ||
            _openingEditor ||
            _loadingData ||
            _editor is not null)
            return;

        Point mouse = _grid.PointToClient(
            Control.MousePosition);

        var hit = _grid.HitTest(mouse.X, mouse.Y);

        if (hit.RowIndex != _hoverRow ||
            hit.ColumnIndex != _hoverColumn)
            return;

        Rectangle rect = _grid.GetCellDisplayRectangle(
            _hoverColumn,
            _hoverRow,
            false);

        _actionTooltip.Show(
            "Edit book",
            _grid,
            rect.Left + rect.Width / 2,
            rect.Bottom + 3,
            2500);
    }

    private void ClearActionHover()
    {
        _hoverTimer.Stop();
        _actionTooltip.Hide(_grid);

        _hoverRow = -1;
        _hoverColumn = -1;

        if (!IsDisposed && !Disposing)
            _grid.Cursor = Cursors.Default;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        UpdateResponsiveLayout();

        if (_loadStarted)
            return;

        _loadStarted = true;
        await LoadBooksAsync();
    }

    private async Task OpenEditorAsync(Book? selected = null)
    {
        if (!CanManage ||
            _editor is not null ||
            _openingEditor ||
            _loadingData)
            return;

        _openingEditor = true;
        _addButton.Enabled = false;
        ClearActionHover();

        try
        {
            Book? existing = null;

            if (selected is not null)
            {
                existing = await _books.GetByIdAsync(
                    selected.BookId);

                if (existing is null)
                {
                    MessageBox.Show(
                        FindForm(),
                        "This book no longer exists.",
                        "LiteraHub",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    await LoadBooksAsync();
                    return;
                }
            }

            if (IsDisposed || Disposing)
                return;

            var editor = new BookEditorPage(existing);
            editor.Dock = DockStyle.Fill;
            editor.ConfigureServices(
                _categories,
                _metadata,
                _books,
                _permissions.Can(Permission.HardDelete),
                _authors,
                _permissions.Can(Permission.CreateLookups));

            editor.BackRequested += HandleBackRequested;
            editor.Saved += async (_, _) =>
            {
                HandleBackRequested(editor, EventArgs.Empty);
                await LoadBooksAsync();
            };

            _editor = editor;
            _layout.Visible = false;
            _host.Controls.Add(editor);
            editor.BringToFront();
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
            {
                MessageBox.Show(
                    FindForm(),
                    "Unable to open book editor: " +
                    ex.Message,
                    "LiteraHub",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        finally
        {
            _openingEditor = false;

            if (!IsDisposed && !Disposing)
                _addButton.Enabled = CanManage;
        }
    }

    private void HandleBackRequested(
        object? sender,
        EventArgs e)
    {
        if (_editor is null)
            return;

        _editor.BackRequested -= HandleBackRequested;

        _host.Controls.Remove(_editor);
        _editor.Dispose();
        _editor = null;

        _layout.Visible = true;
        _layout.BringToFront();

        FilterBooks();
    }

    private async Task LoadBooksAsync()
    {
        if (IsDisposed || Disposing || _loadingData)
            return;

        _loadingData = true;
        _loadFailed = false;

        _status.Text = "Loading books...";
        _grid.Enabled = false;
        _addButton.Enabled = false;

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
                _addButton.Enabled = CanManage;

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

        ClearActionHover();

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
        string? source,
        string query)
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
        if (IsDisposed || Disposing)
            return;

        int available = Math.Max(
            100,
            ClientSize.Width - Padding.Horizontal);

        bool compact = available < 550;

        if (_layoutInitialized && _compact == compact)
            return;

        _compact = compact;
        _layoutInitialized = true;

        _toolbar.SuspendLayout();
        _pagination.SuspendLayout();

        try
        {
            // Same toolbar dimensions as CategoriesPage.
            _toolbar.ColumnStyles[1].Width =
                compact ? 78 : 125;

            _toolbar.ColumnStyles[2].Width =
                compact ? 44 : 160;

            _addButton.Text =
                compact ? "+" : "+ Add Book";

            _addButton.Font = compact
                ? new Font("Segoe UI", 18f)
                : Theme.BodyStrong;

            _grid.AutoSizeColumnsMode = compact
                ? DataGridViewAutoSizeColumnsMode.None
                : DataGridViewAutoSizeColumnsMode.Fill;

            if (compact)
            {
                _grid.Columns["Title"]!.Width = 160;
                _grid.Columns["Authors"]!.Width = 145;
                _grid.Columns["Category"]!.Width = 110;
                _grid.Columns["ISBN"]!.Width = 125;
                _grid.Columns["Copies"]!.Width = 110;
                _grid.Columns["Condition"]!.Width = 120;

                if (CanManage)
                {
                    _grid.Columns["Active"]!.Width = 75;
                    _grid.Columns["Actions"]!.Width = 55;
                }
            }

            _pagination.ColumnStyles[1].Width =
                compact ? 245 : 300;

            _pageInfo.Font = compact
                ? Theme.Caption
                : Theme.Body;

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

    protected override void ApplyTheme()
    {
        base.ApplyTheme();

        var c = Theme.Colors;

        _host.BackColor = c.Background;
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

        _addButton.BackColor = c.Accent;
        _addButton.ForeColor = c.OnAccent;
        _addButton.FlatAppearance.BorderSize = 0;
        _addButton.Font = _compact
            ? new Font("Segoe UI", 18f)
            : Theme.BodyStrong;

        _filter.ApplyTheme();
        _pageSizeFilter.ApplyTheme();
        _grid.ApplyTheme();

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

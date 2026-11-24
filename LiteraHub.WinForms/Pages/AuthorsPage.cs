using System.Collections.Generic;

using System;
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

public sealed class AuthorsPage : ThemedUserControl
{
    private readonly IBookService _books;
    private readonly IAuthorCreationService _creation;
    private readonly IPermissionService _permissions;

    private readonly Panel _host = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private AuthorDetailsPage? _details;
    public event EventHandler<int>? BookRequested;

    private readonly TableLayoutPanel _layout = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 5,
        Margin = Padding.Empty
    };

    private readonly TableLayoutPanel _toolbar = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 3,
        RowCount = 1,
        Margin = Padding.Empty
    };

    private readonly Label _heading = new()
    {
        Text = "Authors",
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly ThemedSearchBox _search = new()
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 4, 8, 4)
    };

    private readonly ThemedCatalogFilterSort _filter = new("GENDER", "All", "Male", "Female", "Other")
    {
        EnableIdSort = true,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 4, 8, 4)
    };

    private readonly Button _add = new()
    {
        Text = "+ Add Author",
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
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly ThemedDataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        Margin = Padding.Empty
    };

    private readonly LoadingHost _loading = new()
    {
        Dock = DockStyle.Fill,
        SkeletonStyle = SkeletonStyle.Rows
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
        TextAlign = ContentAlignment.MiddleLeft
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
        FlatStyle = FlatStyle.Flat
    };

    private readonly Button _next = new()
    {
        Text = "›",
        Size = new Size(36, 32),
        FlatStyle = FlatStyle.Flat
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

    private readonly ThemedFilterDropdown _pageSize =
        new(new[] { "10", "25", "50", "100" })
        {
            Size = new Size(68, 32)
        };

    private AuthorRecord[] _authors =
        Array.Empty<AuthorRecord>();

    private int _page = 1;
    private int _rows = 10;
    private int _filteredCount;
    private bool _loadStarted;
    private bool _loadingData;
    private bool _working;
    private bool _compact;

    private bool CanManage =>
        _permissions.Can(Permission.ManageLookups);

    private int ActionCount => 1;

    private int TotalPages =>
        Math.Max(1, (_filteredCount + _rows - 1) / _rows);

    public AuthorsPage(
        IBookService books,
        IAuthorCreationService creation,
        IPermissionService permissions)
    {
        _books = books;
        _creation = creation;
        _permissions = permissions;

        Dock = DockStyle.Fill;
        Padding = new Padding(Space.L);

        BuildLayout();
        ConfigureGrid();

        _loading.SetContent(_grid);
        _search.Placeholder = "Search author ID, name or gender...";

        _filter.Visible = CanManage;

        _add.Visible =
            _permissions.Can(Permission.CreateLookups);

        _search.TextChanged += (_, _) =>
        {
            _page = 1;
            RenderRows();
        };

        _filter.SelectionChanged += (_, _) =>
        {
            _page = 1;
            RenderRows();
        };

        _pageSize.SelectedIndexChanged += (_, _) =>
        {
            _rows = _pageSize.SelectedIndex switch
            {
                1 => 25,
                2 => 50,
                3 => 100,
                _ => 10
            };

            _page = 1;
            RenderRows();
        };

        _previous.Click += (_, _) =>
        {
            if (_page > 1)
            {
                _page--;
                RenderRows();
            }
        };

        _next.Click += (_, _) =>
        {
            if (_page < TotalPages)
            {
                _page++;
                RenderRows();
            }
        };

        _add.Click += (_, _) => OpenAuthorDetails(null);


        _grid.CellMouseUp += HandleActionMouseUp;
        _grid.CellPainting += PaintSpecialCells;

        // Match the Categories pagination layout.
        // Remove the extra top padding that clips the text.

        // Exact Categories pagination layout.
        _pagination.Padding = new Padding(0, 12, 0, 0);

        _pageInfo.Dock = DockStyle.Fill;
        _pageInfo.Margin = Padding.Empty;
        _pageInfo.Padding = Padding.Empty;
        _pageInfo.AutoEllipsis = true;
        _pageInfo.TextAlign = ContentAlignment.MiddleLeft;

        _pageControls.Dock = DockStyle.Fill;
        _pageControls.Margin = Padding.Empty;
        _pageControls.Padding = Padding.Empty;

        _pagination.RowStyles.Clear();
        _pagination.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));


        // Match Categories action tooltips.
        _grid.ShowCellToolTips = false;

        var actionTooltip = new ToolTip
        {
            InitialDelay = 400,
            ReshowDelay = 100,
            AutoPopDelay = 3000,
            ShowAlways = true
        };

        var hoverTimer = new System.Windows.Forms.Timer
        {
            Interval = 450
        };

        // Status icons are informational; action icons keep their separate tooltips.
        var statusTooltip = new ToolTip { ShowAlways = true, AutoPopDelay = 2500 };
        _grid.CellMouseEnter += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                _grid.Columns[e.ColumnIndex].Name != "Status") return;

            bool active = string.Equals(
                Convert.ToString(_grid.Rows[e.RowIndex].Cells["Status"].Value),
                "Active", StringComparison.OrdinalIgnoreCase);
            Rectangle rect = _grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            statusTooltip.Show(active ? "Active" : "Inactive", _grid,
                rect.Left + rect.Width / 2, rect.Bottom + 3, 2500);
        };
        _grid.CellMouseLeave += (_, e) =>
        {
            if (e.ColumnIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == "Status")
                statusTooltip.Hide(_grid);
        };
        _grid.MouseLeave += (_, _) => statusTooltip.Hide(_grid);
        _grid.Scroll += (_, _) => statusTooltip.Hide(_grid);

        int hoverRow = -1;
        int hoverColumn = -1;
        int hoverSlot = -1;

        void ClearHover()
        {
            hoverTimer.Stop();
            actionTooltip.Hide(_grid);

            hoverRow = -1;
            hoverColumn = -1;
            hoverSlot = -1;

            if (!_grid.IsDisposed)
                _grid.Cursor = Cursors.Default;
        }

        _grid.MouseMove += (_, e) =>
        {
            if (_working || _loadingData)
            {
                ClearHover();
                return;
            }

            var hit = _grid.HitTest(e.X, e.Y);

            if (hit.Type != DataGridViewHitTestType.Cell ||
                hit.RowIndex < 0 ||
                hit.ColumnIndex < 0 ||
                _grid.Columns[hit.ColumnIndex].Name != "Actions")
            {
                ClearHover();
                return;
            }

            Rectangle rect = _grid.GetCellDisplayRectangle(
                hit.ColumnIndex,
                hit.RowIndex,
                false);

            int slot = Math.Clamp(
                (e.X - rect.Left) * ActionCount /
                    Math.Max(1, rect.Width),
                0,
                ActionCount - 1);

            _grid.Cursor = Cursors.Hand;

            if (hoverRow == hit.RowIndex &&
                hoverColumn == hit.ColumnIndex &&
                hoverSlot == slot)
                return;

            actionTooltip.Hide(_grid);
            hoverTimer.Stop();

            hoverRow = hit.RowIndex;
            hoverColumn = hit.ColumnIndex;
            hoverSlot = slot;

            hoverTimer.Start();
        };

        hoverTimer.Tick += (_, _) =>
        {
            hoverTimer.Stop();

            if (hoverRow < 0 ||
                hoverColumn < 0 ||
                hoverSlot < 0 ||
                _working ||
                _loadingData ||
                _grid.IsDisposed)
                return;

            Point mouse = _grid.PointToClient(
                Control.MousePosition);

            var hit = _grid.HitTest(mouse.X, mouse.Y);

            if (hit.RowIndex != hoverRow ||
                hit.ColumnIndex != hoverColumn)
                return;

            Rectangle rect = _grid.GetCellDisplayRectangle(
                hoverColumn,
                hoverRow,
                false);

            int currentSlot = Math.Clamp(
                (mouse.X - rect.Left) * ActionCount /
                    Math.Max(1, rect.Width),
                0,
                ActionCount - 1);

            if (currentSlot != hoverSlot)
                return;

            bool active = string.Equals(
                Convert.ToString(
                    _grid.Rows[hoverRow].Cells["Status"].Value),
                "Active",
                StringComparison.OrdinalIgnoreCase);

            string tooltipText = "View Details";

            if (tooltipText.Length == 0)
                return;

            int centerX = rect.Left +
                (int)((hoverSlot + 0.5) *
                    rect.Width / ActionCount);

            actionTooltip.Show(
                tooltipText,
                _grid,
                centerX,
                rect.Bottom + 3,
                2500);
        };

        _grid.MouseLeave += (_, _) => ClearHover();
        _grid.Scroll += (_, _) => ClearHover();

        Disposed += (_, _) =>
        {
            hoverTimer.Stop();
            hoverTimer.Dispose();
            actionTooltip.Dispose();
            statusTooltip.Dispose();
        };


        Resize += (_, _) => UpdateResponsiveLayout();
        InitializeUnifiedFilters();

        ApplyTheme();
        UpdateResponsiveLayout();
    }

    private void BuildLayout()
    {
        _layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        foreach (int height in new[] { 50, 56, 36 })
        {
            _layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, height));
        }

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 60));

        _toolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _toolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 125));

        _toolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 160));

        _toolbar.Controls.Add(_search, 0, 0);
        _toolbar.Controls.Add(_filter, 1, 0);
        _toolbar.Controls.Add(_add, 2, 0);

        _pagination.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _pagination.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 300));

        foreach (var button in new[] { _previous, _next })
        {
            button.Cursor = Cursors.Hand;
            button.Margin = new Padding(4, 8, 0, 0);
        }

        _pageNumber.Margin = new Padding(4, 8, 0, 0);
        _rowsLabel.Margin = new Padding(4, 8, 0, 0);
        _pageSize.Margin = new Padding(4, 8, 0, 0);

        _pageControls.Controls.Add(_next);
        _pageControls.Controls.Add(_pageNumber);
        _pageControls.Controls.Add(_previous);
        _pageControls.Controls.Add(_pageSize);
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
            Name = "Id",
            HeaderText = "Author ID",
            FillWeight = 12,
            MinimumWidth = 85
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Name",
            HeaderText = "Author Name",
            FillWeight = 32,
            MinimumWidth = 155
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Gender",
            HeaderText = "Gender",
            FillWeight = 13,
            MinimumWidth = 85
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "BirthDate",
            HeaderText = "Birth Date",
            FillWeight = 18,
            MinimumWidth = 120
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "Status",
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

        // Center the Status and Actions columns.
        foreach (string name in new[] { "Status", "Actions" })
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


    private void PaintSpecialCells(
        object? sender,
        DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        string? column = _grid.Columns[e.ColumnIndex].Name;

        if (column != "Status" && column != "Actions")
            return;

        e.PaintBackground(e.CellBounds, false);

        var g = e.Graphics;

        if (g is null)
            return;

        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int cy = e.CellBounds.Top + e.CellBounds.Height / 2;

        bool active = string.Equals(
            Convert.ToString(
                _grid.Rows[e.RowIndex].Cells["Status"].Value),
            "Active",
            StringComparison.OrdinalIgnoreCase);

        var c = Theme.Colors;

        if (column == "Status")
        {
            int cx =
                e.CellBounds.Left + e.CellBounds.Width / 2;

            using var pen = new Pen(
                active ? c.PillGoodText : c.Danger,
                2f);

            g.DrawEllipse(
                pen,
                cx - 9,
                cy - 9,
                18,
                18);

            if (active)
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

        g.SmoothingMode = old;
        e.Handled = true;
    }


    private async void HandleActionMouseUp(
        object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || e.RowIndex < 0 ||
            e.ColumnIndex < 0 || _working || _loadingData ||
            _grid.Columns[e.ColumnIndex].Name != "Actions") return;
        if (!int.TryParse(Convert.ToString(_grid.Rows[e.RowIndex].Cells["Id"].Value),
                out int authorId)) return;
        _working = true;
        try
        {
            var author = await _creation.GetByIdAsync(authorId);
            if (IsDisposed || Disposing) return;
            if (author is null)
            {
                ThemedMessageDialog.ShowAlert(FindForm(), "View Author",
                    "Author no longer exists.", danger: true);
                return;
            }
            OpenAuthorDetails(author);
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                ThemedMessageDialog.ShowAlert(FindForm(), "View Author", ex.Message, danger: true);
        }
        finally { _working = false; }
    }

    private void OpenAuthorDetails(AuthorRecord? author)
    {
        if (_details is not null || IsDisposed || Disposing) return;
        var details = new AuthorDetailsPage(_creation, _books, _permissions, author);
        _details = details;
        details.BackRequested += (_, _) => CloseAuthorDetails();
        details.Saved += async (_, _) =>
        {
            if (!IsDisposed && !Disposing) await LoadAuthorsAsync();
        };
        details.BookRequested += (_, bookId) => BookRequested?.Invoke(this, bookId);
        _layout.Visible = false;
        _host.Controls.Add(details);
        details.BringToFront();
    }

    public Task RefreshAuthorDetailsAsync() => _details?.RefreshBooksAsync() ?? Task.CompletedTask;

    private void CloseAuthorDetails()
    {
        if (_details is null) return;
        _host.Controls.Remove(_details);
        _details.Dispose();
        _details = null;
        _layout.Visible = true;
        _layout.BringToFront();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (_loadStarted)
            return;

        _loadStarted = true;

        await LoadAuthorsAsync();
    }


    private async Task LoadAuthorsAsync()
    {
        if (_loadingData || IsDisposed)
            return;

        _loadingData = true;
        _add.Enabled = false;
        _status.Text = "Loading authors...";

        try
        {
            var result = await _loading.RunAsync(
                () => _creation.GetAllAsync(CanManage));

            if (IsDisposed || Disposing)
                return;

            _authors = result
                .OrderBy(a => a.FullName)
                .ToArray();

            RenderRows();
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
            {
                ThemedMessageDialog.ShowAlert(
                    FindForm(),
                    "Authors",
                    ex.Message,
                    danger: true);
            }
        }
        finally
        {
            _loadingData = false;

            if (!IsDisposed && !Disposing)
            {
                _add.Enabled =
                    _permissions.Can(Permission.CreateLookups);

                UpdatePagination();
            }
        }
    }



    private void RenderRows()
    {
        if (IsDisposed || Disposing)
            return;

        string query = _search.Text.Trim();

        int status = _filter.Visible
            ? _filter.StatusIndex
            : 1;

        var filtered = _authors
            .Where(a =>
                (status == 0 ||
                 (status == 1 && a.IsActive) ||
                 (status == 2 && !a.IsActive)) &&
                (query.Length == 0 ||
                 a.AuthorId.ToString().Contains(
                     query, StringComparison.OrdinalIgnoreCase) ||
                 a.FullName.Contains(
                     query,
                     StringComparison.OrdinalIgnoreCase) ||
                 a.Gender.Contains(
                     query,
                     StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        filtered = filtered
            .Where(a => _filter.ExtraIndex == 0 ||
                (_filter.ExtraIndex == 1 && a.Gender.Equals("Male", StringComparison.OrdinalIgnoreCase)) ||
                (_filter.ExtraIndex == 2 && a.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase)) ||
                (_filter.ExtraIndex == 3 && !a.Gender.Equals("Male", StringComparison.OrdinalIgnoreCase) && !a.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        filtered = _filter.SortIndex switch
        {
            1 => filtered.OrderByDescending(a => a.FullName, StringComparer.OrdinalIgnoreCase).ToArray(),
            2 => filtered.OrderBy(a => a.AuthorId).ToArray(),
            3 => filtered.OrderByDescending(a => a.AuthorId).ToArray(),
            _ => filtered.OrderBy(a => a.FullName, StringComparer.OrdinalIgnoreCase).ToArray()
        };

        RefreshActiveFilterChips();
        _filteredCount = filtered.Length;
        _page = Math.Clamp(_page, 1, TotalPages);

        var visible = filtered
            .Skip((_page - 1) * _rows)
            .Take(_rows)
            .ToArray();

        _grid.Rows.Clear();

        foreach (var author in visible)
        {
            int rowIndex = _grid.Rows.Add(
                author.AuthorId,
                author.FullName,
                author.Gender,
                author.BirthDate?.ToString("MMM dd, yyyy") ?? "—",
                author.IsActive ? "Active" : "Archived");

            // Dates inherit the same alignment and padding as the Gender column,
            // so they line up with the "Birth Date" header. Only the empty
            // placeholder gets extra left padding to sit centered under the dates.
            if (!author.BirthDate.HasValue)
                CenterEmptyBirthDate(_grid.Rows[rowIndex].Cells["BirthDate"]);
        }

        _grid.ClearRowSelection();

        _status.Text = _filteredCount == 0
            ? "No matching authors."
            : $"{_filteredCount} of {_authors.Length} authors match.";

        int start = _filteredCount == 0
            ? 0
            : (_page - 1) * _rows + 1;

        int end = Math.Min(
            _page * _rows,
            _filteredCount);

        _pageInfo.Text =
            $"Showing {start}–{end} of {_filteredCount}";

        UpdatePagination();
    }


    // Centers the "—" placeholder under the width of a typical birth date
    // ("Mar 28, 2000") while keeping the column's own left alignment and padding.
    private static void CenterEmptyBirthDate(DataGridViewCell cell)
    {
        var inherited = cell.InheritedStyle;
        var font = inherited.Font ?? cell.DataGridView!.Font;

        const TextFormatFlags flags =
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

        var unlimited = new Size(int.MaxValue, int.MaxValue);

        int widestDate = 0;

        for (int month = 1; month <= 12; month++)
        {
            string sample =
                new DateTime(2000, month, 28).ToString("MMM dd, yyyy");

            widestDate = Math.Max(
                widestDate,
                TextRenderer.MeasureText(sample, font, unlimited, flags).Width);
        }

        int dashWidth =
            TextRenderer.MeasureText("—", font, unlimited, flags).Width;

        int offset = Math.Max(0, (widestDate - dashWidth) / 2);
        var padding = inherited.Padding;

        cell.Style.Padding = new Padding(
            padding.Left + offset,
            padding.Top,
            padding.Right,
            padding.Bottom);
    }


    private void UpdatePagination()
    {
        _pageNumber.Text = $"{_page} / {TotalPages}";

        _previous.Enabled =
            !_loadingData && _page > 1;

        _next.Enabled =
            !_loadingData && _page < TotalPages;

        _pageSize.Enabled = !_loadingData;
    }

    private void UpdateResponsiveLayout()
    {
        if (IsDisposed || Disposing)
            return;

        int available = Math.Max(
            100,
            ClientSize.Width - Padding.Horizontal);

        bool compact = available < 550;

        _compact = compact;

        _toolbar.SuspendLayout();
        _pagination.SuspendLayout();

        try
        {
            _toolbar.ColumnStyles[1].Width =
                compact ? 108 : 145;

            _toolbar.ColumnStyles[2].Width =
                compact ? 44 : 160;

            _add.Text =
                compact ? "+" : "+ Add Author";

            _add.Font = compact
                ? new Font("Segoe UI", 18f)
                : Theme.BodyStrong;

            _grid.AutoSizeColumnsMode = compact
                ? DataGridViewAutoSizeColumnsMode.None
                : DataGridViewAutoSizeColumnsMode.Fill;

            if (compact)
            {
                _grid.Columns["Id"]!.Width = 95;
                _grid.Columns["Name"]!.Width = 180;
                _grid.Columns["Gender"]!.Width = 100;
                _grid.Columns["BirthDate"]!.Width = 135;
                _grid.Columns["Status"]!.Width = 90;
                _grid.Columns["Actions"]!.Width =
                    55;
            }

            _pagination.ColumnStyles[1].Width =
                compact ? 245 : 300;

            _pageInfo.Font = compact
                ? Theme.Caption
                : Theme.Body;

            // Match CategoriesPage alignment exactly.
            _toolbar.Margin = Padding.Empty;
            _loading.Margin = Padding.Empty;
            _grid.Margin = Padding.Empty;
            _pagination.Margin = Padding.Empty;
            _pageControls.Margin = Padding.Empty;

            _layout.Margin = Padding.Empty;
            _layout.Padding = Padding.Empty;

            _add.Margin = new Padding(0, 4, 0, 4);

            _pagination.Padding = new Padding(0, 12, 0, 0);

            _pageInfo.Margin = Padding.Empty;
            _pageInfo.Padding = Padding.Empty;

            _pageControls.Padding = Padding.Empty;
        }
        finally
        {
            _pagination.ResumeLayout(true);
            _toolbar.ResumeLayout(true);
        }
        ApplyUnifiedToolbar();
    }


    private void InitializeUnifiedFilters()
    {
        _sortControl.Configure(_filter.SortOptions,
            () => _filter.SortIndex, i => _filter.SetSort(i));
        ApplyUnifiedToolbar();
        _filter.SelectionChanged += (_, _) => RefreshActiveFilterChips();
        RefreshActiveFilterChips();
    }

    private void ApplyUnifiedToolbar()
    {
        bool compactToolbar = ClientSize.Width < 590;
        CatalogToolbarChrome.Install(_layout, _toolbar, _status,
            _search, _filter, _add, _sortControl, _activeChips,
            compactToolbar, CanManage);
    }

    private void RefreshActiveFilterChips()
    {
        var chips = new List<(string Text, Action Remove)>();
        if (_filter.StatusIndex != 0)
            chips.Add((_filter.StatusIndex == 1 ? "Record: Active" : "Record: Archived", _filter.ClearStatus));
        if (_filter.ExtraIndex != 0)
            chips.Add(("Gender: " + new[] { "", "Male", "Female", "Other" }[_filter.ExtraIndex], _filter.ClearExtra));
        CatalogToolbarChrome.ShowChips(_layout, _activeChips, chips, _filter.ClearFilters);
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();

        var c = Theme.Colors;

        BackColor = c.Background;
        _host.BackColor = c.Background;
        _layout.BackColor = c.Background;
        _toolbar.BackColor = c.Background;
        _pagination.BackColor = c.Background;
        _pageControls.BackColor = c.Background;

        _heading.ForeColor = c.Text;
        _heading.Font = Theme.Title;

        _status.ForeColor = c.TextMuted;
        _status.Font = Theme.Caption;

        _pageInfo.ForeColor = c.TextMuted;
        _pageInfo.Margin = Padding.Empty;
        _pageInfo.Padding = Padding.Empty;
        _pageInfo.TextAlign = ContentAlignment.MiddleLeft;
        _pageInfo.Font = Theme.Body;

        _pageNumber.ForeColor = c.Text;
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
        _sortControl.ApplyTheme();
        _pageSize.ApplyTheme();

        _add.BackColor = c.Accent;
        _add.ForeColor = c.OnAccent;
        _add.Font = Theme.BodyStrong;
        _add.FlatAppearance.BorderSize = 0;

        _grid.ApplyTheme();
        _grid.ClearRowSelection();
    }
}

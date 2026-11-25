using System.Drawing.Drawing2D;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

/// <summary>Admin-only staff directory using the shared catalog grid, empty states and themed selectors.</summary>
public sealed class UsersPage : ThemedUserControl
{
    private readonly IStaffUserService _service;
    private readonly Panel _host = new() { Dock = DockStyle.Fill };
    private readonly TableLayoutPanel _layout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(16) };
    private readonly Label _title = new() { Text = "Users", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly TableLayoutPanel _toolbar = new() { Dock = DockStyle.Fill, ColumnCount = 5 };
    private readonly ThemedSearchBox _search = new() { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 8, 4), Placeholder = "Search username or user ID..." };
    private readonly ThemedFilterDropdown _role = new(new[] { "All roles", "Admin", "Librarian" }) { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 8, 4) };
    private readonly ThemedFilterDropdown _statusFilter = new(new[] { "All status", "Active", "Inactive" }) { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 8, 4) };
    private readonly ThemedFilterDropdown _sort = new(new[] { "Name A–Z", "Name Z–A", "Newest", "User ID" }) { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 8, 4) };
    private readonly Button _add = new() { Text = "+ Add User", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 4, 0, 4) };
    private readonly Label _summary = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Panel _results = new() { Dock = DockStyle.Fill };
    private readonly ThemedDataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnlyDisplayMode = true };
    private readonly CatalogEmptyState _empty = new();
    private readonly TableLayoutPanel _pagination = new() { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(0, 12, 0, 0) };
    private readonly Label _count = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly FlowLayoutPanel _pageButtons = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
    private readonly Button _prev = new() { Text = "‹", Size = new Size(36, 32), FlatStyle = FlatStyle.Flat };
    private readonly Button _next = new() { Text = "›", Size = new Size(36, 32), FlatStyle = FlatStyle.Flat };
    private readonly Label _pageNumber = new() { Size = new Size(64, 32), TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _rowsLabel = new() { Text = "Rows:", Size = new Size(42, 32), TextAlign = ContentAlignment.MiddleCenter };
    private readonly ThemedFilterDropdown _rows = new(new[] { "10", "25", "50", "100" }) { Size = new Size(68, 32) };
    private readonly List<StaffUser> _all = new();
    private List<StaffUser> _filtered = new();
    private UserDetailsPage? _details;
    private int _page = 1;
    private int _size = 10;
    private bool _busy;
    private bool _loaded;
    private bool _failed;

    public UsersPage(IStaffUserService service)
    {
        _service = service;
        Dock = DockStyle.Fill;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int h in new[] { 50, 56, 36, 0, 60 })
            _layout.RowStyles.Add(h == 0 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, h));
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        _toolbar.Controls.Add(_search, 0, 0);
        _toolbar.Controls.Add(_role, 1, 0);
        _toolbar.Controls.Add(_statusFilter, 2, 0);
        _toolbar.Controls.Add(_sort, 3, 0);
        _toolbar.Controls.Add(_add, 4, 0);
        _layout.Controls.Add(_title, 0, 0);
        _layout.Controls.Add(_toolbar, 0, 1);
        _layout.Controls.Add(_summary, 0, 2);
        _results.Controls.Add(_grid);
        _results.Controls.Add(_empty);
        _layout.Controls.Add(_results, 0, 3);
        _pagination.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _pagination.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        _pageButtons.Controls.AddRange(new Control[] { _next, _pageNumber, _prev, _rows, _rowsLabel });
        foreach (Control c in _pageButtons.Controls) c.Margin = new Padding(4, 8, 0, 0);
        _pagination.Controls.Add(_count, 0, 0);
        _pagination.Controls.Add(_pageButtons, 1, 0);
        _layout.Controls.Add(_pagination, 0, 4);
        _host.Controls.Add(_layout);
        Controls.Add(_host);

        AddColumn("Id", "User ID", nameof(StaffUser.UserId), 14, 90);
        AddColumn("Username", "Username", nameof(StaffUser.Username), 30, 155);
        AddColumn("Role", "Role", nameof(StaffUser.RoleName), 20, 125);
        AddColumn("Created", "Created", nameof(StaffUser.CreatedAt), 19, 125);
        AddColumn("Status", "Status", null, 12, 90);
        AddColumn("Actions", "", null, 5, 55);
        _grid.Columns["Status"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _grid.Columns["Status"]!.Width = 90;
        _grid.Columns["Actions"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _grid.Columns["Actions"]!.Width = 55;
        _grid.Columns["Created"]!.DefaultCellStyle.Format = "yyyy-MM-dd";
        foreach (var n in new[] { "Status", "Actions" })
        {
            _grid.Columns[n]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _grid.Columns[n]!.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.DataSource = new List<StaffUser>();
        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count || _grid.Rows[e.RowIndex].DataBoundItem is not StaffUser user) return;
            if (_grid.Columns[e.ColumnIndex].Name == "Status") { e.Value = user.IsActive ? "Active" : "Inactive"; e.FormattingApplied = true; }
        };
        _grid.CellPainting += PaintIcons;
        _grid.CellClick += async (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "Actions") return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is StaffUser user)
                await OpenDetailsAsync(user.UserId);
        };
        _grid.CellMouseMove += (_, e) =>
            _grid.Cursor = e.RowIndex >= 0 && e.ColumnIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == "Actions" ? Cursors.Hand : Cursors.Default;
        _grid.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || _grid.Rows[e.RowIndex].DataBoundItem is not StaffUser user) return;
            e.ToolTipText = _grid.Columns[e.ColumnIndex].Name switch
            {
                "Status" => user.IsActive ? "Active" : "Inactive",
                "Actions" => "View Details",
                _ => ""
            };
        };
        _search.TextChanged += (_, _) => Filter();
        _role.SelectedIndexChanged += (_, _) => Filter();
        _statusFilter.SelectedIndexChanged += (_, _) => Filter();
        _sort.SelectedIndexChanged += (_, _) => Filter();
        _rows.SelectedIndexChanged += (_, _) => { _size = new[] { 10, 25, 50, 100 }[_rows.SelectedIndex]; _page = 1; Render(); };
        _prev.Click += (_, _) => { _page--; Render(); };
        _next.Click += (_, _) => { _page++; Render(); };
        _add.Click += async (_, _) => await OpenDetailsAsync(null);
        _empty.ActionRequested += async (_, _) =>
        {
            if (_failed) await LoadUsersAsync();
            else if (_all.Count == 0) await OpenDetailsAsync(null);
            else if (!string.IsNullOrWhiteSpace(_search.Text)) _search.Text = "";
            else { _role.SelectedIndex = 0; _statusFilter.SelectedIndex = 0; Filter(); }
        };
        ApplyTheme();
    }

    private void AddColumn(string key, string title, string? property, float weight, int min)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = key,
            HeaderText = title,
            DataPropertyName = property ?? "",
            FillWeight = weight,
            MinimumWidth = min,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void PaintIcons(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
            _grid.Rows[e.RowIndex].DataBoundItem is not StaffUser user) return;
        string key = _grid.Columns[e.ColumnIndex].Name;
        if (key is not ("Status" or "Actions")) return;
        e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int x = e.CellBounds.X + e.CellBounds.Width / 2;
        int y = e.CellBounds.Y + e.CellBounds.Height / 2;
        if (key == "Status")
        {
            var color = user.IsActive ? Theme.Colors.PillGoodText : Theme.Colors.Danger;
            using var pen = new Pen(color, 2f);
            g.DrawEllipse(pen, x - 9, y - 9, 18, 18);
            if (user.IsActive)
                g.DrawLines(pen, new[] { new Point(x - 5, y), new Point(x - 1, y + 4), new Point(x + 6, y - 4) });
            else g.DrawLine(pen, x - 5, y, x + 5, y);
        }
        else
        {
            using var pen = new Pen(Theme.Colors.TextMuted, 2f) { LineJoin = LineJoin.Round };
            g.DrawLines(pen, new[] { new Point(x - 3, y - 6), new Point(x + 3, y), new Point(x - 3, y + 6) });
        }
        e.Handled = true;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        if (_busy) return;
        _busy = true;
        _failed = false;
        _add.Enabled = false;
        _empty.Visible = false;
        _summary.Text = "Loading staff accounts...";
        try
        {
            var users = await _service.GetAllAsync();
            if (IsDisposed || Disposing) return;
            _all.Clear();
            _all.AddRange(users);
            _loaded = true;
            Filter();
        }
        catch (Exception ex)
        {
            if (IsDisposed || Disposing) return;
            System.Diagnostics.Debug.WriteLine(ex);
            _failed = true;
            _summary.Text = "Unable to load staff accounts.";
            _empty.Configure(CatalogEmptyKind.LoadFailed, false, "user", "users");
            _empty.Visible = true;
            _empty.BringToFront();
        }
        finally { _busy = false; if (!IsDisposed) _add.Enabled = true; }
    }

    private void Filter()
    {
        if (!_loaded || _failed || IsDisposed) return;
        string query = _search.Text.Trim();
        _filtered = _all.Where(u =>
            (_role.SelectedIndex == 0 || string.Equals(u.RoleName, _role.SelectedIndex == 1 ? "Admin" : "Librarian", StringComparison.OrdinalIgnoreCase)) &&
            (_statusFilter.SelectedIndex == 0 || u.IsActive == (_statusFilter.SelectedIndex == 1)) &&
            (query.Length == 0 || u.Username.Contains(query, StringComparison.OrdinalIgnoreCase) || u.UserId.ToString().Contains(query)))
            .ToList();
        _filtered = (_sort.SelectedIndex switch
        {
            1 => _filtered.OrderByDescending(u => u.Username, StringComparer.OrdinalIgnoreCase),
            2 => _filtered.OrderByDescending(u => u.CreatedAt),
            3 => _filtered.OrderBy(u => u.UserId),
            _ => _filtered.OrderBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
        }).ToList();
        _page = 1;
        Render();
    }

    private void Render()
    {
        if (!_loaded || _failed || IsDisposed) return;
        int pages = Math.Max(1, (_filtered.Count + _size - 1) / _size);
        _page = Math.Clamp(_page, 1, pages);
        _grid.DataSource = _filtered.Skip((_page - 1) * _size).Take(_size).ToList();
        _grid.ClearRowSelection();
        _summary.Text = $"{_filtered.Count} of {_all.Count} staff accounts match.";
        _count.Text = _filtered.Count == 0 ? "Showing 0 users" :
            $"Showing {(_page - 1) * _size + 1}–{Math.Min(_page * _size, _filtered.Count)} of {_filtered.Count}";
        _pageNumber.Text = $"{_page} / {pages}";
        _prev.Enabled = _page > 1;
        _next.Enabled = _page < pages;
        _empty.Visible = _filtered.Count == 0;
        if (_empty.Visible)
        {
            var kind = _all.Count == 0 ? CatalogEmptyKind.NoRecords :
                queryIsActive() ? CatalogEmptyKind.NoSearchResults : CatalogEmptyKind.NoFilterResults;
            _empty.Configure(kind, true, "user", "users", "username or user ID");
            _empty.BringToFront();
        }
        bool queryIsActive() => !string.IsNullOrWhiteSpace(_search.Text);
    }

    private async Task OpenDetailsAsync(int? id)
    {
        if (_busy || _details is not null) return;
        _busy = true;
        try
        {
            StaffUser? user = id.HasValue ? await _service.GetByIdAsync(id.Value) : null;
            if (IsDisposed || Disposing) return;
            if (id.HasValue && user is null)
            {
                ThemedMessageDialog.ShowAlert(FindForm(), "Users", "Account no longer exists.");
                return;
            }
            _details = new UserDetailsPage(_service, user) { Dock = DockStyle.Fill };
            _details.BackRequested += async (_, _) =>
            {
                if (_details is null) return;
                _host.Controls.Remove(_details);
                _details.Dispose();
                _details = null;
                _layout.Visible = true;
                _layout.BringToFront();
                await LoadUsersAsync();
                if (!IsDisposed) BeginInvoke((MethodInvoker)(() => _grid.Focus()));
            };
            _layout.Visible = false;
            _host.Controls.Add(_details);
            _details.BringToFront();
        }
        catch (Exception ex)
        {
            ThemedMessageDialog.ShowAlert(FindForm(), "Users", ex.Message, danger: true);
        }
        finally { _busy = false; }
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        BackColor = c.Background;
        _layout.BackColor = c.Background;
        _host.BackColor = c.Background;
        _results.BackColor = c.Background;
        _title.Font = Theme.Title;
        _title.ForeColor = c.Text;
        _summary.Font = Theme.Caption;
        _summary.ForeColor = c.TextMuted;
        _count.Font = Theme.Body;
        _count.ForeColor = c.TextMuted;
        _pageNumber.ForeColor = c.Text;
        _rowsLabel.ForeColor = c.TextMuted;
        foreach (var b in new[] { _prev, _next })
        {
            b.BackColor = c.Surface;
            b.ForeColor = c.Text;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = c.Border;
            b.FlatAppearance.MouseOverBackColor = c.SurfaceAlt;
        }
        _add.BackColor = c.Accent;
        _add.ForeColor = c.OnAccent;
        _add.Font = Theme.BodyStrong;
        _add.FlatAppearance.BorderSize = 0;
        _add.FlatAppearance.MouseOverBackColor = c.AccentSoft;
        _grid.Invalidate();
    }
}

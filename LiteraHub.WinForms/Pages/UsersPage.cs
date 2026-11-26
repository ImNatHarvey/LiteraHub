using FontAwesome.Sharp;
using System.Drawing.Drawing2D;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

/// <summary>Admin-only staff directory using the shared catalog grid, empty states and themed selectors.</summary>
public sealed class UsersPage : ThemedUserControl
{
    private readonly IStaffUserService _service;
    private readonly IPermissionService _permissions;
    private readonly string _actorRole;
    private readonly int? _currentUserId;
    private readonly IRoleService? _roles;
    public event EventHandler? ProfileRequested;
    private RolesPage? _rolesPage;
    private readonly CenteredIconButton _manageRoles = new() { Text = "Manage Roles", Dock = DockStyle.Right, Width = 160, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, IconChar = IconChar.ShieldAlt, IconSize = 20, ImageAlign = ContentAlignment.MiddleCenter, TextAlign = ContentAlignment.MiddleCenter, TextImageRelation = TextImageRelation.ImageBeforeText };
    private readonly Panel _host = new() { Dock = DockStyle.Fill };
    private readonly TableLayoutPanel _layout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(Space.L), Margin = Padding.Empty };
    private readonly Label _title = new() { Text = "Users", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly TableLayoutPanel _toolbar = new() { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly ThemedSearchBox _search = new() { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 8, 4), Placeholder = "Search username, full name or user ID..." };
    private readonly ThemedUsersFilterSort _filter = new();
    private readonly CatalogSortButton _sortControl = new();
    private readonly CatalogFilterChips _activeChips = new();
    private bool _compact;
    private bool _responsiveInitialized;
    private readonly Button _add = new() { Text = "+ Add User", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 4, 0, 4) };
    private readonly Label _summary = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Panel _results = new() { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly ThemedDataGridView _grid = new() { Dock = DockStyle.Fill, Margin = Padding.Empty, ReadOnlyDisplayMode = true };
    private readonly CatalogEmptyState _empty = new();
    private readonly TableLayoutPanel _pagination = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = new Padding(0, 12, 0, 0) };
    private readonly Label _count = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly FlowLayoutPanel _pageButtons = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly Button _prev = new() { Text = "‹", Size = new Size(36, 32), FlatStyle = FlatStyle.Flat };
    private readonly Button _next = new() { Text = "›", Size = new Size(36, 32), FlatStyle = FlatStyle.Flat };
    private readonly Label _pageNumber = new() { Size = new Size(64, 32), TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _rowsLabel = new() { Text = "Rows:", AutoSize = false, Size = new Size(42, 32), TextAlign = ContentAlignment.MiddleCenter };
    private readonly ThemedFilterDropdown _rows = new(new[] { "10", "25", "50", "100" }) { Size = new Size(68, 32) };
    private readonly List<StaffUser> _all = new();
    private List<StaffUser> _filtered = new();
    private UserDetailsPage? _details;
    private int _page = 1;
    private int _size = 10;
    private bool _busy;
    private bool _loaded;
    private bool _failed;

    public UsersPage(IStaffUserService service, IPermissionService permissions, string actorRole, int? currentUserId = null, IRoleService? roles = null)
    {
        _service = service;
        _permissions = permissions;
        _actorRole = actorRole;
        _currentUserId = currentUserId;
        _roles = roles;
        Dock = DockStyle.Fill;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int h in new[] { 50, 56, 36, 0, 60 })
            _layout.RowStyles.Add(h == 0 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, h));
        var heading = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        heading.Controls.Add(_title); heading.Controls.Add(_manageRoles); _manageRoles.BringToFront();
        _layout.Controls.Add(heading, 0, 0);
        _manageRoles.Visible = _roles is not null && _permissions.Can(RolePermission.RolesView);
        _manageRoles.Click += (_, _) => OpenRoles();
        _layout.Controls.Add(_toolbar, 0, 1);
        _layout.Controls.Add(_summary, 0, 2);
        _results.Controls.Add(_grid);
        _results.Controls.Add(_empty);
        _layout.Controls.Add(_results, 0, 3);
        _pagination.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _pagination.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        _pagination.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _pageButtons.Controls.AddRange(new Control[] { _next, _pageNumber, _prev, _rows, _rowsLabel });
        foreach (Control c in _pageButtons.Controls) c.Margin = new Padding(4, 8, 0, 0);
        _pagination.Controls.Add(_count, 0, 0);
        _pagination.Controls.Add(_pageButtons, 1, 0);
        _layout.Controls.Add(_pagination, 0, 4);
        _host.Controls.Add(_layout);
        Controls.Add(_host);

        AddColumn("Id", "User ID", nameof(StaffUser.UserId), 14, 90);
        AddColumn("Username", "Username", nameof(StaffUser.Username), 20, 115);
        AddColumn("FullName", "Full Name", nameof(StaffUser.FullName), 24, 140);
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
            _grid.Columns[n]!.HeaderCell.Style.Padding = Padding.Empty;
            _grid.Columns[n]!.DefaultCellStyle.Padding = Padding.Empty;
        }
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
        _grid.AutoGenerateColumns = false;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;
        _grid.ShowCellToolTips = true;
        _grid.DataBindingComplete += (_, _) => _grid.ClearRowSelection();
        _grid.DataSource = new List<StaffUser>();
        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count || _grid.Rows[e.RowIndex].DataBoundItem is not StaffUser user) return;
            if (_grid.Columns[e.ColumnIndex].Name == "Status") { e.Value = user.IsActive ? "Active" : "Inactive"; e.FormattingApplied = true; }
        };
        _grid.CellPainting += PaintIcons;
        _grid.CellMouseUp += async (_, e) =>
        {
            if (e.Button != MouseButtons.Left || e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count || e.ColumnIndex < 0 || e.ColumnIndex >= _grid.Columns.Count || _grid.Columns[e.ColumnIndex].Name != "Actions") return;
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
        _filter.SelectionChanged += (_, _) => { RefreshChips(); Filter(); };
        _rows.SelectedIndexChanged += (_, _) => { _size = new[] { 10, 25, 50, 100 }[_rows.SelectedIndex]; _page = 1; Render(); };
        _prev.Click += (_, _) => { _page--; Render(); };
        _next.Click += (_, _) => { _page++; Render(); };
        _add.Click += async (_, _) => await OpenDetailsAsync(null);
        _empty.ActionRequested += async (_, _) =>
        {
            if (_failed) await LoadUsersAsync();
            else if (_all.Count == 0) await OpenDetailsAsync(null);
            else if (!string.IsNullOrWhiteSpace(_search.Text)) _search.Text = "";
            else _filter.ClearFilters();
        };
        _sortControl.Configure(_filter.SortOptions, () => _filter.SortIndex, i => _filter.SetSort(i));
        Resize += (_, _) => UpdateResponsive();
        UpdateResponsive();
        RefreshChips();
        ApplyTheme();
    }

    private void OpenRoles()
    {
        if (_roles is null || !_permissions.Can(RolePermission.RolesView) || _busy || _details is not null || _rolesPage is not null) return;
        var page = new RolesPage(_roles, _permissions, _actorRole) { Dock = DockStyle.Fill };
        _rolesPage = page;
        page.BackRequested += async (_, _) =>
        {
            _host.Controls.Remove(page);
            page.Dispose(); _rolesPage = null;
            _layout.Visible = true; _layout.BringToFront();
            await LoadUsersAsync();
        };
        _layout.Visible = false;
        _host.Controls.Add(page); page.BringToFront();
    }

    private void UpdateResponsive()
    {
        bool compact = ClientSize.Width < 590;
        if (_responsiveInitialized && _compact == compact) return;
        _responsiveInitialized = true;
        _compact = compact;
        CatalogToolbarChrome.Install(_layout, _toolbar, _summary, _search, _filter, _add, _sortControl, _activeChips, compact, true);
        _add.Visible = _permissions.Can(RolePermission.UsersAdd);
        _toolbar.ColumnStyles[3].Width = _permissions.Can(RolePermission.UsersAdd) ? (compact ? 44 : 160) : 0;
        _add.Text = compact ? "+" : "+ Add User";
        _add.Font = compact ? new Font("Segoe UI", 18f) : Theme.BodyStrong;
        _pagination.ColumnStyles[1].Width = compact ? 245 : 300;
        _title.Font = compact ? Theme.BodyStrong : Theme.Title;
        _count.Font = compact ? Theme.Caption : Theme.Body;
    }

    private void RefreshChips()
    {
        var chips = new List<(string Text, Action Remove)>();
        if (_filter.StatusIndex != 0) chips.Add((_filter.StatusIndex == 1 ? "Record: Active" : "Record: Archived", _filter.ClearStatus));
        if (_filter.RoleIndex != 0) chips.Add(("Role: " + _filter.SelectedRoleName, _filter.ClearRole));
        CatalogToolbarChrome.ShowChips(_layout, _activeChips, chips, _filter.ClearFilters);
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
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (e.Graphics is not Graphics graphics) return;

        var c = Theme.Colors;
        var rect = e.CellBounds;
        using (var fill = new SolidBrush(e.RowIndex % 2 == 0 ? c.Surface : c.SurfaceAlt))
            graphics.FillRectangle(fill, rect);

        // Categories has a single subtle divider below each row, not cell boxes.
        using (var divider = new Pen(c.Border))
            graphics.DrawLine(divider, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);

        if (_grid.Columns[e.ColumnIndex].Name == "Status")
        {
            // The same check-in-circle / minus-in-circle status used by Authors.
            bool active = e.RowIndex < _grid.Rows.Count &&
                _grid.Rows[e.RowIndex].DataBoundItem is StaffUser user &&
                user.IsActive;
            int cx = rect.Left + rect.Width / 2;
            int cy = rect.Top + rect.Height / 2;
            var previous = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(active ? c.PillGoodText : c.Danger, 2f);
            graphics.DrawEllipse(pen, cx - 9, cy - 9, 18, 18);
            if (active)
                graphics.DrawLines(pen, new[]
                {
                    new Point(cx - 5, cy),
                    new Point(cx - 1, cy + 4),
                    new Point(cx + 6, cy - 4)
                });
            else
                graphics.DrawLine(pen, cx - 4, cy, cx + 4, cy);
            graphics.SmoothingMode = previous;
        }
        else if (_grid.Columns[e.ColumnIndex].Name == "Actions")
        {
            var previous = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(c.Accent, 2.2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            int x = rect.Left + rect.Width / 2;
            int y = rect.Top + rect.Height / 2;
            graphics.DrawLines(pen, new[]
            {
                new Point(x - 4, y - 7), new Point(x + 3, y), new Point(x - 4, y + 7)
            });
            graphics.SmoothingMode = previous;
        }
        else
        {
            // Preserve DataGridView formatting for FullName, Status, alignment
            // and the original Theme.Body font; do not paint native cell borders.
            e.Paint(rect, DataGridViewPaintParts.ContentForeground);
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
            _filter.SetRoleNames(_all.Select(u => u.RoleName));
            RefreshChips();
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
        finally { _busy = false; if (!IsDisposed) _add.Enabled = _permissions.Can(RolePermission.UsersAdd); }
    }

    private void Filter()
    {
        if (!_loaded || _failed || IsDisposed) return;
        string query = _search.Text.Trim();
        _filtered = _all.Where(u =>
            (_filter.RoleIndex == 0 || string.Equals(u.RoleName, _filter.SelectedRoleName, StringComparison.OrdinalIgnoreCase)) &&
            (_filter.StatusIndex == 0 || u.IsActive == (_filter.StatusIndex == 1)) &&
            (query.Length == 0 || u.Username.Contains(query, StringComparison.OrdinalIgnoreCase) || u.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) || u.UserId.ToString().Contains(query)))
            .ToList();
        _filtered = (_filter.SortIndex switch
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
            _empty.Configure(kind, _permissions.Can(RolePermission.UsersAdd), "user", "users", "username or user ID");
            _empty.BringToFront();
        }
        bool queryIsActive() => !string.IsNullOrWhiteSpace(_search.Text);
    }

    private async Task OpenDetailsAsync(int? id)
    {
        if (_busy || _details is not null || !_permissions.Can(RolePermission.UsersView) || (!id.HasValue && !_permissions.Can(RolePermission.UsersAdd))) return;
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
            var manageable = await _service.GetManageableRolesAsync();
            var roleNames = manageable.Where(r => r.IsActive).Select(r => r.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();
            bool canManageTarget = user is null || manageable.Any(r => string.Equals(r.Name, user.RoleName, StringComparison.OrdinalIgnoreCase));
            if (IsDisposed || Disposing) return;
            _details = new UserDetailsPage(_service, user, _permissions, canManageTarget, _currentUserId, roleNames) { Dock = DockStyle.Fill };
            _details.ProfileRequested += (_, _) => ProfileRequested?.Invoke(this, EventArgs.Empty);
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
        foreach (Control control in new Control[] { _host, _layout, _toolbar, _pagination, _pageButtons, _results }) control.BackColor = c.Background;
        _title.ForeColor = c.Text;
        _title.Font = _compact ? Theme.BodyStrong : Theme.Title;
        _summary.ForeColor = c.TextMuted;
        _summary.Font = Theme.Caption;
        _count.BackColor = c.Background;
        _count.ForeColor = c.TextMuted;
        _count.Font = _compact ? Theme.Caption : Theme.Body;
        _count.Margin = Padding.Empty;
        _count.Padding = Padding.Empty;
        _count.TextAlign = ContentAlignment.MiddleLeft;
        _pageNumber.BackColor = c.Background;
        _pageNumber.ForeColor = c.Text;
        _rowsLabel.BackColor = c.Background;
        _rowsLabel.ForeColor = c.TextMuted;
        foreach (var button in new[] { _prev, _next })
        {
            button.BackColor = c.Surface;
            button.ForeColor = c.Text;
            button.Font = Theme.BodyStrong;
            button.FlatAppearance.BorderColor = c.Border;
            button.FlatAppearance.BorderSize = 1;
        }
        _manageRoles.IconColor = c.Accent; _manageRoles.BackColor = c.Surface; _manageRoles.ForeColor = c.Text; _manageRoles.Font = Theme.BodyStrong; _manageRoles.FlatAppearance.BorderColor = c.Border; _manageRoles.FlatAppearance.MouseOverBackColor = c.SurfaceAlt; _manageRoles.FlatAppearance.MouseDownBackColor = c.AccentSoft;
        _add.BackColor = c.Accent;
        _add.ForeColor = c.OnAccent;
        _add.FlatAppearance.BorderSize = 0;
        _add.Font = _compact ? new Font("Segoe UI", 18f) : Theme.BodyStrong;
        _add.FlatAppearance.MouseOverBackColor = ControlPaint.Light(c.Accent, 0.08f);
        _add.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(c.Accent, 0.08f);
        _filter.ApplyTheme();
        _sortControl.ApplyTheme();
        _rows.ApplyTheme();
        _grid.ApplyTheme();
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
        _grid.ClearRowSelection();
        _grid.Invalidate();
    }
}

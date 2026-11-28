using FontAwesome.Sharp;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Security;
using LiteraHub.Data.Repositories;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

public sealed class StudentAccountsPage : ThemedUserControl
{
    private readonly StudentAccountRepository _repository;
    private readonly IPermissionService _permissions;
    private readonly DetailsPageHeader _header = new() { Dock = DockStyle.Fill, HeadingText = "Student Accounts", SubtitleText = "", AlignBackToRightEdge = true };
    private readonly Panel _titleHost = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly Label _title = new() { Text = "Student Accounts", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly CenteredIconButton _back = new() { Text = "Back", Dock = DockStyle.Right, Width = 100, IconChar = IconChar.ChevronLeft, IconSize = 16, TextImageRelation = TextImageRelation.ImageBeforeText, FlatStyle = FlatStyle.Flat };
    private readonly CatalogSortButton _sort = new();
    private readonly TableLayoutPanel _details = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(Space.L), Visible = false };
    private readonly DetailsActionFooter _footer = new() { UseStudentSizing = true };
    private readonly CenteredIconButton _cancel = new() { Text = "Cancel", IconChar = IconChar.Times, IconSize = 16, TextImageRelation = TextImageRelation.ImageBeforeText, FlatStyle = FlatStyle.Flat };
    private readonly ThemedSectionCard _detailCard = new(new Label { Text = "Account Information" }, IconChar.User);
    private readonly FlowLayoutPanel _detailBody = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Margin = Padding.Empty };
    private readonly List<Label> _detailValues = new();
    private readonly ThemedFilterDropdown _reason = new(new[] { "Identity Not Verified", "Not Eligible" });
    private readonly Label _reasonLabel = new() { Text = "Rejection Reason", AutoSize = false };
    private StudentAccountRow? _detailAccount;
    private StudentRegistrationRow? _detailRegistration;
    private bool _detailOpen;
    private int _sortIndex;
    private readonly ModuleSummaryCardStrip _summary = new();
    private readonly ThemedDataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnlyDisplayMode = false, Margin = Padding.Empty };
    private readonly ThemedSearchBox _search = new() { Dock = DockStyle.Fill, Placeholder = "Search student ID, name, username or status…", Margin = new Padding(0, 4, 8, 4) };
    private readonly ThemedFilterDropdown _accountFilter = new(new[] { "All", "Pending review", "Linked", "No account", "Archived" }) { Dock = DockStyle.Fill };
    private readonly ThemedFilterDropdown _registrationFilter = new(new[] { "All", "Pending", "Approved", "Rejected", "Expired" }) { Dock = DockStyle.Fill };
    private readonly TableLayoutPanel _layout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(Space.L), Margin = Padding.Empty };
    private readonly TableLayoutPanel _toolbar = new() { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = Padding.Empty };
    private readonly Panel _filters = new() { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 8, 4) };
    private readonly FlowLayoutPanel _tabs = new() { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
    private readonly FlowLayoutPanel _paging = new() { Dock = DockStyle.Right, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Width = 300, Margin = Padding.Empty };
    private readonly ThemedFilterDropdown _pageSize = new(new[] { "10", "25", "50", "100" }) { Size = new Size(68, 32) };
    private readonly Label _rowsLabel = new() { Text = "Rows:", Size = new Size(42, 32), TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _status = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, UseMnemonic = false };
    private readonly Label _count = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly Label _pageNumber = new() { Width = 64, Height = 32, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Button _accounts = Make("Accounts", 102), _registrations = Make("Registrations", 116), _refresh = Make("Refresh", 84);
    private readonly CenteredIconButton _provision = ActionButton("Create Account", IconChar.UserPlus), _reject = ActionButton("Reject", IconChar.Times);
    private readonly CenteredIconButton _active = ActionButton("Archive Account", IconChar.Archive), _reset = ActionButton("Reset Credentials", IconChar.Key), _unlock = ActionButton("Unlock Account", IconChar.Unlock);
    private readonly Button _prev = Make("‹", 36), _next = Make("›", 36);
    private IReadOnlyList<StudentAccountRow> _allAccounts = Array.Empty<StudentAccountRow>();
    private IReadOnlyList<StudentRegistrationRow> _allRegistrations = Array.Empty<StudentRegistrationRow>();
    private bool _registrationsMode, _loaded, _busy, _rendering;
    private int _page = 1;
    private int PageSize => new[] { 10, 25, 50, 100 }[_pageSize.SelectedIndex];
    public bool IsBusy => _busy;
    public event EventHandler? BackRequested;

    public StudentAccountsPage(StudentAccountRepository repository, IPermissionService permissions)
    {
        _repository = repository; _permissions = permissions;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int h in new[] { 50, ModuleSummaryCardStrip.PreferredRowHeight, 40, 56, 32, 0, 60 })
            _layout.RowStyles.Add(h == 0 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, h));
        _titleHost.Controls.Add(_title); _titleHost.Controls.Add(_back); _back.BringToFront();
        _layout.Controls.Add(_titleHost, 0, 0); _layout.Controls.Add(_summary, 0, 1); _layout.Controls.Add(_tabs, 0, 2); _layout.Controls.Add(_toolbar, 0, 3);
        _layout.Controls.Add(_status, 0, 4); _layout.Controls.Add(_grid, 0, 5);
        var pagination = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        pagination.Padding = new Padding(0, 12, 0, 0);
        pagination.Controls.Add(_count); pagination.Controls.Add(_paging); _paging.Controls.AddRange(new Control[] { _next, _pageNumber, _prev, _pageSize, _rowsLabel });
        foreach (Control child in _paging.Controls) child.Margin = new Padding(4, 8, 0, 0);
        _prev.Size = _next.Size = new Size(36, 32);
        _layout.Controls.Add(pagination, 0, 6); Controls.Add(_layout);
        foreach (float w in new[] { 0f, 148f, 56f, 112f }) _toolbar.ColumnStyles.Add(w == 0 ? new ColumnStyle(SizeType.Percent, 100) : new ColumnStyle(SizeType.Absolute, w));
        _toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _tabs.Controls.AddRange(new Control[] { _accounts, _registrations });
        _accounts.Height = _registrations.Height = 32;
        _refresh.Dock = DockStyle.Fill; _refresh.Margin = new Padding(0, 4, 0, 4);
        _toolbar.Controls.Add(_search, 0, 0); _toolbar.Controls.Add(_filters, 1, 0); _toolbar.Controls.Add(_sort, 2, 0); _toolbar.Controls.Add(_refresh, 3, 0);
        _sort.Configure(new[] { "Student ID (A–Z)", "Student ID (Z–A)", "Full Name (A–Z)", "Full Name (Z–A)", "Status (A–Z)" }, () => _sortIndex, i => { _sortIndex = i; _page = 1; Render(); }); _filters.Controls.Add(_registrationFilter); _filters.Controls.Add(_accountFilter);
        _registrationFilter.Visible = false;
        _details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _details.RowStyles.Add(new RowStyle(SizeType.Absolute, 98)); _details.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); _details.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        _details.Controls.Add(_header, 0, 0); _details.Controls.Add(_detailBody, 0, 1); _details.Controls.Add(_footer, 0, 2);
        _detailBody.Controls.Add(_detailCard); _detailBody.Resize += (_, _) => ArrangeDetail();
        _footer.Controls.AddRange(new Control[] { _provision, _reject, _active, _reset, _unlock, _cancel });
        _cancel.Click += (_, _) => CloseDetail(); Controls.Add(_details);
        _back.Click += (_, _) => { if (!_busy) BackRequested?.Invoke(this, EventArgs.Empty); };
        _summary.Add("Linked accounts", IconChar.UserGraduate, () => Choose(false, 2));
        _summary.Add("Pending review", IconChar.Clock, () => Choose(true, 1));
        _summary.Add("No account", IconChar.UserPlus, () => Choose(false, 3));
        _header.BackRequested += (_, _) => CloseDetail();
        _accounts.Click += (_, _) => Choose(false, 0); _registrations.Click += (_, _) => Choose(true, 0);
        _refresh.Click += async (_, _) => await ReloadAsync();
        _search.TextChanged += (_, _) => { _page = 1; Render(); };
        _accountFilter.SelectedIndexChanged += (_, _) => { _page = 1; Render(); }; _registrationFilter.SelectedIndexChanged += (_, _) => { _page = 1; Render(); };
        _grid.CellClick += (_, e) => { if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == "Actions") OpenDetail(_grid.Rows[e.RowIndex].DataBoundItem); };
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenDetail(_grid.Rows[e.RowIndex].DataBoundItem); };
        _grid.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter && _grid.CurrentRow is { } row) { e.Handled = true; e.SuppressKeyPress = true; OpenDetail(row.DataBoundItem); } };
        _grid.CellPainting += PaintChevron;
        _pageSize.SelectedIndexChanged += (_, _) => { _page = 1; Render(); };
        _prev.Click += (_, _) => { _page--; Render(); }; _next.Click += (_, _) => { _page++; Render(); };
        _provision.Click += async (_, _) => await ProvisionAsync();
        _reject.Click += async (_, _) => await RejectAsync(_reason.SelectedIndex == 1);
        _active.Click += async (_, _) => await ChangeActiveAsync(); _reset.Click += async (_, _) => await ResetAsync(); _unlock.Click += async (_, _) => await UnlockAsync();
        Resize += (_, _) => ArrangeDetail();
        ConfigureColumns(); RefreshActions(); ApplyTheme();
    }
    protected override async void OnLoad(EventArgs e) { base.OnLoad(e); await ReloadAsync(); }
    private static Button Make(string text, int width) => new() { Text = text, Width = width, Height = 36, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 4, 8, 4), UseMnemonic = false };
    private static CenteredIconButton ActionButton(string text, IconChar icon) => new() { Text = text, IconChar = icon, IconSize = 16, TextImageRelation = TextImageRelation.ImageBeforeText, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
    private void Choose(bool registrations, int filter)
    {
        if (_busy) return;
        _rendering = true; _registrationsMode = registrations; _registrationFilter.Visible = registrations; _accountFilter.Visible = !registrations;
        if (registrations) { _registrationFilter.BringToFront(); _registrationFilter.SelectedIndex = filter; }
        else { _accountFilter.BringToFront(); _accountFilter.SelectedIndex = filter; }
        _page = 1; ConfigureColumns(); Render(); ApplyTheme();
    }
    private void ConfigureColumns()
    {
        _rendering = true;
        try
        {
            _grid.DataSource = null; _grid.Columns.Clear();
            if (_registrationsMode)
            {
                Column("RegistrationId", "Request ID", 12, 70); Column("StudentNo", "Student ID", 18, 100); Column("FullName", "Full Name", 24, 140);
                Column("Status", "Status", 15, 90); Column("SubmittedAt", "Submitted (UTC)", 18, 110, "MMM d, HH:mm"); Column("ExpiresAt", "Expires (UTC)", 18, 110, "MMM d, HH:mm");
            }
            else
            {
                Column("StudentNo", "Student ID", 18, 110); Column("FullName", "Full Name", 26, 150); Column("Username", "Username", 20, 115);
                Column("AccountStatus", "Account Status", 26, 130); Column("UserId", "Account ID", 10, 70);
            }
        }

        finally { _rendering = false; }
        var chevron = new DataGridViewTextBoxColumn { Name = "Actions", HeaderText = "", Width = 55, MinimumWidth = 55, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, SortMode = DataGridViewColumnSortMode.NotSortable, ToolTipText = "View Details" };
        _grid.Columns.Add(chevron);
    }
    private void Column(string property, string title, float weight, int minimum, string? format = null)
    {
        var c = new DataGridViewTextBoxColumn { Name = property, DataPropertyName = property, HeaderText = title, FillWeight = weight, MinimumWidth = minimum, SortMode = DataGridViewColumnSortMode.NotSortable };
        if (format is not null) c.DefaultCellStyle.Format = format;
        c.DefaultCellStyle.NullValue = "—"; _grid.Columns.Add(c);
    }
    private async Task ReloadAsync()
    {
        if (_busy) return;
        _busy = true; _loaded = false; _summary.SetLoading(); _status.Text = "Loading student accounts…"; RefreshActions();
        try { await LoadDataAsync(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.GetType().Name); if (!IsDisposed) { _loaded = false; _grid.DataSource = null; _summary.SetUnavailable(); _status.Text = "Unable to load accounts. Select Refresh to retry."; } }
        finally { _busy = false; if (!IsDisposed) { if (_loaded) Render(); else RefreshActions(); } }
    }
    private async Task LoadDataAsync()
    {
        _permissions.Demand(RolePermission.StudentsManageAccounts);
        var accounts = await _repository.GetAccountsAsync();
        var registrations = await _repository.GetRegistrationsAsync();
        if (IsDisposed || Disposing) return;
        _allAccounts = accounts; _allRegistrations = registrations; _loaded = true;
        _summary.SetValues(accounts.Count(x => x.IsStudentAccount), registrations.Count(x => x.Status == "Pending"), accounts.Count(x => x.UserId is null));
    }
    private static bool Contains(string? value, string query) => value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
    private void Render()
    {
        if (!_loaded || IsDisposed || _busy || _rendering) return;
        string q = _search.Text.Trim(); int total;
        _rendering = true;
        try
        {
            if (_registrationsMode)
            {
                string? status = _registrationFilter.SelectedIndex == 0 ? null : new[] { "", "Pending", "Approved", "Rejected", "Expired" }[_registrationFilter.SelectedIndex];
                var rows = _allRegistrations.Where(x => (status is null || x.Status == status) && (q.Length == 0 || Contains(x.StudentNo, q) || Contains(x.FullName, q) || Contains(x.Status, q) || Contains(x.RegistrationId.ToString(), q))).ToList();
                total = rows.Count; ClampPage(total); _grid.DataSource = Sort(rows, x => x.StudentNo, x => x.FullName, x => x.Status).Skip((_page - 1) * PageSize).Take(PageSize).ToList();
            }
            else
            {
                var rows = _allAccounts.Where(x => (_accountFilter.SelectedIndex switch { 1 => x.PendingRegistrationId.HasValue, 2 => x.IsStudentAccount, 3 => x.UserId is null, 4 => !x.StudentActive || (x.UserId.HasValue && !x.AccountActive), _ => true }) && (q.Length == 0 || Contains(x.StudentNo, q) || Contains(x.FullName, q) || Contains(x.Username, q) || Contains(x.AccountStatus, q))).ToList();
                total = rows.Count; ClampPage(total); _grid.DataSource = Sort(rows, x => x.StudentNo, x => x.FullName, x => x.AccountStatus).Skip((_page - 1) * PageSize).Take(PageSize).ToList();
            }
            _grid.ClearRowSelection();
        }
        finally { _rendering = false; }
        _count.Text = total == 0 ? "Showing 0 of 0" : $"Showing {(_page - 1) * PageSize + 1}–{Math.Min(total, _page * PageSize)} of {total}";
        _pageNumber.Text = $"{_page} / {Math.Max(1, (total + PageSize - 1) / PageSize)}";
        _prev.Enabled = _page > 1; _next.Enabled = _page * PageSize < total;
        _summary.SetSelected(_registrationsMode && _registrationFilter.SelectedIndex == 1 ? 1 : !_registrationsMode && _accountFilter.SelectedIndex == 2 ? 0 : !_registrationsMode && _accountFilter.SelectedIndex == 3 ? 2 : -1);
        RefreshActions();
    }
    private void ClampPage(int total) => _page = Math.Clamp(_page, 1, Math.Max(1, (total + PageSize - 1) / PageSize));
    private StudentRegistrationRow? SelectedRegistration => _detailOpen ? _detailRegistration : null;
    private StudentAccountRow? SelectedAccount => _detailOpen ? _detailAccount : null;
    private StudentAccountRow? ProvisionTarget => SelectedAccount ?? (SelectedRegistration?.StudentId is int id ? _allAccounts.FirstOrDefault(x => x.StudentId == id) : null);
    private int? SelectedPending => SelectedRegistration is { Status: "Pending" } registration ? registration.RegistrationId : SelectedAccount?.PendingRegistrationId;
    private bool CanManage => !_busy && _loaded && _permissions.Can(RolePermission.StudentsManageAccounts);
    private void RefreshActions()
    {
        bool enabled = !_busy && _loaded;
        _header.BackEnabled = !_busy; _back.Enabled = !_busy; _cancel.Enabled = !_busy; _refresh.Enabled = !_busy; _accounts.Enabled = _registrations.Enabled = _search.Enabled = _grid.Enabled = _sort.Enabled = _pageSize.Enabled = enabled;
        _accountFilter.Enabled = _registrationFilter.Enabled = enabled;
        if (_busy) { _prev.Enabled = _next.Enabled = false; }
        var target = ProvisionTarget; var account = SelectedAccount; bool pending = SelectedPending.HasValue;
        _provision.Visible = enabled && target is { StudentActive: true, UserId: null } && (!_registrationsMode || pending);
        _provision.Text = pending ? "Approve Registration" : "Create Account";
        _reject.Visible = enabled && pending; _reason.Visible = _reasonLabel.Visible = _reject.Visible;
        _active.Visible = enabled && account is { IsStudentAccount: true } && (account.AccountActive || account.StudentActive && account.RoleActive);
        _active.Text = account?.AccountActive == true ? "Archive Account" : "Restore Account";
        _reset.Visible = enabled && account is { IsStudentAccount: true, AccountActive: true, StudentActive: true, RoleActive: true };
        _unlock.Visible = _reset.Visible && (account?.LoginLocked == true || account?.RecoveryLocked == true);
        _provision.IconChar = pending ? IconChar.Check : IconChar.UserPlus;
        _active.IconChar = account?.AccountActive == true ? IconChar.Archive : IconChar.Undo;
        if (_detailOpen)
        {
            _footer.Configure(pending ? _reject : _unlock, _active, _provision.Visible ? _provision : _reset, _cancel, () => false);
            _footer.ArrangeActions(); ArrangeDetail();
        }
        if (!_busy && _loaded)
        {
            _status.Text = SelectedRegistration is { } r ? $"Request #{r.RegistrationId} · {r.Status} · Reviewer: {r.ReviewerUserId?.ToString() ?? "—"} · {r.DecisionCode ?? "Awaiting review"}" :
                account is not null ? $"{account.FullName} · Student record: {(account.StudentActive ? "Active" : "Archived")} · {account.AccountStatus}{(account.UserId.HasValue ? (account.HasPasskey ? " · Passkey set" : " · No passkey") : "")}" : _registrationsMode ? $"{_allRegistrations.Count} registration records. Open View Details to review." : $"{_allAccounts.Count} student records. Open View Details to manage.";
        }
    }
    private IEnumerable<T> Sort<T>(IEnumerable<T> rows, Func<T, string?> id, Func<T, string?> name, Func<T, string?> status) => _sortIndex switch
    {
        1 => rows.OrderByDescending(id, StringComparer.OrdinalIgnoreCase),
        2 => rows.OrderBy(name, StringComparer.OrdinalIgnoreCase),
        3 => rows.OrderByDescending(name, StringComparer.OrdinalIgnoreCase),
        4 => rows.OrderBy(status, StringComparer.OrdinalIgnoreCase).ThenBy(id, StringComparer.OrdinalIgnoreCase),
        _ => rows.OrderBy(id, StringComparer.OrdinalIgnoreCase)
    };
    private void PaintChevron(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "Actions" || e.Graphics is not Graphics g) return;
        e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border | DataGridViewPaintParts.SelectionBackground);
        var b = e.CellBounds; int x = b.Left + b.Width / 2, y = b.Top + b.Height / 2;
        var mode = g.SmoothingMode; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var pen = new Pen(Theme.Colors.Accent, 2.2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round, LineJoin = System.Drawing.Drawing2D.LineJoin.Round };
        g.DrawLines(pen, new[] { new Point(x - 4, y - 7), new Point(x + 3, y), new Point(x - 4, y + 7) }); g.SmoothingMode = mode; e.Handled = true;
    }
    private void OpenDetail(object? row)
    {
        if (!CanManage || _detailOpen || row is not StudentAccountRow && row is not StudentRegistrationRow) return;
        _detailAccount = row as StudentAccountRow; _detailRegistration = row as StudentRegistrationRow; _detailOpen = true;
        foreach (var label in _detailValues) { _detailCard.Controls.Remove(label); label.Dispose(); }
        _detailValues.Clear();
        var account = ProvisionTarget; var request = _detailRegistration ?? (_detailAccount?.PendingRegistrationId is int id ? _allRegistrations.FirstOrDefault(x => x.RegistrationId == id) : null);
        _header.HeadingText = request is not null ? "Registration Details" : "Student Account Details";
        _header.SubtitleText = "Review the verified student identity and account information.";
        AddDetail("Student ID", account?.StudentNo ?? request?.StudentNo); AddDetail("Full Name", account?.FullName ?? request?.FullName);
        AddDetail("Username", account?.Username); AddDetail("Account Status", account?.AccountStatus);
        if (request is not null) { AddDetail("Request ID", request.RegistrationId.ToString()); AddDetail("Registration Status", request.Status); AddDetail("Submitted (UTC)", request.SubmittedAt.ToString("MMM d, yyyy HH:mm")); AddDetail("Decision", request.DecisionCode switch { "IdentityNotVerified" => "Identity Not Verified", "NotEligible" => "Not Eligible", "IdentityVerified" => "Identity Verified", _ => request.DecisionCode }); }
        else { AddDetail("Account ID", account?.UserId?.ToString()); AddDetail("Recovery Passkey", account?.HasPasskey == true ? "Configured" : "Not Configured"); }
        if (!_detailCard.Controls.Contains(_reason)) _detailCard.AddFields(_reasonLabel, _reason);
        _reason.SelectedIndex = 0; _layout.Visible = false; _details.Visible = true; _details.BringToFront(); RefreshActions(); ApplyTheme(); ArrangeDetail();
    }
    private void AddDetail(string caption, string? text)
    {
        var label = new Label { Text = caption + ":   " + (string.IsNullOrWhiteSpace(text) ? "—" : text), AutoSize = false, UseMnemonic = false, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 3, 12, 3) };
        label.Paint += (_, e) => { using var pen = new Pen(Theme.Colors.Border); e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, label.Width - 1), Math.Max(0, label.Height - 1)); };
        _detailValues.Add(label); _detailCard.AddFields(label);
    }
    private void ArrangeDetail()
    {
        if (!_detailOpen || _detailBody.ClientSize.Width < 80) return;
        int w = Math.Max(60, _detailBody.ClientSize.Width - 22), pad = 16, gap = 12; bool two = w >= 660; int fw = two ? (w - 2 * pad - gap) / 2 : w - 2 * pad;
        int y = _detailCard.ArrangeHeading(w, pad);
        for (int i = 0; i < _detailValues.Count; i++) { int col = two ? i % 2 : 0, row = two ? i / 2 : i; _detailValues[i].SetBounds(pad + col * (fw + gap), y + row * 50, fw, 40); }
        y += ((_detailValues.Count + (two ? 1 : 0)) / (two ? 2 : 1)) * 50;
        if (_reason.Visible) { _reasonLabel.SetBounds(pad, y, fw, 22); _reason.SetBounds(pad, y + 24, fw, 40); y += 76; }
        _detailCard.Size = new Size(w, y + pad); _footer.ArrangeActions();
    }
    private void CloseDetail()
    {
        if (_busy || !_detailOpen) return;
        _detailOpen = false; _detailAccount = null; _detailRegistration = null; _details.Visible = false; _layout.Visible = true; _layout.BringToFront(); Render();
    }
    private async Task ProvisionAsync()
    {
        if (!CanManage || ProvisionTarget is not { StudentActive: true, UserId: null } target) return;
        int? registrationId = SelectedPending;
        if (_registrationsMode && registrationId is null) return;
        using var dialog = new StudentProvisionDialog(target.StudentNo, target.FullName, registrationId.HasValue);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        await MutateAsync(async () => {
            var credentials = await _repository.ProvisionAsync(target.StudentId, dialog.Username, true, registrationId);
            StaffCredentialsDialog.ShowCredentials(FindForm(), credentials.Username, credentials.TemporaryPassword, credentials.RecoveryPasskey, "Student Account Created");
        });
    }
    private async Task RejectAsync(bool notEligible)
    {
        if (!CanManage || SelectedPending is not int id) return;
        string reason = notEligible ? "student not eligible" : "identity not verified";
        if (!ThemedMessageDialog.Confirm(FindForm(), "Reject Registration", $"Reject request #{id}: {reason}? The review will be retained.", true, "Reject")) return;
        await MutateAsync(() => _repository.RejectAsync(id, notEligible));
    }
    private async Task ChangeActiveAsync()
    {
        if (!CanManage || SelectedAccount is not { IsStudentAccount: true, UserId: int id } account) return;
        bool active = !account.AccountActive; string action = active ? "Restore" : "Archive";
        if (!ThemedMessageDialog.Confirm(FindForm(), action + " Student Account", active ? $"Restore the linked account for {account.FullName}? Only active students may sign in." : $"Archive the linked account for {account.FullName}? Existing sessions will be invalidated. History is retained.", !active, action)) return;
        await MutateAsync(() => _repository.SetActiveAsync(id, active));
    }
    private async Task ResetAsync()
    {
        if (!CanManage || SelectedAccount is not { IsStudentAccount: true, UserId: int id, AccountActive: true, StudentActive: true, RoleActive: true } account) return;
        if (!ThemedMessageDialog.Confirm(FindForm(), "Reset Student Credentials", $"Confirm you have personally verified the identity and enrollment of {account.FullName}. Resetting issues a new temporary password and passkey, invalidates the old credentials and ends existing sessions.", true, "Verified — Reset")) return;
        await MutateAsync(async () => {
            var credentials = await _repository.ResetCredentialsAsync(id, true);
            StaffCredentialsDialog.ShowCredentials(FindForm(), credentials.Username, credentials.TemporaryPassword, credentials.RecoveryPasskey, "Student Credentials Reset");
        });
    }
    private async Task UnlockAsync()
    {
        if (!CanManage || SelectedAccount is not { IsStudentAccount: true, UserId: int id } account) return;
        if (!ThemedMessageDialog.Confirm(FindForm(), "Unlock Student Account", $"Confirm that you verified {account.FullName}'s identity. Clear login and recovery attempt limits?", false, "Verified — Unlock")) return;
        await MutateAsync(() => _repository.UnlockAsync(id, true));
    }
    private async Task MutateAsync(Func<Task> operation)
    {
        if (_busy) return;
        _busy = true; _status.Text = "Saving changes…"; RefreshActions();
        try
        {
            await operation();
            if (!IsDisposed) CatalogActionFeedback.Notify(this, "Student account updated successfully.");
        }
        catch (UnauthorizedAccessException) { if (!IsDisposed) ThemedMessageDialog.ShowAlert(FindForm(), "Student Accounts", "You no longer have permission for this action.", true); }
        catch (InvalidOperationException ex) { if (!IsDisposed) ThemedMessageDialog.ShowAlert(FindForm(), "Student Accounts", ex.Message, true); }
        catch (ArgumentException ex) { if (!IsDisposed) ThemedMessageDialog.ShowAlert(FindForm(), "Student Accounts", ex.Message, true); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.GetType().Name); if (!IsDisposed) ThemedMessageDialog.ShowAlert(FindForm(), "Student Accounts", "The operation could not be completed. Refresh before retrying.", true); }
        finally { _busy = false; }
        if (!IsDisposed) { CloseDetail(); await ReloadAsync(); }
    }
    protected override void ApplyTheme()
    {
        base.ApplyTheme(); var c = Theme.Colors;
        foreach (var p in new Control[] { _layout, _toolbar, _tabs, _paging, _filters, _details, _detailBody, _titleHost }) p.BackColor = c.Background;
        _title.Font = Theme.Title; _title.ForeColor = c.Text; _rowsLabel.Font = Theme.Caption; _rowsLabel.ForeColor = c.TextMuted;
        _reasonLabel.Font = Theme.Body; _reasonLabel.ForeColor = c.Text; _reasonLabel.BackColor = c.Surface;
        foreach (var v in _detailValues) { v.Font = Theme.Body; v.BackColor = c.SurfaceAlt; v.ForeColor = c.Text; v.Invalidate(); }
        _status.ForeColor = _count.ForeColor = _pageNumber.ForeColor = c.TextMuted; _status.Font = Theme.Caption; _count.Font = Theme.Body; _pageNumber.Font = Theme.Body;
        foreach (Button b in new Button[] { _accounts, _registrations, _refresh, _provision, _reject, _active, _reset, _unlock, _prev, _next, _back, _cancel })
        {
            bool primary = b == _provision || b == (_registrationsMode ? _registrations : _accounts);
            b.Font = Theme.BodyStrong; b.BackColor = primary ? c.Accent : c.Surface; b.ForeColor = primary ? c.OnAccent : c.Text;
            if (b == _reject) b.ForeColor = c.Danger;
            if (b is IconButton icon) icon.IconColor = b == _reject ? c.Danger : primary ? c.OnAccent : c.Accent;
            b.FlatAppearance.BorderColor = primary ? c.Accent : c.Border; b.FlatAppearance.MouseOverBackColor = primary ? ControlPaint.Light(c.Accent, .08f) : c.AccentSoft;
        }
        _header.ApplyTheme(); _grid.ApplyTheme(); _sort.ApplyTheme();
    }
}

using System.Drawing.Drawing2D;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

/// <summary>Student directory, styled and navigated consistently with Books.</summary>
public sealed class StudentsPage : ThemedUserControl
{
    private readonly IStudentService _service;
    private readonly IPermissionService _permissions;
    private readonly Panel _host = new() { Dock = DockStyle.Fill };
    private readonly TableLayoutPanel _layout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(Space.L), Margin = Padding.Empty };
    private readonly TableLayoutPanel _toolbar = new() { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly Label _heading = new() { Text = "Students", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ThemedSearchBox _search = new() { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 8, 4), Placeholder = "Search student ID, name, email or address..." };
    private readonly ThemedStudentsFilterSort _filter = new() { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 8, 4) };
    private readonly Button _add = new() { Text = "+ Add Student", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 4, 0, 4) };
    private readonly Label _status = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly ThemedDataGridView _grid = new() { Dock = DockStyle.Fill, Margin = Padding.Empty, ReadOnlyDisplayMode = true };
    private readonly TableLayoutPanel _pagination = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = new Padding(0, 12, 0, 0) };
    private readonly Label _pageInfo = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly FlowLayoutPanel _pageControls = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly Button _previous = new() { Text = "‹", Size = new Size(36, 32), FlatStyle = FlatStyle.Flat };
    private readonly Button _next = new() { Text = "›", Size = new Size(36, 32), FlatStyle = FlatStyle.Flat };
    private readonly Label _pageNumber = new() { Size = new Size(64, 32), TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _rowsLabel = new() { Text = "Rows:", AutoSize = false, Size = new Size(42, 32), TextAlign = ContentAlignment.MiddleCenter };
    private readonly ThemedFilterDropdown _pageSize = new(new[] { "10", "25", "50", "100" }) { Size = new Size(68, 32) };
    private readonly List<Student> _all = new();
    private List<Student> _filtered = new();
    private StudentDetailsPage? _details;
    private int _page = 1;
    private int _size = 10;
    private bool _loading;
    private bool _compact;
    private bool _responsiveInitialized;

    public StudentsPage(IStudentService service, IPermissionService permissions)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        Dock = DockStyle.Fill;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int height in new[] { 50, 56, 36, 0, 60 })
            _layout.RowStyles.Add(height == 0 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, height));
        _toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        _toolbar.Controls.Add(_search, 0, 0);
        _toolbar.Controls.Add(_filter, 1, 0);
        _toolbar.Controls.Add(_add, 2, 0);
        _pagination.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _pagination.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        _pagination.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _pageControls.Margin = Padding.Empty;
        _pageControls.Padding = Padding.Empty;
        _pageControls.Controls.AddRange(new Control[] { _next, _pageNumber, _previous, _pageSize, _rowsLabel });
        foreach (Control control in _pageControls.Controls) control.Margin = new Padding(4, 8, 0, 0);
        _pagination.Controls.Add(_pageInfo, 0, 0);
        _pagination.Controls.Add(_pageControls, 1, 0);
        _layout.Controls.Add(_heading, 0, 0);
        _layout.Controls.Add(_toolbar, 0, 1);
        _layout.Controls.Add(_status, 0, 2);
        _layout.Controls.Add(_grid, 0, 3);
        _layout.Controls.Add(_pagination, 0, 4);
        _host.Controls.Add(_layout);
        Controls.Add(_host);
        AddColumn("StudentNo", "Student ID", nameof(Student.StudentNo), 14, 110);
        AddColumn("FullName", "Full name", null, 24, 175);
        AddColumn("Gender", "Gender", nameof(Student.Gender), 10, 85);
        AddColumn("MobileNo", "Mobile", nameof(Student.MobileNo), 14, 120);
        AddColumn("Email", "Email", nameof(Student.Email), 20, 160);
        AddColumn("Status", "Status", null, 10, 90);
        AddColumn("Actions", "", null, 6, 55);
        // Shared grid styling is owned by ThemedDataGridView.
        // Native cell borders are disabled because the horizontal divider
        // is painted once per Student cell by PaintStudentCell.
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
        _grid.AutoGenerateColumns = false;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;
        _grid.DataSource = new List<Student>();
        _grid.DataBindingComplete += (_, _) =>
        {
            _grid.ClearRowSelection();
        };
        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count || _grid.Rows[e.RowIndex].DataBoundItem is not Student student) return;
            string column = _grid.Columns[e.ColumnIndex].Name;
            if (column == "FullName") { e.Value = FullName(student); e.FormattingApplied = true; }
            if (column == "Status") { e.Value = student.IsActive ? "Active" : "Archived"; e.FormattingApplied = true; }
        };
        foreach (string name in new[] { "Status", "Actions" })
        {
            _grid.Columns[name]!.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _grid.Columns[name]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _grid.Columns[name]!.HeaderCell.Style.Padding = Padding.Empty;
            _grid.Columns[name]!.DefaultCellStyle.Padding = Padding.Empty;
        }
        _grid.CellPainting += PaintStudentCell;
        _grid.CellMouseUp += async (_, e) =>
        {
            if (e.Button == MouseButtons.Left && e.RowIndex >= 0 && e.ColumnIndex >= 0 &&
                _grid.Columns[e.ColumnIndex].Name == "Actions" && e.RowIndex < _grid.Rows.Count && _grid.Rows[e.RowIndex].DataBoundItem is Student student)
                await OpenDetailsAsync(student.StudentId);
        };
        _grid.CellMouseMove += (_, e) => _grid.Cursor = e.RowIndex >= 0 && e.ColumnIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == "Actions" ? Cursors.Hand : Cursors.Default;
        // Use native DataGridView cell tooltips rather than showing and hiding
        // a separate ToolTip in CellMouseEnter/Leave. The Status and Actions
        // cells are custom-painted, but remain normal bound grid cells.
        _grid.ShowCellToolTips = true;
        _grid.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                e.RowIndex >= _grid.Rows.Count ||
                e.ColumnIndex >= _grid.Columns.Count ||
                _grid.Rows[e.RowIndex].DataBoundItem is not Student student)
                return;

            e.ToolTipText = _grid.Columns[e.ColumnIndex].Name switch
            {
                "Actions" => "View Details",
                "Status" => student.IsActive ? "Active" : "Inactive",
                _ => string.Empty
            };
        };
        _grid.MouseLeave += (_, _) => _grid.Cursor = Cursors.Default;
        _search.TextChanged += (_, _) => Filter();
        _filter.SelectionChanged += (_, _) => Filter();
        _pageSize.SelectedIndexChanged += (_, _) => { _size = new[] { 10, 25, 50, 100 }[_pageSize.SelectedIndex]; _page = 1; Render(); };
        _previous.Click += (_, _) => { _page--; Render(); };
        _next.Click += (_, _) => { _page++; Render(); };
        _add.Click += async (_, _) => await OpenDetailsAsync(null);
        Resize += (_, _) => UpdateResponsive();
        UpdateResponsive();
        ApplyTheme();
    }

    private void AddColumn(string name, string title, string? property, float weight, int minimum)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = title, DataPropertyName = property ?? "", FillWeight = weight, MinimumWidth = minimum, SortMode = DataGridViewColumnSortMode.NotSortable });
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (_loading) return;
        _loading = true;
        _add.Enabled = false;
        _status.Text = "Loading students...";
        try
        {
            var students = await _service.GetAllAsync(includeInactive: true);
            if (IsDisposed || Disposing) return;
            _all.Clear();
            _all.AddRange(students);
            Filter();
        }
        catch (Exception ex)
        {
            if (IsDisposed || Disposing) return;
            _status.Text = "Unable to load students.";
            MessageBox.Show(FindForm(), ex.Message, "Students", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _loading = false;
            if (!IsDisposed && !Disposing) _add.Enabled = true;
        }
    }

    private void Filter()
    {
        string query = _search.Text.Trim();
        IEnumerable<Student> items = _all.Where(s =>
            (_filter.StatusIndex == 0 || (_filter.StatusIndex == 1 ? s.IsActive : !s.IsActive)) &&
            (query.Length == 0 || s.StudentNo.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             FullName(s).Contains(query, StringComparison.OrdinalIgnoreCase) ||
             s.Email.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             Address(s).Contains(query, StringComparison.OrdinalIgnoreCase)));
        items = _filter.SortIndex switch
        {
            1 => items.OrderByDescending(FullName, StringComparer.OrdinalIgnoreCase),
            2 => items.OrderBy(s => s.StudentNo, StringComparer.OrdinalIgnoreCase),
            3 => items.OrderByDescending(s => s.StudentNo, StringComparer.OrdinalIgnoreCase),
            4 => items.OrderByDescending(s => s.CreatedAt),
            _ => items.OrderBy(FullName, StringComparer.OrdinalIgnoreCase)
        };
        _filtered = items.ToList();
        _page = 1;
        Render();
    }

    private void Render()
    {
        int pages = Math.Max(1, (_filtered.Count + _size - 1) / _size);
        _page = Math.Clamp(_page, 1, pages);
        _grid.DataSource = _filtered.Skip((_page - 1) * _size).Take(_size).ToList();
        _grid.ClearRowSelection();
        _status.Text = _all.Count == 0 ? "No matching students." : $"{_filtered.Count} of {_all.Count} students match.";
        _pageInfo.Text = _filtered.Count == 0 ? "Showing 0 students" : $"Showing {(_page - 1) * _size + 1}–{Math.Min(_page * _size, _filtered.Count)} of {_filtered.Count}";
        _pageNumber.Text = $"{_page} / {pages}";
        _previous.Enabled = _page > 1;
        _next.Enabled = _page < pages;
    }

    private async Task OpenDetailsAsync(int? id)
    {
        if (_details is not null || _loading) return;
        try
        {
            Student? student = id.HasValue ? await _service.GetByIdAsync(id.Value) : null;
            if (IsDisposed || Disposing) return;
            if (id.HasValue && student is null) { MessageBox.Show(FindForm(), "Student not found.", "Students"); await ReloadAsync(); return; }
            var details = new StudentDetailsPage(_service, student, _permissions.Can(Permission.HardDelete)) { Dock = DockStyle.Fill };
            _details = details;
            details.BackRequested += async (_, _) => { CloseDetails(); await ReloadAsync(); };
            _layout.Visible = false;
            _host.Controls.Add(details);
            details.BringToFront();
        }
        catch (Exception ex) { MessageBox.Show(FindForm(), ex.Message, "Students", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void CloseDetails()
    {
        if (_details is null) return;
        _host.Controls.Remove(_details);
        _details.Dispose();
        _details = null;
        _layout.Visible = true;
        _layout.BringToFront();
    }

    // Paint every Student cell as one horizontal-banded table. The shared
    // ThemedDataGridView still owns fonts, selection, scrolling and column sizing.
    // Painting content only (not Borders/Background) prevents the vertical
    // seams that were visible on alternating rows after navigation/theme refresh.
    private void PaintStudentCell(object? sender, DataGridViewCellPaintingEventArgs e)
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
                _grid.Rows[e.RowIndex].DataBoundItem is Student student &&
                student.IsActive;
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

    private static string FullName(Student s) => string.Join(" ", new[] { s.FirstName, s.MiddleName, s.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    private static string Address(Student s) => string.Join(", ", new[] { s.HouseNo, s.Street, s.Municipality, s.CityProvince, s.ZipCode }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private void UpdateResponsive()
    {
        bool compact = ClientSize.Width < 590;
        if (_responsiveInitialized && _compact == compact) return;
        _responsiveInitialized = true;
        _compact = compact;
        _toolbar.ColumnStyles[1].Width = compact ? 108 : 145;
        _toolbar.ColumnStyles[2].Width = compact ? 44 : 160;
        _add.Text = compact ? "+" : "+ Add Student";
        _add.Font = compact ? new Font("Segoe UI", 18f) : Theme.BodyStrong;
        _pagination.ColumnStyles[1].Width = compact ? 245 : 300;
        _heading.Font = compact ? Theme.BodyStrong : Theme.Title;
        _pageInfo.Font = compact ? Theme.Caption : Theme.Body;
        _grid.AutoSizeColumnsMode = compact ? DataGridViewAutoSizeColumnsMode.None : DataGridViewAutoSizeColumnsMode.Fill;
        if (compact)
            foreach (DataGridViewColumn column in _grid.Columns) column.Width = column.MinimumWidth;
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        foreach (Control control in new Control[] { _host, _layout, _toolbar, _pagination, _pageControls }) control.BackColor = c.Background;
        _heading.ForeColor = c.Text;
        _heading.Font = _compact ? Theme.BodyStrong : Theme.Title;
        _status.ForeColor = c.TextMuted;
        _status.Font = Theme.Caption;
        _pageInfo.BackColor = c.Background;
        _pageInfo.ForeColor = c.TextMuted;
        _pageInfo.Font = _compact ? Theme.Caption : Theme.Body;
        _pageInfo.Margin = Padding.Empty;
        _pageInfo.Padding = Padding.Empty;
        _pageInfo.TextAlign = ContentAlignment.MiddleLeft;
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
        _add.BackColor = c.Accent;
        _add.ForeColor = c.OnAccent;
        _add.FlatAppearance.BorderSize = 0;
        _add.Font = _compact ? new Font("Segoe UI", 18f) : Theme.BodyStrong;
        _add.FlatAppearance.MouseOverBackColor = ControlPaint.Light(c.Accent, 0.08f);
        _add.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(c.Accent, 0.08f);
        _filter.ApplyTheme();
        _pageSize.ApplyTheme();
        _grid.ApplyTheme();
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
        _grid.ClearRowSelection();
        _grid.Invalidate();
    }
}


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
        RowCount = 4,
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
        Text = "Book Categories",
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly ThemedSearchBox _search = new()
    {
        Dock = DockStyle.Fill
    };

    private readonly ComboBox _filter = new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = 24
    };

    private readonly Button _addButton = new()
    {
        Text = "+ Add Category",
        Dock = DockStyle.Fill,
        FlatStyle = FlatStyle.Flat
    };

    private readonly Label _status = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };

    private readonly ThemedDataGridView _grid = new();

    private readonly LoadingHost _loading = new()
    {
        Dock = DockStyle.Fill,
        SkeletonStyle = SkeletonStyle.Rows
    };

    private List<Category> _allCategories = new();

    private bool _loadStarted;
    private bool _compact;
    private bool _loadingData;
    private bool _working;

    private bool CanManage =>
        _permissions.Can(Permission.ManageLookups);

    public CategoriesPage(
        ICategoryService categories,
        IPermissionService permissions)
    {
        _categories = categories ??
            throw new ArgumentNullException(nameof(categories));

        _permissions = permissions ??
            throw new ArgumentNullException(nameof(permissions));

        Dock = DockStyle.Fill;
        Padding = new Padding(Space.L);

        BuildLayout();
        ConfigureGrid();

        _loading.SetContent(_grid);

        _filter.Items.AddRange(
            new object[] { "All", "Active", "Inactive" });

        _filter.SelectedIndex = 0;
        _filter.Visible = CanManage;

        _addButton.Visible =
            _permissions.Can(Permission.CreateLookups);

        _search.TextChanged += (_, _) => FilterCategories();

        _filter.SelectedIndexChanged += (_, _) =>
            FilterCategories();

        _filter.DrawItem += DrawFilterItem;

        _addButton.Click += async (_, _) =>
            await OpenDialogAsync(null);

        _grid.CellPainting += PaintSpecialCells;
        _grid.CellClick += HandleCellClick;
        _grid.CellToolTipTextNeeded += HandleCellTooltip;

        _status.Text = "Waiting to load categories...";

        Resize += (_, _) => UpdateResponsiveLayout();
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

        _toolbar.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _search.Margin = new Padding(0, 4, 8, 4);
        _filter.Margin = new Padding(0, 6, 8, 6);
        _addButton.Margin = new Padding(0, 4, 0, 4);

        _layout.Controls.Add(_heading, 0, 0);
        _layout.Controls.Add(_toolbar, 0, 1);
        _layout.Controls.Add(_status, 0, 2);
        _layout.Controls.Add(_loading, 0, 3);

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
            FillWeight = 30,
            MinimumWidth = 115
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Description",
            HeaderText = "Description",
            DataPropertyName = nameof(Category.Description),
            FillWeight = 50,
            MinimumWidth = 150
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "IsActive",
            HeaderText = "Active",
            DataPropertyName = nameof(Category.IsActive),
            FillWeight = 12,
            MinimumWidth = 75
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Actions",
            HeaderText = "Actions",
            FillWeight = 18,
            MinimumWidth = 105,
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
        int status = CanManage ? _filter.SelectedIndex : 1;

        var filtered = _allCategories.Where(c =>
            (status == 0 ||
             (status == 1 && c.IsActive) ||
             (status == 2 && !c.IsActive)) &&
            (string.IsNullOrEmpty(query) ||
             c.Name.Contains(
                 query, StringComparison.OrdinalIgnoreCase) ||
             (c.Description?.Contains(
                 query, StringComparison.OrdinalIgnoreCase)
                 ?? false)))
            .ToList();

        _grid.DataSource = filtered;
        _grid.ClearRowSelection();

        _status.Text = filtered.Count == 0
            ? "No matching categories."
            : $"{filtered.Count} of {_allCategories.Count} categories shown.";
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

        try
        {
            using var dialog = new CategoryDialog(
                _categories, existing);

            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                await LoadCategoriesAsync();
        }
        finally
        {
            _working = false;

            if (!IsDisposed && !Disposing)
                _addButton.Enabled =
                    _permissions.Can(Permission.CreateLookups);
        }
    }

    private async Task ChangeStatusAsync(Category category)
    {
        if (_working || _loadingData || !CanManage)
            return;

        bool activate = !category.IsActive;

        string verb = activate ? "reactivate" : "deactivate";

        var confirmation = MessageBox.Show(
            FindForm(),
            $"Do you want to {verb} '{category.Name}'?",
            "Confirm Category Status",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirmation != DialogResult.Yes)
            return;

        _working = true;
        _grid.Enabled = false;
        _addButton.Enabled = false;

        try
        {
            var result = await _categories.SetActiveAsync(
                category.CategoryId, activate);

            if (result.Success)
            {
                await LoadCategoriesAsync();
            }
            else
            {
                MessageBox.Show(
                    FindForm(),
                    result.Error ?? "Unable to change category status.",
                    "Category Status",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                FindForm(),
                ex.Message,
                "Category Status Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _working = false;

            if (!IsDisposed && !Disposing)
            {
                _grid.Enabled = true;
                _addButton.Enabled =
                    _permissions.Can(Permission.CreateLookups);
            }
        }
    }

    private async void HandleCellClick(
        object? sender,
        DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || !CanManage ||
            _working || _loadingData)
            return;

        if (_grid.Columns[e.ColumnIndex].Name != "Actions")
            return;

        if (_grid.Rows[e.RowIndex].DataBoundItem
            is not Category category)
            return;

        var rect = _grid.GetCellDisplayRectangle(
            e.ColumnIndex, e.RowIndex, false);

        Point mouse = _grid.PointToClient(Cursor.Position);
        int relativeX = mouse.X - rect.Left;

        if (relativeX < rect.Width / 2)
            await OpenDialogAsync(category);
        else
            await ChangeStatusAsync(category);
    }

    private void HandleCellTooltip(
        object? sender,
        DataGridViewCellToolTipTextNeededEventArgs e)
    {
        if (e.RowIndex < 0 ||
            _grid.Columns[e.ColumnIndex].Name != "Actions")
            return;

        if (_grid.Rows[e.RowIndex].DataBoundItem
            is Category category)
        {
            e.ToolTipText = category.IsActive
                ? "Pencil: Edit | Archive: Deactivate"
                : "Pencil: Edit | Restore: Reactivate";
        }
    }

    private void PaintSpecialCells(
        object? sender,
        DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        string column = _grid.Columns[e.ColumnIndex].Name;

        if (column != "IsActive" && column != "Actions")
            return;

        if (_grid.Rows[e.RowIndex].DataBoundItem
            is not Category category)
            return;

        e.PaintBackground(e.CellBounds, false);

        var g = e.Graphics;
        var c = Theme.Colors;

        if (g is null)
            return;

        var oldSmoothing = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int cy = e.CellBounds.Top + e.CellBounds.Height / 2;

        if (column == "IsActive")
        {
            int cx = e.CellBounds.Left + e.CellBounds.Width / 2;

            Color color = category.IsActive
                ? c.PillGoodText
                : c.TextMuted;

            using var pen = new Pen(color, 2f);

            g.DrawEllipse(pen, cx - 9, cy - 9, 18, 18);

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
                g.DrawLine(pen, cx - 4, cy, cx + 4, cy);
            }
        }
        else
        {
            int quarter = e.CellBounds.Width / 4;

            int editX = e.CellBounds.Left + quarter;
            int statusX = e.CellBounds.Left + quarter * 3;

            using var editPen = new Pen(c.Accent, 2.2f);
            using var statusPen = new Pen(
                category.IsActive ? c.Danger : c.PillGoodText,
                2f);

            // Pencil.
            g.DrawLine(
                editPen,
                editX - 6, cy + 5,
                editX + 5, cy - 6);

            g.DrawLine(
                editPen,
                editX - 8, cy + 8,
                editX - 3, cy + 7);

            g.DrawLine(
                editPen,
                editX + 4, cy - 7,
                editX + 7, cy - 4);

            // Archive / restore.
            g.DrawRectangle(
                statusPen,
                statusX - 8, cy - 5, 16, 13);

            g.DrawLine(
                statusPen,
                statusX - 10, cy - 8,
                statusX + 10, cy - 8);

            if (category.IsActive)
            {
                g.DrawLine(
                    statusPen,
                    statusX, cy - 2,
                    statusX, cy + 4);

                g.DrawLines(statusPen, new[]
                {
                    new Point(statusX - 4, cy + 1),
                    new Point(statusX, cy + 5),
                    new Point(statusX + 4, cy + 1)
                });
            }
            else
            {
                g.DrawLine(
                    statusPen,
                    statusX, cy + 5,
                    statusX, cy - 2);

                g.DrawLines(statusPen, new[]
                {
                    new Point(statusX - 4, cy + 1),
                    new Point(statusX, cy - 3),
                    new Point(statusX + 4, cy + 1)
                });
            }
        }

        g.SmoothingMode = oldSmoothing;
        e.Handled = true;
    }

    private void DrawFilterItem(
        object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;

        var c = Theme.Colors;
        bool selected =
            (e.State & DrawItemState.Selected) != 0;

        using var brush = new SolidBrush(
            selected ? c.AccentSoft : c.Surface);

        e.Graphics.FillRectangle(brush, e.Bounds);

        TextRenderer.DrawText(
            e.Graphics,
            _filter.Items[e.Index]!.ToString()!,
            Theme.Body,
            new Rectangle(
                e.Bounds.Left + 8,
                e.Bounds.Top,
                e.Bounds.Width - 8,
                e.Bounds.Height),
            c.Text,
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.Left);
    }

    private void UpdateResponsiveLayout()
    {
        if (IsDisposed || Disposing)
            return;

        int available = Math.Max(
            100, ClientSize.Width - Padding.Horizontal);

        bool compact = available < 550;
        _compact = compact;

        _toolbar.SuspendLayout();

        try
        {
            _toolbar.Controls.Clear();
            _toolbar.ColumnStyles.Clear();

            _toolbar.ColumnCount = 3;

            _toolbar.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            _toolbar.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute, compact ? 78 : 125));

            _toolbar.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute, compact ? 44 : 160));

            _toolbar.Controls.Add(_search, 0, 0);
            _toolbar.Controls.Add(_filter, 1, 0);
            _toolbar.Controls.Add(_addButton, 2, 0);

            _filter.Visible = CanManage;

            _addButton.Text = compact ? "+" : "+ Add Category";
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
                _grid.Columns["Actions"]!.Width = 110;
            }
        }
        finally
        {
            _toolbar.ResumeLayout(true);
        }
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        UpdateResponsiveLayout();

        if (_loadStarted) return;

        _loadStarted = true;
        await LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        if (IsDisposed || Disposing || _loadingData)
            return;

        _loadingData = true;
        _status.Text = "Loading categories...";
        _grid.Enabled = false;
        _addButton.Enabled = false;

        try
        {
            var categories = await _loading.RunAsync(
                () => _categories.GetAllAsync(CanManage));

            if (IsDisposed || Disposing)
                return;

            _allCategories = categories.ToList();

            _loadingData = false;
            FilterCategories();
        }
        catch (Exception ex)
        {
            if (IsDisposed || Disposing)
                return;

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

        _heading.ForeColor = c.Text;
        _heading.Font = Theme.Title;

        _status.ForeColor = c.TextMuted;
        _status.Font = Theme.Caption;

        _filter.BackColor = c.Surface;
        _filter.ForeColor = c.Text;
        _filter.Font = Theme.Body;
        _filter.Invalidate();

        _addButton.BackColor = c.Accent;
        _addButton.ForeColor = c.OnAccent;
        _addButton.FlatAppearance.BorderSize = 0;

        _grid.ApplyTheme();

        foreach (string name in new[] { "IsActive", "Actions" })
        {
            var column = _grid.Columns[name];

            if (column == null) continue;

            column.HeaderCell.Style.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            column.HeaderCell.Style.Padding = Padding.Empty;

            column.DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            column.DefaultCellStyle.Padding = Padding.Empty;
        }

        _grid.ClearRowSelection();
        _grid.Invalidate();
    }
}

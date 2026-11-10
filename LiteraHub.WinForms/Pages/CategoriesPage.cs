
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
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 4, 8, 4)
    };

    private readonly Button _filter = new()
    {
        Text = "All  ▾",
        Dock = DockStyle.Fill,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 4, 8, 4)
    };

    private readonly ContextMenuStrip _filterMenu = new();

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

    private readonly ThemedDataGridView _grid = new();

    private readonly LoadingHost _loading = new()
    {
        Dock = DockStyle.Fill,
        SkeletonStyle = SkeletonStyle.Rows
    };

    private List<Category> _allCategories = new();
    private int _filterIndex;
    private bool _loadStarted;
    private bool _loadingData;
    private bool _working;
    private bool _compact;

    private bool CanManage =>
        _permissions.Can(Permission.ManageLookups);

    private bool CanDelete =>
        CanManage && _permissions.Can(Permission.HardDelete);

    private int ActionCount => CanDelete ? 3 : 2;

    private static Color SuccessColor =>
        Theme.Mode == ThemeMode.Dark
            ? Color.FromArgb(115, 222, 164)
            : Color.FromArgb(22, 130, 75);

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

        _filter.Visible = CanManage;
        _addButton.Visible =
            _permissions.Can(Permission.CreateLookups);

        _search.Placeholder = "Search categories...";
        _search.TextChanged += (_, _) => FilterCategories();

        _filter.Click += (_, _) =>
        {
            BuildFilterMenu();
            _filterMenu.Show(_filter, new Point(0, _filter.Height));
        };

        _addButton.Click += async (_, _) =>
            await OpenDialogAsync(null);

        _grid.CellPainting += PaintSpecialCells;
        _grid.CellMouseUp += HandleActionMouseUp;
        _grid.CellMouseMove += HandleActionMouseMove;
        _grid.CellToolTipTextNeeded += HandleTooltip;
        _grid.KeyDown += HandleGridKeyDown;

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
            var col = _grid.Columns[name]!;
            col.HeaderCell.Style.Alignment =
                DataGridViewContentAlignment.MiddleCenter;
            col.DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;
            col.HeaderCell.Style.Padding = Padding.Empty;
            col.DefaultCellStyle.Padding = Padding.Empty;
        }

        _grid.DataBindingComplete += (_, _) =>
            _grid.ClearRowSelection();
    }

    private void BuildFilterMenu()
    {
        _filterMenu.Items.Clear();

        string[] options = { "All", "Active", "Inactive" };

        for (int i = 0; i < options.Length; i++)
        {
            int index = i;

            var item = new ToolStripMenuItem(options[i])
            {
                Checked = _filterIndex == i
            };

            item.Click += (_, _) =>
            {
                _filterIndex = index;
                _filter.Text = options[index] + "  ▾";
                FilterCategories();
            };

            _filterMenu.Items.Add(item);
        }

        var c = Theme.Colors;
        _filterMenu.BackColor = c.Surface;
        _filterMenu.ForeColor = c.Text;
        _filterMenu.RenderMode = ToolStripRenderMode.Professional;
    }

    private void FilterCategories()
    {
        if (IsDisposed || Disposing || _loadingData)
            return;

        string query = _search.Text.Trim();
        int filter = CanManage ? _filterIndex : 1;

        var filtered = _allCategories.Where(c =>
            (filter == 0 ||
             (filter == 1 && c.IsActive) ||
             (filter == 2 && !c.IsActive)) &&
            (query.Length == 0 ||
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

        if (MessageBox.Show(
                FindForm(),
                $"Do you want to {verb} '{category.Name}'?",
                "Confirm Category Status",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        await ExecuteMutationAsync(
            () => _categories.SetActiveAsync(
                category.CategoryId, activate),
            "Category Status");
    }

    private async Task DeleteCategoryAsync(Category category)
    {
        if (_working || _loadingData || !CanDelete)
            return;

        if (MessageBox.Show(
                FindForm(),
                $"Permanently delete '{category.Name}'?\n\n" +
                "This cannot be undone. Categories used by books " +
                "cannot be deleted.",
                "Permanently Delete Category",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2)
            != DialogResult.Yes)
            return;

        await ExecuteMutationAsync(
            () => _categories.DeleteAsync(category.CategoryId),
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
                MessageBox.Show(
                    FindForm(),
                    result.Error ?? "Operation failed.",
                    title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                FilterCategories();
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
            {
                MessageBox.Show(
                    FindForm(),
                    ex.Message,
                    title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
            }
        }
    }

    private async void HandleActionMouseUp(
        object? sender,
        DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left ||
            e.RowIndex < 0 || e.ColumnIndex < 0 ||
            _working || _loadingData || !CanManage)
            return;

        if (_grid.Columns[e.ColumnIndex]?.Name != "Actions")
            return;

        if (_grid.Rows[e.RowIndex].DataBoundItem
            is not Category category)
            return;

        int slotWidth = _grid.Columns[e.ColumnIndex].Width
            / ActionCount;

        int slot = Math.Clamp(
            e.X / Math.Max(1, slotWidth),
            0,
            ActionCount - 1);

        await RunActionAsync(category, slot);
    }

    private async Task RunActionAsync(Category category, int slot)
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

    private void HandleActionMouseMove(
        object? sender,
        DataGridViewCellMouseEventArgs e)
    {
        _grid.Cursor =
            e.RowIndex >= 0 &&
            e.ColumnIndex >= 0 &&
            _grid.Columns[e.ColumnIndex]?.Name == "Actions" &&
            CanManage
                ? Cursors.Hand
                : Cursors.Default;
    }

    private void HandleTooltip(
        object? sender,
        DataGridViewCellToolTipTextNeededEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
            _grid.Columns[e.ColumnIndex]?.Name != "Actions")
            return;

        if (_grid.Rows[e.RowIndex].DataBoundItem
            is Category category)
        {
            e.ToolTipText = CanDelete
                ? $"Edit | {(category.IsActive ? "Deactivate" : "Reactivate")} | Delete"
                : $"Edit | {(category.IsActive ? "Deactivate" : "Reactivate")}";
        }
    }

    private void HandleGridKeyDown(
        object? sender, KeyEventArgs e)
    {
        // Actions remain mouse-driven in read-only display mode.
        // The keyboard shortcut opens the selected row's Edit dialog
        // if a row is available.
        if (e.KeyCode != Keys.F2 || !CanManage)
            return;

        var category = _grid.CurrentRow?.DataBoundItem as Category;

        if (category is not null)
        {
            e.Handled = true;
            _ = OpenDialogAsync(category);
        }
    }

    private void PaintSpecialCells(
        object? sender,
        DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        string? column = _grid.Columns[e.ColumnIndex]?.Name;

        if (column != "IsActive" && column != "Actions")
            return;

        if (_grid.Rows[e.RowIndex].DataBoundItem
            is not Category category)
            return;

        e.PaintBackground(e.CellBounds, false);

        var g = e.Graphics;
        if (g is null) return;

        var c = Theme.Colors;
        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int cy = e.CellBounds.Top + e.CellBounds.Height / 2;

        if (column == "IsActive")
        {
            int cx = e.CellBounds.Left + e.CellBounds.Width / 2;

            using var pen = new Pen(
                category.IsActive ? SuccessColor : c.TextMuted, 2f);

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
            int count = ActionCount;
            int width = e.CellBounds.Width;

            for (int slot = 0; slot < count; slot++)
            {
                int cx = e.CellBounds.Left +
                    (int)((slot + 0.5) * width / count);

                if (slot == 0)
                {
                    // Edit-in-square icon.
                    using var pen = new Pen(SuccessColor, 2f);

                    g.DrawLine(pen, cx - 8, cy - 7, cx + 1, cy - 7);
                    g.DrawLine(pen, cx - 8, cy - 7, cx - 8, cy + 8);
                    g.DrawLine(pen, cx - 8, cy + 8, cx + 7, cy + 8);
                    g.DrawLine(pen, cx + 7, cy + 8, cx + 7, cy + 1);

                    g.DrawLine(pen, cx - 2, cy + 3, cx + 8, cy - 7);
                    g.DrawLine(pen, cx - 3, cy + 5, cx - 2, cy + 3);
                }
                else if (slot == 1)
                {
                    using var pen = new Pen(
                        category.IsActive ? c.TextMuted : SuccessColor,
                        2f);

                    g.DrawRectangle(pen, cx - 8, cy - 4, 16, 12);
                    g.DrawLine(pen, cx - 10, cy - 8, cx + 10, cy - 8);

                    int direction = category.IsActive ? 1 : -1;

                    g.DrawLine(
                        pen, cx, cy - 2, cx, cy + 4 * direction);

                    g.DrawLine(
                        pen,
                        cx - 4, cy + direction,
                        cx, cy + 5 * direction);

                    g.DrawLine(
                        pen,
                        cx + 4, cy + direction,
                        cx, cy + 5 * direction);
                }
                else
                {
                    // Trash icon.
                    using var pen = new Pen(c.Danger, 2f);

                    g.DrawLine(pen, cx - 9, cy - 6, cx + 9, cy - 6);
                    g.DrawLine(pen, cx - 4, cy - 9, cx + 4, cy - 9);
                    g.DrawRectangle(pen, cx - 7, cy - 3, 14, 12);
                    g.DrawLine(pen, cx - 3, cy, cx - 3, cy + 6);
                    g.DrawLine(pen, cx + 3, cy, cx + 3, cy + 6);
                }
            }
        }

        g.SmoothingMode = previous;
        e.Handled = true;
    }

    private void UpdateResponsiveLayout()
    {
        if (IsDisposed || Disposing)
            return;

        int available = Math.Max(
            100, ClientSize.Width - Padding.Horizontal);

        bool compact = available < 550;

        if (_compact == compact && _toolbar.Controls.Count == 3)
            return;

        _compact = compact;
        _toolbar.SuspendLayout();

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
                    SizeType.Absolute, compact ? 78 : 125));

            _toolbar.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute, compact ? 44 : 160));

            _toolbar.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            _toolbar.Controls.Add(_search, 0, 0);
            _toolbar.Controls.Add(_filter, 1, 0);
            _toolbar.Controls.Add(_addButton, 2, 0);

            _filter.Visible = CanManage;

            _addButton.Text = compact ? "+" : "+ Add Category";
            _addButton.AccessibleName = "Add Category";

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

            ApplyAddButtonFont();
        }
        finally
        {
            _toolbar.ResumeLayout(true);
        }
    }

    private void ApplyAddButtonFont()
    {
        _addButton.Font = _compact
            ? new Font("Segoe UI", 18f)
            : Theme.BodyStrong;
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

        _heading.BackColor = c.Background;
        _heading.ForeColor = c.Text;
        _heading.Font = Theme.Title;

        _status.BackColor = c.Background;
        _status.ForeColor = c.TextMuted;
        _status.Font = Theme.Caption;

        _filter.BackColor = c.Surface;
        _filter.ForeColor = c.Text;
        _filter.Font = Theme.Body;
        _filter.FlatAppearance.BorderColor = c.Border;
        _filter.FlatAppearance.BorderSize = 1;

        _addButton.BackColor = c.Accent;
        _addButton.ForeColor = c.OnAccent;
        _addButton.FlatAppearance.BorderSize = 0;

        ApplyAddButtonFont();

        _grid.ApplyTheme();

        foreach (string name in new[] { "IsActive", "Actions" })
        {
            var column = _grid.Columns[name];

            if (column is null) continue;

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _filterMenu.Dispose();

        base.Dispose(disposing);
    }
}

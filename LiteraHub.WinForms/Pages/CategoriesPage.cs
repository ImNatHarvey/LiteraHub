
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
        ColumnCount = 2,
        RowCount = 1,
        Margin = Padding.Empty
    };

    private readonly Label _heading = new()
    {
        Text = "Book Categories",
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = Padding.Empty
    };

    private readonly ThemedSearchBox _search = new()
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 4, 12, 4)
    };

    private readonly Button _addButton = new()
    {
        Text = "+ Add Category",
        Dock = DockStyle.Fill,
        FlatStyle = FlatStyle.Flat,
        Margin = new Padding(0, 4, 0, 4)
    };

    private readonly Label _status = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true,
        Margin = Padding.Empty
    };

    private readonly ThemedDataGridView _grid = new();

    private readonly LoadingHost _loading = new()
    {
        Dock = DockStyle.Fill,
        SkeletonStyle = SkeletonStyle.Rows,
        Margin = Padding.Empty
    };

    private List<Category> _allCategories = new();

    private bool _loadStarted;
    private bool _compact;
    private bool _loadingData;
    private bool _adding;

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

        _addButton.Visible =
            _permissions.Can(Permission.CreateLookups);

        _search.TextChanged += (_, _) => FilterCategories();

        _addButton.Click += async (_, _) =>
            await OpenAddDialogAsync();

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

        _toolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _toolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 160));

        _toolbar.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _toolbar.Controls.Add(_search, 0, 0);
        _toolbar.Controls.Add(_addButton, 1, 0);

        _grid.Margin = Padding.Empty;

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
            MinimumWidth = 115,
            ReadOnly = true
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Description",
            HeaderText = "Description",
            DataPropertyName = nameof(Category.Description),
            FillWeight = 50,
            MinimumWidth = 150,
            ReadOnly = true
        });

        var activeColumn = new DataGridViewCheckBoxColumn
        {
            Name = "IsActive",
            HeaderText = "Active",
            DataPropertyName = nameof(Category.IsActive),
            FillWeight = 20,
            MinimumWidth = 85,
            ReadOnly = true,
            FlatStyle = FlatStyle.Flat
        };

        activeColumn.HeaderCell.Style.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        activeColumn.HeaderCell.Style.Padding =
            Padding.Empty;

        activeColumn.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        activeColumn.DefaultCellStyle.Padding =
            Padding.Empty;

        activeColumn.DefaultCellStyle.NullValue = false;

        _grid.Columns.Add(activeColumn);

        _grid.DataBindingComplete += (_, _) =>
            _grid.ClearRowSelection();

        _grid.ClearRowSelection();
    }

    private void FilterCategories()
    {
        if (IsDisposed || Disposing || _loadingData)
            return;

        string query = _search.Text.Trim();

        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allCategories.ToList()
            : _allCategories.Where(c =>
                c.Name.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase) ||
                (c.Description?.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

        _grid.DataSource = filtered;
        _grid.ClearRowSelection();

        if (_allCategories.Count == 0)
        {
            _status.Text = "No categories found.";
        }
        else if (filtered.Count == 0)
        {
            _status.Text = "No matching categories.";
        }
        else if (string.IsNullOrWhiteSpace(query))
        {
            _status.Text =
                $"{filtered.Count} " +
                (filtered.Count == 1
                    ? "category loaded."
                    : "categories loaded.");
        }
        else
        {
            _status.Text =
                $"{filtered.Count} of " +
                $"{_allCategories.Count} categories shown.";
        }
    }

    private async Task OpenAddDialogAsync()
    {
        if (_adding || _loadingData ||
            !_permissions.Can(Permission.CreateLookups))
            return;

        _adding = true;
        _addButton.Enabled = false;

        try
        {
            using var dialog = new CategoryDialog(_categories);

            var result = dialog.ShowDialog(FindForm());

            if (result == DialogResult.OK)
            {
                await LoadCategoriesAsync();
            }
        }
        finally
        {
            _adding = false;

            if (!IsDisposed && !Disposing)
                _addButton.Enabled = true;
        }
    }

    private void UpdateResponsiveLayout()
    {
        if (IsDisposed || Disposing)
            return;

        int available = Math.Max(
            100,
            ClientSize.Width - Padding.Horizontal);

        bool compact = available < 550;

        if (_compact == compact && _loadStarted)
            return;

        _compact = compact;

        _layout.SuspendLayout();
        _toolbar.SuspendLayout();

        try
        {
            _toolbar.Controls.Clear();
            _toolbar.ColumnStyles.Clear();
            _toolbar.RowStyles.Clear();

            _toolbar.ColumnCount = 2;
            _toolbar.RowCount = 1;

            _toolbar.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            _toolbar.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            if (compact)
            {
                _toolbar.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Absolute, 44));

                _search.Margin =
                    new Padding(0, 4, 8, 4);

                _addButton.Margin =
                    new Padding(0, 4, 0, 4);

                _addButton.Text = "+";
                _addButton.AccessibleName = "Add Category";

                _layout.RowStyles[1].Height = 56;

                _grid.AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.None;

                _grid.Columns["Name"]!.Width = 140;
                _grid.Columns["Description"]!.Width = 220;
                _grid.Columns["IsActive"]!.Width = 90;
            }
            else
            {
                _toolbar.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Absolute, 160));

                _search.Margin =
                    new Padding(0, 4, 12, 4);

                _addButton.Margin =
                    new Padding(0, 4, 0, 4);

                _addButton.Text = "+ Add Category";
                _addButton.AccessibleName = "Add Category";

                _layout.RowStyles[1].Height = 56;

                _grid.AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill;
            }

            _toolbar.Controls.Add(_search, 0, 0);
            _toolbar.Controls.Add(_addButton, 1, 0);

            ApplyAddButtonFont();
        }
        finally
        {
            _toolbar.ResumeLayout(true);
            _layout.ResumeLayout(true);
        }
    }

    private void ApplyAddButtonFont()
    {
        _addButton.Font = _compact
            ? new Font(
                "Segoe UI",
                18f,
                FontStyle.Regular,
                GraphicsUnit.Point)
            : Theme.BodyStrong;
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
        _status.Text = "Loading categories...";
        _grid.Enabled = false;
        _addButton.Enabled = false;

        try
        {
            bool includeInactive =
                _permissions.Can(Permission.ManageLookups);

            var categories = await _loading.RunAsync(
                () => _categories.GetAllAsync(includeInactive));

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

            _allCategories.Clear();
            _grid.DataSource = null;

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

        _search.BackColor = c.Surface;

        _addButton.BackColor = c.Accent;
        _addButton.ForeColor = c.OnAccent;
        _addButton.FlatAppearance.BorderSize = 0;

        ApplyAddButtonFont();

        _grid.ApplyTheme();

        var activeColumn = _grid.Columns["IsActive"];

        if (activeColumn is not null)
        {
            activeColumn.HeaderCell.Style.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            activeColumn.HeaderCell.Style.Padding =
                Padding.Empty;

            activeColumn.DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            activeColumn.DefaultCellStyle.Padding =
                Padding.Empty;
        }

        _grid.ClearRowSelection();
    }
}

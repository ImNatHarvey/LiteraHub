
using System;
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

    private readonly TextBox _search = new()
    {
        PlaceholderText = "Search categories...",
        AutoSize = false,
        Height = 38,
        Dock = DockStyle.Fill,
        Font = Theme.Body,
        TextAlign = HorizontalAlignment.Left,
        Margin = new Padding(0, 4, 12, 4)
    };

    private readonly Button _addButton = new()
    {
        Text = "+ Add Category",
        Height = 38,
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

    private bool _loadStarted;
    private bool _compact;

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

        // Search and CRUD are not implemented yet.
        _search.Enabled = false;
        _addButton.Enabled = false;

        _addButton.Visible =
            _permissions.Can(Permission.CreateLookups);

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
        _layout.Controls.Add(_grid, 0, 3);

        Controls.Add(_layout);
    }

    private void ConfigureGrid()
    {
        _grid.ReadOnlyDisplayMode = true;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;

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

            if (compact)
            {
                _toolbar.ColumnCount = 1;
                _toolbar.RowCount = 2;

                _toolbar.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 100));

                _toolbar.RowStyles.Add(
                    new RowStyle(SizeType.Absolute, 46));

                _toolbar.RowStyles.Add(
                    new RowStyle(SizeType.Absolute, 46));

                _search.Margin =
                    new Padding(0, 4, 0, 4);

                _addButton.Margin =
                    new Padding(0, 4, 0, 4);

                _toolbar.Controls.Add(_search, 0, 0);
                _toolbar.Controls.Add(_addButton, 0, 1);

                _layout.RowStyles[1].Height = 100;

                _grid.AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.None;

                _grid.Columns["Name"]!.Width = 140;
                _grid.Columns["Description"]!.Width = 220;
                _grid.Columns["IsActive"]!.Width = 90;
            }
            else
            {
                _toolbar.ColumnCount = 2;
                _toolbar.RowCount = 1;

                _toolbar.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 100));

                _toolbar.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Absolute, 160));

                _toolbar.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 100));

                _search.Margin =
                    new Padding(0, 4, 12, 4);

                _addButton.Margin =
                    new Padding(0, 4, 0, 4);

                _toolbar.Controls.Add(_search, 0, 0);
                _toolbar.Controls.Add(_addButton, 1, 0);

                _layout.RowStyles[1].Height = 56;

                _grid.AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill;
            }
        }
        finally
        {
            _toolbar.ResumeLayout(true);
            _layout.ResumeLayout(true);
        }
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
        if (IsDisposed || Disposing)
            return;

        _status.Text = "Loading categories...";
        _grid.Enabled = false;

        try
        {
            bool includeInactive =
                _permissions.Can(Permission.ManageLookups);

            var categories =
                await _categories.GetAllAsync(includeInactive);

            if (IsDisposed || Disposing)
                return;

            var rows = categories.ToList();

            _grid.DataSource = rows;
            _grid.ClearRowSelection();

            _status.Text = rows.Count == 0
                ? "No categories found."
                : $"{rows.Count} categor" +
                  (rows.Count == 1 ? "y" : "ies") +
                  " loaded.";
        }
        catch (Exception ex)
        {
            if (IsDisposed || Disposing)
                return;

            _grid.DataSource = null;

            _status.Text =
                "Unable to load categories: " + ex.Message;
        }
        finally
        {
            if (!IsDisposed && !Disposing)
            {
                _grid.Enabled = true;
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

        // Search field
        NativeTheme.Apply(_search);
        _search.Font = Theme.Body;

        // Add Category button
        _addButton.BackColor = c.Accent;
        _addButton.ForeColor = c.OnAccent;
        _addButton.Font = Theme.BodyStrong;
        _addButton.FlatAppearance.BorderSize = 0;

        // Categories table
        _grid.ApplyTheme();

        // Center Active header and checkboxes.
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


using System;
using System.Drawing;
using System.Windows.Forms;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

/// <summary>
/// Categories management screen.
/// Step 5A.4A: layout and table foundation.
/// Database loading will be connected in Step 5A.4B.
/// </summary>
public sealed class CategoriesPage : ThemedUserControl
{
    private readonly ICategoryService _categories;
    private readonly IPermissionService _permissions;

    private readonly TableLayoutPanel _layout = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 3
    };

    private readonly TableLayoutPanel _toolbar = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1
    };

    private readonly Label _heading = new()
    {
        Text = "Book Categories",
        AutoSize = true,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly TextBox _search = new()
    {
        PlaceholderText = "Search categories...",
        Width = 260
    };

    private readonly Button _addButton = new()
    {
        Text = "+ Add Category",
        Width = 145,
        Height = 38,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand
    };

    private readonly ThemedDataGridView _grid = new();

    public CategoriesPage(
        ICategoryService categories,
        IPermissionService permissions)
    {
        _categories = categories ??
            throw new ArgumentNullException(nameof(categories));

        _permissions = permissions ??
            throw new ArgumentNullException(nameof(permissions));

        Dock = DockStyle.Fill;
        Padding = new Padding(Space.XL);

        BuildLayout();
        ConfigureGrid();

        _addButton.Visible =
            _permissions.Can(Permission.CreateLookups);

        // Events and real database operations will be
        // connected in the next implementation steps.
        _search.Enabled = false;
        _addButton.Enabled = false;
    }

    private void BuildLayout()
    {
        _layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 58));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 60));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _toolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _toolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.AutoSize));

        _toolbar.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _search.Anchor = AnchorStyles.Left;
        _search.Margin = new Padding(0, 0, Space.M, 0);

        var searchHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = Padding.Empty
        };

        _search.Location = new Point(0, 12);
        searchHost.Controls.Add(_search);

        _addButton.Anchor = AnchorStyles.Right;
        _addButton.Margin = new Padding(
            Space.M, 0, 0, 0);

        _toolbar.Controls.Add(searchHost, 0, 0);
        _toolbar.Controls.Add(_addButton, 1, 0);

        _grid.Margin = Padding.Empty;

        _layout.Controls.Add(_heading, 0, 0);
        _layout.Controls.Add(_toolbar, 0, 1);
        _layout.Controls.Add(_grid, 0, 2);

        Controls.Add(_layout);
    }

    private void ConfigureGrid()
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Name",
            HeaderText = "Category Name",
            DataPropertyName = nameof(Category.Name),
            FillWeight = 30,
            MinimumWidth = 140
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Description",
            HeaderText = "Description",
            DataPropertyName = nameof(Category.Description),
            FillWeight = 50,
            MinimumWidth = 180
        });

        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "IsActive",
            HeaderText = "Active",
            DataPropertyName = nameof(Category.IsActive),
            FillWeight = 20,
            MinimumWidth = 85,
            ReadOnly = true
        });

        _grid.ClearRowSelection();
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

        NativeTheme.Apply(_search);

        _addButton.BackColor = c.Accent;
        _addButton.ForeColor = c.OnAccent;
        _addButton.Font = Theme.BodyStrong;
        _addButton.FlatAppearance.BorderSize = 0;
        _addButton.FlatAppearance.MouseOverBackColor =
            c.Accent;
        _addButton.FlatAppearance.MouseDownBackColor =
            c.AccentSoft;

        _grid.ApplyTheme();
    }
}

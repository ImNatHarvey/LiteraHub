using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>
/// Searchable multi-category picker. Reuses the same checkbox selector,
/// themed popup and scrolling behavior as ThemedAuthorPicker.
/// Category IDs remain selected across search filters.
/// </summary>
public sealed class ThemedCategoryPicker : Button
{
    private readonly List<Category> _categories = new();
    private readonly List<int> _selectedIds = new();
    private Form? _popup;

    public event EventHandler? SelectionChanged;
    public event EventHandler? AddCategoryRequested;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<int> SelectedCategoryIds => _selectedIds.ToArray();

    // The first selected category is the primary category for the legacy
    // Books.CategoryId column (also enforced by BookService).
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedCategoryId => _selectedIds.Count == 0 ? 0 : _selectedIds[0];

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HasCategories => _categories.Count > 0;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool AllowCreateCategory { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string RequestedCategoryName { get; private set; } = string.Empty;

    public ThemedCategoryPicker()
    {
        FlatStyle = FlatStyle.Flat;
        TextAlign = ContentAlignment.MiddleLeft;
        Padding = new Padding(12, 0, 8, 0);
        Cursor = Cursors.Hand;
        TabStop = true;
        AutoEllipsis = true;

        Click += (_, _) => OpenPicker();
        KeyPress += (_, e) =>
        {
            if (char.IsControl(e.KeyChar)) return;
            OpenPicker(e.KeyChar.ToString());
            e.Handled = true;
        };
        Theme.Changed += HandleThemeChanged;
        ApplyTheme();
        UpdateCaption();
    }

    public void SetCategories(
        IEnumerable<Category> categories,
        IEnumerable<int>? selectedIds = null)
    {
        ArgumentNullException.ThrowIfNull(categories);
        var requested = (selectedIds ?? _selectedIds).Distinct().ToArray();
        _categories.Clear();
        _categories.AddRange(categories
            .Where(c => c.IsActive)
            .GroupBy(c => c.CategoryId)
            .Select(group => group.First())
            .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase));

        _selectedIds.Clear();
        foreach (int id in requested)
            if (_categories.Any(c => c.CategoryId == id))
                _selectedIds.Add(id);

        _popup?.Close();
        UpdateCaption();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    // Retain compatibility with the original single-select picker API.
    public void SetCategories(IEnumerable<Category> categories, int selectedId) =>
        SetCategories(categories, selectedId > 0 ? new[] { selectedId } : Array.Empty<int>());

    public void SelectCategories(IEnumerable<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        _selectedIds.Clear();
        foreach (int id in ids.Distinct())
            if (_categories.Any(c => c.CategoryId == id))
                _selectedIds.Add(id);
        UpdateCaption();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SelectCategory(int id)
    {
        if (id <= 0 || !_categories.Any(c => c.CategoryId == id) || _selectedIds.Contains(id))
            return;
        _selectedIds.Add(id);
        UpdateCaption();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateCaption()
    {
        if (_selectedIds.Count == 0)
            Text = _categories.Count == 0
                ? "No categories loaded  ▾" : "Select category(s)  ▾";
        else
        {
            var names = _selectedIds.Select(id =>
                _categories.First(c => c.CategoryId == id).Name).ToArray();
            Text = names.Length <= 2
                ? string.Join(", ", names) + "  ▾"
                : $"{names.Length} categories selected  ▾";
        }
        AccessibleName = "Categories: " + Text;
    }

    private void OpenPicker(string initialSearch = "")
    {
        if (_popup is not null || !Enabled ||
            (_categories.Count == 0 && !AllowCreateCategory)) return;

        var colors = Theme.Colors;
        var popup = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            BackColor = colors.Border,
            Padding = new Padding(1),
            Size = new Size(Math.Clamp(Width, 270, 480), 250)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = AllowCreateCategory ? 3 : 2,
            BackColor = colors.Surface,
            Padding = new Padding(8),
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        if (AllowCreateCategory)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        var search = new ThemedTextField
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8)
        };
        layout.Controls.Add(search, 0, 0);

        // This is the identical checkbox-list control used by the Author picker.
        var selector = new ThemedAuthorSelector { Margin = Padding.Empty };
        var simpleHost = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = colors.Surface,
            AutoScroll = false
        };
        simpleHost.Controls.Add(selector);
        selector.Dock = DockStyle.Fill;

        var scroll = new ScrollHost
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        scroll.UseSurfaceBackground();

        BookLookupOption[] Filtered() => _categories
            .Where(c => c.Name.Contains(search.Text.Trim(),
                StringComparison.OrdinalIgnoreCase))
            .Select(c => new BookLookupOption
            {
                Id = c.CategoryId,
                Name = c.Name,
                IsActive = true
            }).ToArray();

        void RefreshRows()
        {
            var filtered = Filtered();
            selector.SetAuthors(filtered, _selectedIds);
            int contentHeight = Math.Max(48, filtered.Length * 44);
            int footerHeight = AllowCreateCategory ? 46 : 0;
            popup.Height = Math.Clamp(
                108 + contentHeight + footerHeight,
                154 + footerHeight, 356);

            bool needsScroll = filtered.Length > 4;
            scroll.Visible = needsScroll;
            simpleHost.Visible = !needsScroll;
            if (needsScroll)
            {
                if (!ReferenceEquals(selector.Parent, scroll))
                {
                    simpleHost.Controls.Remove(selector);
                    selector.Dock = DockStyle.None;
                    scroll.SetContent(selector);
                }
                selector.Height = contentHeight;
                scroll.RefreshMetrics();
            }
            else
            {
                if (!ReferenceEquals(selector.Parent, simpleHost))
                {
                    scroll.Controls.Remove(selector);
                    simpleHost.Controls.Add(selector);
                }
                selector.Dock = DockStyle.Fill;
            }
        }

        selector.SelectionChanged += (_, _) =>
        {
            var visibleIds = Filtered().Select(x => x.Id).ToHashSet();
            var nowSelected = selector.SelectedAuthorIds.ToHashSet();
            // Preserve selection order; unchecking a secondary category must
            // never silently change the primary Books.CategoryId.
            _selectedIds.RemoveAll(id => visibleIds.Contains(id) && !nowSelected.Contains(id));
            foreach (int id in nowSelected)
                if (!_selectedIds.Contains(id))
                    _selectedIds.Add(id);
            UpdateCaption();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        };
        search.TextChanged += (_, _) => RefreshRows();

        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = colors.Surface
        };
        host.Controls.Add(simpleHost);
        host.Controls.Add(scroll);
        scroll.Visible = false;
        layout.Controls.Add(host, 0, 1);

        if (AllowCreateCategory)
        {
            var add = new Button
            {
                Text = "+ Add Category",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = colors.Accent,
                ForeColor = colors.OnAccent,
                Font = Theme.BodyStrong,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            add.FlatAppearance.BorderSize = 0;
            add.FlatAppearance.MouseOverBackColor = colors.Accent;
            add.FlatAppearance.MouseDownBackColor = colors.Accent;
            add.Click += (_, _) =>
            {
                RequestedCategoryName = search.Text.Trim();
                popup.Close();
                AddCategoryRequested?.Invoke(this, EventArgs.Empty);
            };
            layout.Controls.Add(add, 0, 2);
        }

        popup.Controls.Add(layout);
        _popup = popup;
        popup.Deactivate += (_, _) => popup.Close();
        popup.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_popup, popup))
                _popup = null;
            popup.Dispose();
        };

        if (initialSearch.Length > 0)
            search.Text = initialSearch;
        RefreshRows();

        var origin = PointToScreen(new Point(0, Height + 4));
        var bounds = Screen.FromControl(this).WorkingArea;
        popup.Location = new Point(
            Math.Clamp(origin.X, bounds.Left,
                Math.Max(bounds.Left, bounds.Right - popup.Width)),
            origin.Y + popup.Height <= bounds.Bottom
                ? origin.Y
                : Math.Max(bounds.Top,
                    PointToScreen(Point.Empty).Y - popup.Height - 4));
        if (FindForm() is Form owner)
            popup.Show(owner);
        else
            popup.Show();
        search.FocusInput();
    }

    private void HandleThemeChanged(object? sender, EventArgs e) => ApplyTheme();

    public void ApplyTheme()
    {
        if (IsDisposed) return;
        var colors = Theme.Colors;
        BackColor = colors.Surface;
        ForeColor = colors.Text;
        Font = Theme.Body;
        FlatAppearance.BorderSize = 1;
        FlatAppearance.BorderColor = colors.Border;
        FlatAppearance.MouseOverBackColor = colors.AccentSoft;
        FlatAppearance.MouseDownBackColor = colors.AccentSoft;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= HandleThemeChanged;
            _popup?.Close();
        }
        base.Dispose(disposing);
    }
}

using FontAwesome.Sharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
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

/// <summary>In-shell Category Add/View/Edit, with a fixed information panel and scrollable books.</summary>
public sealed class CategoryDetailsPage : ThemedUserControl
{
    private readonly ICategoryService _categories;
    private readonly IBookService _books;
    private readonly IPermissionService _permissions;
    private Category? _category;
    private bool _editing;
    private bool _busy;
    private bool _loaded;
    private bool _loadingCovers;

    private readonly TableLayoutPanel _layout = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly DetailsPageHeader _header = new() { AlignBackToRightEdge = true };
    private readonly ThemedSectionCard _infoPanel = new(new Label { Text = "Category Information", UseMnemonic = false, TextAlign = ContentAlignment.MiddleLeft }, IconChar.Tags);
    private readonly TableLayoutPanel _info = new() { ColumnCount = 1, RowCount = 3, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly ResponsiveDetailsPair _pair = new();
    private readonly Dictionary<string, (Label Caption, Label Value, Control Edit)> _fields = new();
    private readonly ThemedTextField _name = new() { MaxLength = 100 };
    private readonly ThemedTextField _description = new() { MaxLength = 500, Multiline = true };
    private readonly CatalogValidationController _validation = new();
    private bool _validationAttempted;
    private readonly Label _id = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    // Shared gallery. Preserve the accepted Category card dimensions when
    // its one-row information panel leaves more vertical gallery space.
    private readonly AssociatedBooksGallery _associatedBooks = new()
    {
        Dock = DockStyle.Fill,
        MaximumCardWidth = 183,
        MaximumCardHeight = 241
    };
    private readonly DetailsActionFooter _actions = new();
    private readonly Button _primary = new() { FlatStyle = FlatStyle.Flat, Width = 210, Height = 40, Cursor = Cursors.Hand };
    private readonly Button _cancel = new() { Text = "Cancel", FlatStyle = FlatStyle.Flat, Width = 115, Height = 40, Cursor = Cursors.Hand };
    private readonly Button _archive = new() { FlatStyle = FlatStyle.Flat, Width = 210, Height = 40, Cursor = Cursors.Hand };
    private readonly Button _delete = new() { FlatStyle = FlatStyle.Flat, Width = 210, Height = 40, Cursor = Cursors.Hand };

    public event EventHandler? BackRequested;
    public event EventHandler<int>? BookRequested;
    public event EventHandler? Saved;

    public CategoryDetailsPage(ICategoryService categories, IBookService books,
        IPermissionService permissions, Category? category)
    {
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        _books = books ?? throw new ArgumentNullException(nameof(books));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _category = category;
        _editing = category is null;
        Dock = DockStyle.Fill;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Space.L));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        _layout.Controls.Add(_header, 0, 0);
        _layout.Controls.Add(_pair, 0, 1);
        _layout.SetColumnSpan(_pair, 2);
        _info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 3; i++)
            _info.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 2 ? 190 : 72));
        _infoPanel.AddFields(_info);
        _infoPanel.Resize += (_, _) => ArrangeInformation();
        _pair.LayoutUpdated += (_, _) => ArrangeInformation();
        AddField("ID", "Category ID", _id, 0, 0);
        AddField("Name", "Category Name", _name, 1, 0);
        AddField("Description", "Description", _description, 2, 0);

        _associatedBooks.BookRequested += (_, bookId) =>
            BookRequested?.Invoke(this, bookId);
        _pair.Configure(_infoPanel, _associatedBooks, 400, 310, true, Space.L);
        ArrangeInformation();
        _actions.Controls.AddRange(new Control[] { _delete, _archive, _primary, _cancel });
        _actions.Configure(_delete, _archive, _primary, _cancel, () => _category is null);
        _primary.Paint += (_, e) => PaintActionIcon(e, _primary, 0);
        _archive.Paint += (_, e) => PaintActionIcon(e, _archive, 1);
        _delete.Paint += (_, e) => PaintActionIcon(e, _delete, 2);
        _layout.Controls.Add(_actions, 0, 2);
        Controls.Add(_layout);
        _header.BackRequested += (_, _) => { if (!_busy) BackRequested?.Invoke(this, EventArgs.Empty); };
        _cancel.Click += (_, _) => { if (_busy) return; _editing = false; _validation.Clear(); _validationAttempted = false; Populate(); UpdateMode(); RefreshValidationCaption(); };
        _primary.Click += async (_, _) =>
        {
            if (_busy) return;
            if (!_editing) { _editing = true; UpdateMode(); return; }
            await SaveAsync();
        };
        _archive.Click += async (_, _) => await ChangeActiveAsync();
        _delete.Click += async (_, _) => await DeleteAsync();
        _validation.Register(_name, () => !string.IsNullOrWhiteSpace(_name.Text),
            "Category name is required.");
        _name.TextChanged += (_, _) => RefreshValidationCaption();
        Populate();
        UpdateMode();
        PositionFooterActions();
    }

    // Fixed-height input fields; the Category description can use its multiline row.
    private void AddField(string key, string caption, Control editor, int col, int row)
    {
        var cell = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        var label = new Label
        {
            Text = caption,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false
        };
        var value = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = key == "Description" ? ContentAlignment.TopLeft : ContentAlignment.MiddleLeft,
            Padding = key == "Description" ? new Padding(12, 8, 8, 8) : new Padding(12, 0, 0, 0)
        };
        var area = new Panel { Margin = Padding.Empty, Padding = Padding.Empty };
        editor.Dock = DockStyle.Fill;
        area.Controls.Add(value);
        area.Controls.Add(editor);
        cell.Controls.Add(label);
        cell.Controls.Add(area);

        void PositionField()
        {
            label.SetBounds(0, 0, Math.Max(1, cell.ClientSize.Width), 19);
            area.SetBounds(0, 23, Math.Max(1, cell.ClientSize.Width), key == "Description" ? Math.Max(40, cell.ClientSize.Height - 28) : 40);
        }
        cell.Resize += (_, _) => PositionField();
        _info.Controls.Add(cell, 0, col);
        _fields[key] = (label, value, editor);
        PositionField();
    }

    private void ArrangeInformation()
    {
        if (_infoPanel.Width <= 0) return;
        int pad = _pair.ContentPadding;
        int top = _infoPanel.ArrangeHeading(_infoPanel.Width, pad);
        _info.SetBounds(pad, top, Math.Max(1, _infoPanel.Width - 2 * pad), 334);
    }

    private void Populate()
    {
        _id.Text = _category?.CategoryId.ToString(CultureInfo.InvariantCulture) ?? "Generated on Save";
        _name.Text = _category?.Name ?? "";
        _description.Text = _category?.Description ?? "";
    }

    private void UpdateMode()
    {
        bool existing = _category is not null;
        _header.HeadingText = !existing ? "Add Category" : _editing ? "Edit Category" : "View Category";
        _header.SubtitleText = !existing ? "Create a new category record." :
            (_editing ? "Editing: " : "Viewing: ") + _category!.Name;
        _primary.Visible = _permissions.Can(_category is null ? RolePermission.CategoriesAdd : RolePermission.CategoriesEdit);
        _primary.Enabled = !_busy;
        _cancel.Visible = existing && _editing;
        _archive.Visible = existing && _permissions.Can(RolePermission.CategoriesArchiveRestore);
        _archive.Text = "";
        _archive.AccessibleName = _category?.IsActive == false ? "Restore Category" : "Archive Category";
        _delete.Visible = existing && _permissions.Can(RolePermission.CategoriesDelete);
        _delete.Text = "";
        _delete.AccessibleName = "Delete Category";
        _primary.Text = "";
        _primary.AccessibleName = _editing ? "Save Category" : "Edit Category";
        if (!existing) _associatedBooks.ShowBeforeSave();
        foreach (var (key, pair) in _fields)
        {
            string text = key switch
            {
                "ID" => _id.Text,
                "Name" => _category?.Name ?? "—",
                "Description" => string.IsNullOrWhiteSpace(_category?.Description) ? "—" : _category!.Description!,
                _ => "—"
            };
            pair.Value.Text = text;
            pair.Edit.Visible = _editing && key != "ID";
            pair.Value.Visible = !_editing || key == "ID";
            if (pair.Value.Visible) pair.Value.BringToFront();
        }
        ApplyTheme();
        PositionFooterActions();
        _primary.Invalidate();
        _archive.Invalidate();
        _delete.Invalidate();
    }

    private void RefreshValidationCaption()
    {
        if (!_validationAttempted || IsDisposed || Disposing) return;
        if (!_fields.TryGetValue("Name", out var field)) return;
        bool invalid = _name.HasValidationError;
        field.Caption.Text = invalid ? "Category Name • Required" : "Category Name";
        field.Caption.ForeColor = invalid ? Theme.Colors.Danger : Theme.Colors.Text;
        field.Caption.AutoEllipsis = true;
    }

    private async Task SaveAsync()
    {
        if (_busy || !_editing) return;
        var name = _name.Text.Trim();
        var description = _description.Text.Trim();
        _validationAttempted = true;
        if (_validation.Validate().Count > 0)
        {
            RefreshValidationCaption();
            _validation.ShowSummary(FindForm(), "this category");
            if (!IsDisposed && !Disposing) _validation.FocusFirstInvalid();
            return;
        }
        var input = new CategoryInput(name, description.Length == 0 ? null : description);
        bool creating = _category is null;
        _busy = true;
        try
        {
            if (_category is null)
            {
                if (!_permissions.Can(RolePermission.CategoriesAdd)) return;
                var created = await _categories.CreateAsync(input);
                if (!created.Success) { Alert(created.Error ?? "Unable to create category."); return; }
                var list = await _categories.GetAllAsync(true);
                _category = list.Where(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(c => c.CategoryId).FirstOrDefault();
                if (_category is null)
                {
                    CatalogActionFeedback.Notify(this, "Category added successfully.");
                    Saved?.Invoke(this, EventArgs.Empty);
                    BackRequested?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }
            else
            {
                var result = await _categories.UpdateAsync(_category.CategoryId, input);
                if (!result.Success) { Alert(result.Error ?? "Unable to update category."); return; }
                _category = await _categories.GetByIdAsync(_category.CategoryId);
            }
            if (IsDisposed || Disposing) return;
            _editing = false;
            _validation.Clear();
            _validationAttempted = false;
            Populate(); UpdateMode();
            CatalogActionFeedback.Notify(this,
                creating ? "Category added successfully." : "Category updated successfully.");
            Saved?.Invoke(this, EventArgs.Empty);
            await LoadBooksAsync();
        }
        catch (Exception ex) { if (!IsDisposed && !Disposing) Alert(ex.Message); }
        finally { _busy = false; if (!IsDisposed && !Disposing) UpdateMode(); }
    }

    private async Task ChangeActiveAsync()
    {
        if (_busy || _category is null || !_permissions.Can(RolePermission.CategoriesArchiveRestore)) return;
        bool restore = !_category.IsActive;
        if (!ThemedMessageDialog.Confirm(FindForm(), restore ? "Restore Category" : "Archive Category",
            $"{(restore ? "Restore" : "Archive")} '{_category.Name}'?", danger: !restore,
            confirmText: restore ? "Restore" : "Archive")) return;
        _busy = true;
        try
        {
            var result = await _categories.SetActiveAsync(_category.CategoryId, restore);
            if (!result.Success) { Alert(result.Error ?? "Unable to change category status."); return; }
            _category = await _categories.GetByIdAsync(_category.CategoryId);
            if (IsDisposed || Disposing) return;
            Populate(); UpdateMode();
            CatalogActionFeedback.Notify(this,
                restore ? "Category restored successfully." : "Category archived successfully.");
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { if (!IsDisposed && !Disposing) Alert(ex.Message); }
        finally { _busy = false; if (!IsDisposed && !Disposing) UpdateMode(); }
    }

    private async Task DeleteAsync()
    {
        if (_busy || _category is null || !_permissions.Can(RolePermission.CategoriesDelete)) return;
        if (!ThemedMessageDialog.Confirm(FindForm(), "Delete Category",
            $"Permanently delete '{_category.Name}'? Categories linked to books cannot be deleted.",
            danger: true, confirmText: "Delete")) return;
        _busy = true;
        try
        {
            var result = await _categories.DeleteAsync(_category.CategoryId);
            if (!result.Success) { Alert(result.Error ?? "Deletion blocked."); return; }
            CatalogActionFeedback.Notify(this, "Category deleted successfully.");
            Saved?.Invoke(this, EventArgs.Empty);
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { if (!IsDisposed && !Disposing) Alert(ex.Message); }
        finally { _busy = false; }
    }

    private void Alert(string message) =>
        ThemedMessageDialog.ShowAlert(FindForm(), "Category Management", message, danger: true);

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (_loaded || _category is null) return;
        _loaded = true;
        await LoadBooksAsync();
    }

    public Task RefreshBooksAsync() => LoadBooksAsync();

    private async Task LoadBooksAsync()
    {
        if (_category is null || _loadingCovers) return;
        if (!_permissions.Can(RolePermission.BooksView))
        {
            _associatedBooks.Visible = false;
            return;
        }
        _loadingCovers = true;
        try
        {
            int categoryId = _category.CategoryId;
            var all = await _books.GetAllAsync(true);
            if (IsDisposed || Disposing || _category?.CategoryId != categoryId) return;
            var related = all.Where(b => b.CategoryIds.Contains(categoryId) || b.CategoryId == categoryId)
                .OrderBy(b => b.Title, StringComparer.OrdinalIgnoreCase).ToArray();
            _associatedBooks.SetBooks(related, "No books assigned to this category.");
        }
        catch (Exception)
        {
            if (!IsDisposed && !Disposing)
                _associatedBooks.ShowError("Unable to load associated books. Try reopening this category.");
        }
        finally { _loadingCovers = false; }
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        _layout.BackColor = c.Background;
        _header.ApplyTheme();
        _infoPanel.BackColor = c.Surface; _info.BackColor = c.Surface;
        _actions.BackColor = c.Background;
        foreach (var (_, pair) in _fields)
        {
            pair.Caption.ForeColor = c.Text; pair.Caption.BackColor = c.Surface; pair.Caption.Font = Theme.Body;
            pair.Value.BackColor = c.SurfaceAlt; pair.Value.ForeColor = c.Text;
            pair.Value.Font = Theme.BodyStrong;
            pair.Value.Paint -= DrawFieldBorder;
            pair.Value.Paint += DrawFieldBorder;
        }
        _cancel.BackColor = c.SurfaceAlt;
        _cancel.ForeColor = c.Text;
        _cancel.Font = Theme.Body;
        _cancel.FlatAppearance.BorderSize = 1;
        _cancel.FlatAppearance.BorderColor = c.Border;
        _cancel.FlatAppearance.MouseOverBackColor = c.AccentSoft;
        _cancel.FlatAppearance.MouseDownBackColor = c.AccentSoft;

        _archive.BackColor = c.Surface;
        _archive.ForeColor = c.Text;
        _archive.Font = Theme.BodyStrong;
        _archive.FlatAppearance.BorderSize = 1;
        _archive.FlatAppearance.BorderColor = c.Border;

        var deleteRed = Color.FromArgb(255, 143, 137);
        _delete.BackColor = deleteRed;
        _delete.ForeColor = Color.White;
        _delete.Font = Theme.BodyStrong;
        _delete.FlatAppearance.BorderSize = 0;
        _delete.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(deleteRed, 0.12f);
        _delete.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(deleteRed, 0.23f);

        _primary.BackColor = Color.FromArgb(34, 139, 86);
        _primary.ForeColor = Color.White;
        _primary.Font = Theme.BodyStrong;
        _primary.FlatAppearance.BorderSize = 0;
        PositionFooterActions();
    }

    private static void DrawFieldBorder(object? sender, PaintEventArgs e)
    {
        if (sender is not Label l) return;
        using var pen = new Pen(Theme.Colors.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, l.Width - 1), Math.Max(0, l.Height - 1));
    }

    private static void StyleButton(Button button, Color back, Color fore, Color border)
    {
        button.BackColor = back; button.ForeColor = fore; button.Font = Theme.BodyStrong;
        button.FlatAppearance.BorderSize = 1; button.FlatAppearance.BorderColor = border;
        button.FlatAppearance.MouseOverBackColor = Theme.Colors.AccentSoft;
    }


    // Shared footer owns geometry; page retains all behavior and styling.
    private void PositionFooterActions() => _actions.ArrangeActions();

    // The same icon strokes, placement, foreground selection, and labels
    // as BookEditorPage.PaintActionIcon (Category wording substituted).
    private void PaintActionIcon(PaintEventArgs e, Button button, int kind)
    {
        string label = kind == 0 ? (_editing ? "Save Category" : "Edit Category") : kind == 2 ? "Delete Category" :
            _category?.IsActive == false ? "Restore Category" : "Archive Category";
        IconButtonContent.DrawAction(e, button, kind, label, _category?.IsActive == false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _validation.Dispose();
        base.Dispose(disposing);
    }
}

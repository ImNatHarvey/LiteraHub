using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
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

    private Task LoadCoverAsync(PictureBox target, string? path)
    {
        return CatalogCoverCache.LoadIntoAsync(target, path);
    }

    private readonly ICategoryService _categories;
    private readonly IBookService _books;
    private readonly IPermissionService _permissions;
    private Category? _category;
    private bool _editing;
    private bool _busy;
    private bool _loaded;
    private bool _loadingCovers;

    private readonly TableLayoutPanel _layout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(0, 0, 0, 16), Margin = Padding.Empty };
    private readonly TableLayoutPanel _header = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
    private readonly Label _heading = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly BackButton _back = new()
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(8, 4, 2, 4)
    };
    private readonly Label _subtitle = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Panel _infoPanel = new()
    {
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
        Padding = new Padding(16)
    };
    private readonly TableLayoutPanel _info = new() { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly Dictionary<string, (Label Caption, Label Value, Control Edit)> _fields = new();
    private readonly ThemedTextField _name = new() { MaxLength = 100 };
    private readonly ThemedTextField _description = new() { MaxLength = 500, Multiline = true };
    private readonly Label _id = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _galleryHeading = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly FlowLayoutPanel _gallery = new()
    {
        AutoScroll = false,
        WrapContents = true,
        FlowDirection = FlowDirection.LeftToRight,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };
    private readonly ScrollHost _galleryScroll = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private bool _galleryArranging;
    private readonly Panel _actions = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly Button _primary = new() { FlatStyle = FlatStyle.Flat, Width = 210, Height = 40, Cursor = Cursors.Hand };
    private readonly Button _cancel = new() { Text = "Cancel", FlatStyle = FlatStyle.Flat, Width = 115, Height = 40, Cursor = Cursors.Hand };
    private readonly Button _archive = new() { FlatStyle = FlatStyle.Flat, Width = 210, Height = 40, Cursor = Cursors.Hand };
    private readonly Button _delete = new() { FlatStyle = FlatStyle.Flat, Width = 210, Height = 40, Cursor = Cursors.Hand };
    private readonly ToolTip _tips = new();

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
        foreach (var height in new[] { 56f, 42f, 104f, 0f, 52f })
            _layout.RowStyles.Add(height == 0 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, height));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        _header.Controls.Add(_heading, 0, 0);
        _header.Controls.Add(_back, 1, 0);
        _layout.Controls.Add(_header, 0, 0);
        _layout.Controls.Add(_subtitle, 0, 1);
        _layout.Controls.Add(_infoPanel, 0, 2);

        _info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        _info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        _info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
        // Each field gets a compact, fixed height instead of stretching vertically.
        _info.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        _infoPanel.Controls.Add(_info);
        AddField("ID", "Category ID", _id, 0, 0);
        AddField("Name", "Category Name", _name, 1, 0);
        AddField("Description", "Description", _description, 2, 0);

        var galleryContainer = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = Padding.Empty };
        galleryContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        galleryContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        galleryContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        galleryContainer.Controls.Add(_galleryHeading, 0, 0);
        _galleryScroll.SetContent(_gallery);
        galleryContainer.Controls.Add(_galleryScroll, 0, 1);
        _galleryScroll.Resize += (_, _) => ArrangeGallery();
        _gallery.MouseWheel += (_, e) => _galleryScroll.ScrollByPixels(e.Delta > 0 ? -72 : 72);
        _layout.Controls.Add(galleryContainer, 0, 3);
        _actions.Controls.AddRange(new Control[] { _delete, _archive, _primary, _cancel });
        _actions.Resize += (_, _) => PositionFooterActions();
        _primary.Paint += (_, e) => PaintActionIcon(e, _primary, 0);
        _archive.Paint += (_, e) => PaintActionIcon(e, _archive, 1);
        _delete.Paint += (_, e) => PaintActionIcon(e, _delete, 2);
        _layout.Controls.Add(_actions, 0, 4);
        Controls.Add(_layout);
        _back.Click += (_, _) => { if (!_busy) BackRequested?.Invoke(this, EventArgs.Empty); };
        _cancel.Click += (_, _) => { if (_busy) return; _editing = false; Populate(); UpdateMode(); };
        _primary.Click += async (_, _) =>
        {
            if (_busy) return;
            if (!_editing) { _editing = true; UpdateMode(); return; }
            await SaveAsync();
        };
        _archive.Click += async (_, _) => await ChangeActiveAsync();
        _delete.Click += async (_, _) => await DeleteAsync();
        Populate();
        UpdateMode();
        PositionFooterActions();
    }

    // Match Student Details: 19px caption, 8px spacing, 40px input,
    // and a 72px field row. A Panel prevents TableLayoutPanel from
    // stretching the visible input to consume unused vertical space.
    private void AddField(string key, string caption, Control editor, int col, int row)
    {
        var cell = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0),
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
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
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
            area.SetBounds(0, 27, Math.Max(1, cell.ClientSize.Width), 40);
        }
        cell.Resize += (_, _) => PositionField();
        _info.Controls.Add(cell, col, row);
        _fields[key] = (label, value, editor);
        PositionField();
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
        _heading.Text = !existing ? "Add Category" : _editing ? "Edit Category" : "View Category";
        _subtitle.Text = !existing ? "Create a new category record." :
            (_editing ? "Editing: " : "Viewing: ") + _category!.Name;
        _primary.Visible = (_editing && _permissions.Can(Permission.CreateLookups)) ||
            (existing && _permissions.Can(Permission.ManageLookups));
        _primary.Enabled = !_busy;
        _cancel.Visible = existing && _editing;
        _archive.Visible = existing && _permissions.Can(Permission.ManageLookups);
        _archive.Text = "";
        _archive.AccessibleName = _category?.IsActive == false ? "Restore Category" : "Archive Category";
        _delete.Visible = existing && _permissions.Can(Permission.HardDelete) && _permissions.Can(Permission.ManageLookups);
        _delete.Text = "";
        _delete.AccessibleName = "Delete Category";
        _primary.Text = "";
        _primary.AccessibleName = _editing ? "Save Category" : "Edit Category";
        _galleryHeading.Text = existing ? "Associated Books" : "Associated Books (available after saving)";
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

    private async Task SaveAsync()
    {
        if (_busy || !_editing) return;
        var name = _name.Text.Trim();
        var description = _description.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) { Alert("Category name is required."); return; }
        var input = new CategoryInput(name, description.Length == 0 ? null : description);
        _busy = true;
        try
        {
            if (_category is null)
            {
                if (!_permissions.Can(Permission.CreateLookups)) return;
                var created = await _categories.CreateAsync(input);
                if (!created.Success) { Alert(created.Error ?? "Unable to create category."); return; }
                var list = await _categories.GetAllAsync(true);
                _category = list.Where(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(c => c.CategoryId).FirstOrDefault();
                if (_category is null) { Saved?.Invoke(this, EventArgs.Empty); BackRequested?.Invoke(this, EventArgs.Empty); return; }
            }
            else
            {
                var result = await _categories.UpdateAsync(_category.CategoryId, input);
                if (!result.Success) { Alert(result.Error ?? "Unable to update category."); return; }
                _category = await _categories.GetByIdAsync(_category.CategoryId);
            }
            if (IsDisposed || Disposing) return;
            _editing = false;
            Populate(); UpdateMode();
            Saved?.Invoke(this, EventArgs.Empty);
            await LoadBooksAsync();
        }
        catch (Exception ex) { if (!IsDisposed && !Disposing) Alert(ex.Message); }
        finally { _busy = false; if (!IsDisposed && !Disposing) UpdateMode(); }
    }

    private async Task ChangeActiveAsync()
    {
        if (_busy || _category is null || !_permissions.Can(Permission.ManageLookups)) return;
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
            Populate(); UpdateMode(); Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { if (!IsDisposed && !Disposing) Alert(ex.Message); }
        finally { _busy = false; if (!IsDisposed && !Disposing) UpdateMode(); }
    }

    private async Task DeleteAsync()
    {
        if (_busy || _category is null || !_permissions.Can(Permission.HardDelete) ||
            !_permissions.Can(Permission.ManageLookups)) return;
        if (!ThemedMessageDialog.Confirm(FindForm(), "Delete Category",
            $"Permanently delete '{_category.Name}'? Categories linked to books cannot be deleted.",
            danger: true, confirmText: "Delete")) return;
        _busy = true;
        try
        {
            var result = await _categories.DeleteAsync(_category.CategoryId);
            if (!result.Success) { Alert(result.Error ?? "Deletion blocked."); return; }
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
        _loadingCovers = true;
        try
        {
            int categoryId = _category.CategoryId;
            var all = await _books.GetAllAsync(true);
            if (IsDisposed || Disposing || _category?.CategoryId != categoryId) return;
            var related = all.Where(b => b.CategoryIds.Contains(categoryId) || b.CategoryId == categoryId)
                .OrderBy(b => b.Title, StringComparer.OrdinalIgnoreCase).ToArray();
            _gallery.SuspendLayout();
            foreach (Control control in _gallery.Controls.Cast<Control>().ToArray())
            {
                foreach (var picture in control.Controls.OfType<Panel>().SelectMany(p => p.Controls.OfType<PictureBox>()))
                {
                    picture.Image?.Dispose();
                    picture.Image = null;
                }
                control.Dispose();
            }
            _gallery.Controls.Clear();
            _galleryHeading.Text = $"Associated Books  ({related.Length})";
            if (related.Length == 0)
            {
                _gallery.Controls.Add(new Label
                {
                    Text = "No books assigned to this category.",
                    AutoSize = true,
                    ForeColor = Theme.Colors.TextMuted,
                    Font = Theme.Body
                });
            }
            foreach (var book in related)
            {
                var card = MakeBookCard(book, out PictureBox cover);
                _gallery.Controls.Add(card);
                _ = LoadCoverAsync(cover, book.CoverPath);
            }
            _gallery.ResumeLayout(true);
            ArrangeGallery();
        }
        catch (Exception ex) { if (!IsDisposed && !Disposing) _galleryHeading.Text = "Unable to load associated books: " + ex.Message; }
        finally { _loadingCovers = false; }
    }

    // Show complete rows in the visible gallery viewport. At 1280x720 this
    // means one full row, with subsequent rows accessible by themed scrolling;
    // at taller desktop sizes, two or more full rows can fit naturally.
    private void ArrangeGallery()
    {
        if (_galleryArranging || IsDisposed || Disposing) return;
        _galleryArranging = true;
        try
        {
            const int gap = 12;
            const int preferredRowHeight = 244;
            int viewportWidth = Math.Max(150, _galleryScroll.ClientSize.Width - 18);
            int viewportHeight = Math.Max(1, _galleryScroll.ClientSize.Height);
            int count = Math.Max(1, _gallery.Controls.Count);
            int columns = Math.Max(1, (viewportWidth + gap) / (170 + gap));
            // Match the accepted Author Details gallery cover width at 1280px,
            // even when a category contains only one book. Do not stretch cards
            // to fill the viewport; unused room belongs to the gallery surface.
            int cardWidth = Math.Min(183,
                Math.Max(125, (viewportWidth - columns * gap) / columns));
            int rows = (count + columns - 1) / columns;

            // No partial second row: each visible row occupies an exact share
            // of the viewport. The extra 1px keeps the next row fully hidden.
            int visibleRows = Math.Max(1, viewportHeight / preferredRowHeight);
            int rowHeight = Math.Max(preferredRowHeight, viewportHeight / visibleRows + 1);
            // Keep Category book cards the same visual height as Author cards
            // at the approved 1280x720 desktop size, even though this page has
            // a shorter information panel and therefore a taller gallery.
            // Retain rowHeight for vertical scrolling so the following row
            // does not peek into the visible viewport.
            int cardHeight = Math.Min(241, rowHeight - gap);
            int titleHeight = 42;
            int frameHeight = cardHeight - titleHeight;

            _gallery.SuspendLayout();
            _gallery.Width = viewportWidth;
            foreach (var card in _gallery.Controls.OfType<Panel>())
            {
                card.Size = new Size(cardWidth, cardHeight);
                var frame = card.Controls.OfType<Panel>()
                    .FirstOrDefault(p => Equals(p.Tag, "cover-frame"));
                if (frame is not null)
                {
                    frame.Bounds = new Rectangle(0, 0, cardWidth, frameHeight);
                    var cover = frame.Controls.OfType<PictureBox>().FirstOrDefault();
                    if (cover is not null)
                        cover.Bounds = new Rectangle(7, 0, Math.Max(1, cardWidth - 7), frameHeight);
                }
                foreach (var label in card.Controls.OfType<Label>())
                    if (Equals(label.Tag, "title"))
                        label.Bounds = new Rectangle(0, frameHeight, cardWidth, titleHeight);
            }
            _gallery.Height = rows * rowHeight + 1;
            _gallery.ResumeLayout(true);
            _galleryScroll.RefreshMetrics();
        }
        finally { _galleryArranging = false; }
    }

    private Control MakeBookCard(Book book, out PictureBox cover)
    {
        var c = Theme.Colors;
        var card = new Panel
        {
            Size = new Size(150, 232),
            Margin = new Padding(0, 0, 12, 12),
            Cursor = Cursors.Hand,
            BackColor = c.SurfaceAlt
        };
        // Separate frame = reliable accent spine, independent of loaded images.
        var frame = new Panel
        {
            Tag = "cover-frame",
            Bounds = new Rectangle(0, 0, 150, 190),
            BackColor = c.AccentSoft,
            Cursor = Cursors.Hand
        };
        frame.Paint += (_, e) =>
        {
            using var spine = new SolidBrush(Theme.Colors.Accent);
            e.Graphics.FillRectangle(spine, 0, 0, Math.Min(7, frame.Width), frame.Height);
        };
        cover = new PictureBox
        {
            Bounds = new Rectangle(7, 0, 143, 190),
            BackColor = c.AccentSoft,
            SizeMode = PictureBoxSizeMode.Zoom,
            Cursor = Cursors.Hand
        };
        frame.Controls.Add(cover);
        var title = new Label
        {
            Tag = "title",
            Bounds = new Rectangle(0, 190, 150, 42),
            Text = book.Title,
            AutoEllipsis = true,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.Caption,
            BackColor = c.SurfaceAlt,
            ForeColor = c.Text,
            Cursor = Cursors.Hand,
            Padding = new Padding(3, 1, 3, 1)
        };
        card.Controls.AddRange(new Control[] { frame, title });
        foreach (var item in new Control[] { card, frame, cover, title })
        {
            item.Click += (_, _) => BookRequested?.Invoke(this, book.BookId);
            item.MouseWheel += (_, e) => _galleryScroll.ScrollByPixels(e.Delta > 0 ? -244 : 244);
            _tips.SetToolTip(item, "View Book: " + book.Title);
        }
        return card;
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        _layout.BackColor = c.Background;
        _header.BackColor = c.Background;
        _heading.ForeColor = c.Text; _heading.Font = Theme.Title;
        _subtitle.ForeColor = c.TextMuted; _subtitle.Font = Theme.Body;
        _galleryHeading.ForeColor = c.Text; _galleryHeading.Font = Theme.BodyStrong;
        _infoPanel.BackColor = c.Surface; _info.BackColor = c.Surface;
        _gallery.BackColor = c.Background; _actions.BackColor = c.Background;
        _galleryScroll.BackColor = c.Background;
        foreach (var card in _gallery.Controls.OfType<Panel>())
        {
            card.BackColor = c.SurfaceAlt;
            foreach (var frame in card.Controls.OfType<Panel>().Where(p => Equals(p.Tag, "cover-frame")))
            {
                frame.BackColor = c.AccentSoft;
                frame.Invalidate();
                foreach (var picture in frame.Controls.OfType<PictureBox>())
                    picture.BackColor = c.AccentSoft;
            }
            foreach (var label in card.Controls.OfType<Label>())
            {
                label.BackColor = c.SurfaceAlt;
                label.ForeColor = c.Text;
                label.Font = Theme.Caption;
            }
        }
        foreach (var (_, pair) in _fields)
        {
            pair.Caption.ForeColor = c.Text; pair.Caption.BackColor = c.Surface; pair.Caption.Font = Theme.Body;
            pair.Value.BackColor = c.SurfaceAlt; pair.Value.ForeColor = c.Text;
            pair.Value.Font = Theme.BodyStrong;
            pair.Value.Paint -= DrawFieldBorder;
            pair.Value.Paint += DrawFieldBorder;
        }
        _back.RefreshTheme();
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


    // Same right-aligned three-button geometry as BookEditorPage;
    // Cancel stays to the far left while editing.
    private void PositionFooterActions()
    {
        int width = _actions.ClientSize.Width;
        if (width < 1) return;
        const int gap = 8;
        const int height = 40;
        int actionWidth = Math.Min(210, Math.Max(110, (width * 2 / 3 - 2 * gap) / 3));
        int right = Math.Max(0, width - (actionWidth * 3 + gap * 2));
        _delete.SetBounds(right, 8, actionWidth, height);
        _archive.SetBounds(right + actionWidth + gap, 8, actionWidth, height);
        _primary.SetBounds(right + (actionWidth + gap) * 2, 8, actionWidth, height);
        // If hard deletion is restricted, keep the other two actions in their slots.
        _cancel.SetBounds(0, 8, Math.Min(115, Math.Max(80, right - gap)), height);
        if (_category is null)
            _primary.SetBounds(Math.Max(0, width - 160), 8, Math.Min(160, width), height);
    }

    // The same icon strokes, placement, foreground selection, and labels
    // as BookEditorPage.PaintActionIcon (Category wording substituted).
    private void PaintActionIcon(PaintEventArgs e, Button button, int kind)
    {
        if (button.Width < 90 || button.Height < 24) return;
        var c = Theme.Colors;
        var g = e.Graphics;
        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Color color = kind == 2 ? Color.White :
            kind == 0 ? Color.FromArgb(118, 235, 173) :
            (_category?.IsActive == false ? Color.FromArgb(118, 235, 173) : c.TextMuted);
        if (!button.Enabled) color = c.TextMuted;
        string label = kind == 0 ? (_editing ? "Save Category" : "Edit Category") : kind == 2 ? "Delete Category" :
            _category?.IsActive == false ? "Restore Category" : "Archive Category";
        Size labelSize = TextRenderer.MeasureText(label, button.Font);
        int cx = Math.Max(13, (button.Width - labelSize.Width - 30) / 2 + 9);
        int cy = button.Height / 2;
        using var pen = new Pen(color, 2f);
        if (kind == 0)
        {
            g.DrawLine(pen, cx - 8, cy - 7, cx + 1, cy - 7);
            g.DrawLine(pen, cx - 8, cy - 7, cx - 8, cy + 8);
            g.DrawLine(pen, cx - 8, cy + 8, cx + 7, cy + 8);
            g.DrawLine(pen, cx + 7, cy + 8, cx + 7, cy + 1);
            g.DrawLine(pen, cx - 2, cy + 3, cx + 8, cy - 7);
            g.DrawLine(pen, cx - 3, cy + 5, cx - 2, cy + 3);
        }
        else if (kind == 1)
        {
            g.DrawRectangle(pen, cx - 8, cy - 4, 16, 12);
            g.DrawLine(pen, cx - 10, cy - 8, cx + 10, cy - 8);
            bool restoring = _category?.IsActive == false;
            g.DrawLine(pen, cx, restoring ? cy + 5 : cy - 2,
                cx, restoring ? cy - 2 : cy + 5);
            if (restoring)
            {
                g.DrawLine(pen, cx - 4, cy + 1, cx, cy - 3);
                g.DrawLine(pen, cx, cy - 3, cx + 4, cy + 1);
            }
            else
            {
                g.DrawLine(pen, cx - 4, cy + 1, cx, cy + 5);
                g.DrawLine(pen, cx, cy + 5, cx + 4, cy + 1);
            }
        }
        else
        {
            g.DrawLine(pen, cx - 9, cy - 6, cx + 9, cy - 6);
            g.DrawLine(pen, cx - 4, cy - 9, cx + 4, cy - 9);
            g.DrawRectangle(pen, cx - 7, cy - 3, 14, 12);
            g.DrawLine(pen, cx - 3, cy, cx - 3, cy + 6);
            g.DrawLine(pen, cx + 3, cy, cx + 3, cy + 6);
        }
        var textRect = new Rectangle(cx + 16, 0,
            Math.Max(0, button.Width - cx - 18), button.Height);
        TextRenderer.DrawText(g, label, button.Font, textRect,
            button.Enabled ? button.ForeColor : c.TextMuted,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left |
            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        g.SmoothingMode = previous;
    }

    // Ported from the working BookEditorPage.BackButton.
    private sealed class BackButton : Control
    {
        private bool _hovered;
        public BackButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.Selectable, true);
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleName = "Back to Categories";
        }
        public void RefreshTheme()
        {
            BackColor = Theme.Colors.Surface;
            ForeColor = Theme.Colors.Text;
            Font = Theme.Body;
            Invalidate();
        }
        protected override void OnMouseEnter(EventArgs e)
        { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e)
        { base.OnMouseLeave(e); _hovered = false; Invalidate(); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode is Keys.Enter or Keys.Space)
            { OnClick(EventArgs.Empty); e.Handled = true; e.SuppressKeyPress = true; }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var c = Theme.Colors;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var background = new SolidBrush(_hovered ? c.SurfaceAlt : c.Surface);
            g.FillRectangle(background, ClientRectangle);
            using var border = new Pen(c.Border);
            g.DrawRectangle(border, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
            int iconX = 18;
            int iconY = Height / 2;
            using var pen = new Pen(c.Accent, 2.2f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLines(pen, new[] { new Point(iconX + 4, iconY - 6),
                new Point(iconX - 2, iconY), new Point(iconX + 4, iconY + 6) });
            TextRenderer.DrawText(g, "Back", Font,
                new Rectangle(31, 0, Math.Max(0, Width - 34), Height), c.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            if (Focused && ShowFocusCues)
                ControlPaint.DrawFocusRectangle(g, new Rectangle(3, 3,
                    Math.Max(0, Width - 6), Math.Max(0, Height - 6)));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tips.Dispose();
            foreach (PictureBox image in _gallery.Controls.OfType<Panel>().SelectMany(card => card.Controls.OfType<Panel>()).SelectMany(frame => frame.Controls.OfType<PictureBox>()))
            {
                image.Image?.Dispose();
                image.Image = null;
            }
        }
        base.Dispose(disposing);
    }
}

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

/// <summary>In-shell Author Add/View/Edit, with a fixed information panel and scrollable books.</summary>
public sealed class AuthorDetailsPage : ThemedUserControl
{
    private readonly IAuthorCreationService _authors;
    private readonly IBookService _books;
    private readonly IPermissionService _permissions;
    private AuthorRecord? _author;
    private bool _editing;
    private bool _busy;
    private bool _loaded;
    private bool _loadingCovers;

    private readonly TableLayoutPanel _layout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(0, 0, 0, 16), Margin = Padding.Empty };
    private readonly DetailsPageHeader _header = new();
    private readonly Panel _infoPanel = new()
    {
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
        Padding = new Padding(16)
    };
    private readonly TableLayoutPanel _info = new() { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly Dictionary<string, (Label Caption, Label Value, Control Edit)> _fields = new();
    private readonly ThemedTextField _first = new() { MaxLength = 100 };
    private readonly ThemedTextField _middle = new() { MaxLength = 100 };
    private readonly ThemedTextField _last = new() { MaxLength = 100 };
    private readonly ThemedFilterDropdown _gender = new(new[] { "Unknown", "Male", "Female", "Other" }) { TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 8, 0) };
    private readonly ThemedTextField _birthDate = new() { MaxLength = 10 };
    private readonly Button _calendar = new() { Text = "▦", FlatStyle = FlatStyle.Flat, Width = 40, Dock = DockStyle.Right };
    private readonly Label _id = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly AssociatedBooksGallery _associatedBooks = new() { Dock = DockStyle.Fill };
    private readonly DetailsActionFooter _actions = new();
    private readonly Button _primary = new() { FlatStyle = FlatStyle.Flat, Width = 210, Height = 40, Cursor = Cursors.Hand };
    private readonly Button _cancel = new() { Text = "Cancel", FlatStyle = FlatStyle.Flat, Width = 115, Height = 40, Cursor = Cursors.Hand };
    private readonly Button _archive = new() { FlatStyle = FlatStyle.Flat, Width = 210, Height = 40, Cursor = Cursors.Hand };
    private readonly Button _delete = new() { FlatStyle = FlatStyle.Flat, Width = 210, Height = 40, Cursor = Cursors.Hand };
    private readonly ToolTip _tips = new();
    private readonly CatalogValidationController _validation = new();
    private bool _validationAttempted;

    public event EventHandler? BackRequested;
    public event EventHandler<int>? BookRequested;
    public event EventHandler? Saved;

    public AuthorDetailsPage(IAuthorCreationService authors, IBookService books,
        IPermissionService permissions, AuthorRecord? author)
    {
        _authors = authors ?? throw new ArgumentNullException(nameof(authors));
        _books = books ?? throw new ArgumentNullException(nameof(books));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _author = author;
        _editing = author is null;
        Dock = DockStyle.Fill;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var h in new[] { 56f, 42f, 176f, 0f, 52f })
            _layout.RowStyles.Add(h == 0 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, h));
        _layout.Controls.Add(_header, 0, 0);
        _layout.SetRowSpan(_header, 2);
        _layout.Controls.Add(_infoPanel, 0, 2);

        _info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        _info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        _info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
        _info.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        _info.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        _infoPanel.Controls.Add(_info);
        AddField("ID", "Author ID", _id, 0, 0);
        AddField("First", "First Name", _first, 1, 0);
        AddField("Middle", "Middle Name (optional)", _middle, 2, 0);
        AddField("Last", "Last Name", _last, 0, 1);
        AddField("Gender", "Gender", _gender, 1, 1);
        var dateEditor = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        _birthDate.Dock = DockStyle.Fill;
        _calendar.Click += (_, _) => OpenCalendar();
        dateEditor.Controls.Add(_birthDate);
        dateEditor.Controls.Add(_calendar);
        _calendar.BringToFront();
        AddField("DOB", "Birth Date (YYYY-MM-DD)", dateEditor, 2, 1);

        _associatedBooks.BookRequested += (_, bookId) =>
            BookRequested?.Invoke(this, bookId);
        _layout.Controls.Add(_associatedBooks, 0, 3);
        _actions.Controls.AddRange(new Control[] { _delete, _archive, _primary, _cancel });
        _actions.Configure(_delete, _archive, _primary, _cancel, () => _author is null);
        _primary.Paint += (_, e) => PaintActionIcon(e, _primary, 0);
        _archive.Paint += (_, e) => PaintActionIcon(e, _archive, 1);
        _delete.Paint += (_, e) => PaintActionIcon(e, _delete, 2);
        _layout.Controls.Add(_actions, 0, 4);
        Controls.Add(_layout);
        _tips.SetToolTip(_calendar, "Choose birth date");
        _header.BackRequested += (_, _) => { if (!_busy) BackRequested?.Invoke(this, EventArgs.Empty); };
        _cancel.Click += (_, _) => { if (_busy) return; _editing = false; _validation.Clear(); _validationAttempted = false; Populate(); UpdateMode(); RefreshValidationCaptions(); };
        _primary.Click += async (_, _) =>
        {
            if (_busy) return;
            if (!_editing) { _editing = true; UpdateMode(); return; }
            await SaveAsync();
        };
        _archive.Click += async (_, _) => await ChangeActiveAsync();
        _delete.Click += async (_, _) => await DeleteAsync();
        // Fixed-height caption rows: no added width or horizontal scrolling.
        _validation.Register(_first, () => !string.IsNullOrWhiteSpace(_first.Text),
            "First name is required.");
        _validation.Register(_last, () => !string.IsNullOrWhiteSpace(_last.Text),
            "Last name is required.");
        _validation.Register(_birthDate, IsBirthDateValid,
            "Birth date must use YYYY-MM-DD and cannot be in the future.");
        _first.TextChanged += (_, _) => RefreshValidationCaptions();
        _last.TextChanged += (_, _) => RefreshValidationCaptions();
        _birthDate.TextChanged += (_, _) => RefreshValidationCaptions();
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
        _first.Text = _author?.FirstName ?? "";
        _middle.Text = _author?.MiddleName ?? "";
        _last.Text = _author?.LastName ?? "";
        _id.Text = _author?.AuthorId.ToString(CultureInfo.InvariantCulture) ?? "Generated on Save";
        _gender.SelectedIndex = _author?.Gender switch
        {
            "Male" => 1,
            "Female" => 2,
            "Other" => 3,
            _ => 0
        };
        _birthDate.Text = _author?.BirthDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
    }

    private void UpdateMode()
    {
        bool existing = _author is not null;
        _header.HeadingText = !existing ? "Add Author" : _editing ? "Edit Author" : "View Author";
        _header.SubtitleText = !existing ? "Create a new author record." :
            (_editing ? "Editing: " : "Viewing: ") + _author!.FullName;
        _primary.Text = _editing ? "Save Author" : "Edit Author";
        _primary.Visible = _editing || _permissions.Can(Permission.ManageLookups);
        _primary.Enabled = !_busy;
        _cancel.Visible = existing && _editing;
        // Mirror Book View/Edit: the three main actions remain visible in both modes.
        _archive.Visible = existing && _permissions.Can(Permission.ManageLookups);
        _archive.Text = "";
        _archive.AccessibleName = _author?.IsActive == false ? "Restore Author" : "Archive Author";
        _delete.Visible = existing && _permissions.Can(Permission.HardDelete) && _permissions.Can(Permission.ManageLookups);
        _delete.Text = "";
        _delete.AccessibleName = "Delete Author";
        _primary.Text = "";
        _primary.AccessibleName = _editing ? "Save Author" : "Edit Author";
        if (!existing) _associatedBooks.ShowBeforeSave();
        foreach (var (key, pair) in _fields)
        {
            string text = key switch
            {
                "ID" => _id.Text,
                "First" => _author?.FirstName ?? "—",
                "Middle" => string.IsNullOrWhiteSpace(_author?.MiddleName) ? "—" : _author!.MiddleName!,
                "Last" => _author?.LastName ?? "—",
                "Gender" => _author?.Gender ?? "Unknown",
                "DOB" => _author?.BirthDate?.ToString("MMMM d, yyyy", CultureInfo.CurrentCulture) ?? "—",
                _ => "—"
            };
            pair.Value.Text = text;
            pair.Edit.Visible = _editing && key != "ID";
            pair.Value.Visible = !_editing || key == "ID";
            if (pair.Value.Visible) pair.Value.BringToFront();
        }
        _calendar.Enabled = _editing;
        ApplyTheme();
        PositionFooterActions();
        _primary.Invalidate();
        _archive.Invalidate();
        _delete.Invalidate();
    }

    private void OpenCalendar()
    {
        if (!_editing || _busy) return;
        DateTime initial = DateTime.Today;
        if (DateTime.TryParseExact(_birthDate.Text.Trim(), "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) initial = parsed;
        using var dialog = new ThemedBirthDateDialog(initial);
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            _birthDate.Text = dialog.SelectedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
    }

    private bool IsBirthDateValid()
    {
        string value = _birthDate.Text.Trim();
        return value.Length == 0 ||
            (DateTime.TryParseExact(value, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date) &&
             date.Date <= DateTime.Today);
    }

    private void RefreshValidationCaptions()
    {
        if (!_validationAttempted || IsDisposed || Disposing) return;
        UpdateValidationCaption("First", "First Name", _first.HasValidationError, "Required");
        UpdateValidationCaption("Last", "Last Name", _last.HasValidationError, "Required");
        UpdateValidationCaption("DOB", "Birth Date (YYYY-MM-DD)",
            _birthDate.HasValidationError, "Invalid date");
    }

    private void UpdateValidationCaption(string key, string title, bool invalid, string error)
    {
        if (!_fields.TryGetValue(key, out var field)) return;
        field.Caption.Text = invalid ? title + " • " + error : title;
        field.Caption.ForeColor = invalid ? Theme.Colors.Danger : Theme.Colors.Text;
        field.Caption.AutoEllipsis = true;
    }

    private async Task SaveAsync()
    {
        if (_busy || !_editing) return;
        _validationAttempted = true;
        var errors = _validation.Validate();
        RefreshValidationCaptions();
        if (errors.Count > 0)
        {
            _validation.ShowSummary(FindForm(), "this author");
            if (!IsDisposed && !Disposing) _validation.FocusFirstInvalid();
            return;
        }
        string first = _first.Text.Trim(), last = _last.Text.Trim(), middle = _middle.Text.Trim();
        DateTime? dob = null;
        if (_birthDate.Text.Trim().Length > 0)
        {
            DateTime.TryParseExact(_birthDate.Text.Trim(), "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed);
            dob = parsed.Date;
        }
        var input = new AuthorInput(first, middle.Length == 0 ? null : middle, last,
            _gender.SelectedIndex switch { 1 => "Male", 2 => "Female", 3 => "Other", _ => "Unknown" }, dob);
        bool creating = _author is null;
        _busy = true;
        try
        {
            if (_author is null)
            {
                var created = await _authors.CreateAsync(input);
                if (!created.Success || !created.AuthorId.HasValue)
                {
                    Alert(created.Error ?? "Unable to create author."); return;
                }
                _author = await _authors.GetByIdAsync(created.AuthorId.Value);
            }
            else
            {
                var result = await _authors.UpdateAsync(_author.AuthorId, input);
                if (!result.Success) { Alert(result.Error ?? "Unable to update author."); return; }
                _author = await _authors.GetByIdAsync(_author.AuthorId);
            }
            if (IsDisposed || Disposing) return;
            _editing = false;
            _validation.Clear();
            _validationAttempted = false;
            Populate();
            UpdateMode();
            Saved?.Invoke(this, EventArgs.Empty);
            CatalogActionFeedback.Notify(this,
                creating ? "Author added successfully." : "Author updated successfully.");
            await LoadBooksAsync();
        }
        catch (Exception ex) { if (!IsDisposed && !Disposing) Alert(ex.Message); }
        finally { _busy = false; if (!IsDisposed && !Disposing) UpdateMode(); }
    }

    private async Task ChangeActiveAsync()
    {
        if (_busy || _author is null || !_permissions.Can(Permission.ManageLookups)) return;
        bool restore = !_author.IsActive;
        if (!ThemedMessageDialog.Confirm(FindForm(), restore ? "Restore Author" : "Archive Author",
            $"{(restore ? "Restore" : "Archive")} '{_author.FullName}'?", danger: !restore,
            confirmText: restore ? "Restore" : "Archive")) return;
        _busy = true;
        try
        {
            var result = await _authors.SetActiveAsync(_author.AuthorId, restore);
            if (!result.Success) { Alert(result.Error ?? "Unable to change status."); return; }
            _author = await _authors.GetByIdAsync(_author.AuthorId);
            if (IsDisposed || Disposing) return;
            Populate(); UpdateMode(); Saved?.Invoke(this, EventArgs.Empty);
            CatalogActionFeedback.Notify(this,
                restore ? "Author restored successfully." : "Author archived successfully.");
        }
        catch (Exception ex) { if (!IsDisposed && !Disposing) Alert(ex.Message); }
        finally { _busy = false; if (!IsDisposed && !Disposing) UpdateMode(); }
    }

    private async Task DeleteAsync()
    {
        if (_busy || _author is null || !_permissions.Can(Permission.HardDelete) ||
            !_permissions.Can(Permission.ManageLookups)) return;
        if (!ThemedMessageDialog.Confirm(FindForm(), "Delete Author",
            $"Permanently delete '{_author.FullName}'? Authors linked to books cannot be deleted.",
            danger: true, confirmText: "Delete")) return;
        _busy = true;
        try
        {
            var result = await _authors.DeleteAsync(_author.AuthorId);
            if (!result.Success) { Alert(result.Error ?? "Deletion blocked."); return; }
            Saved?.Invoke(this, EventArgs.Empty);
            CatalogActionFeedback.Notify(this, "Author deleted successfully.");
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { if (!IsDisposed && !Disposing) Alert(ex.Message); }
        finally { _busy = false; }
    }

    private void Alert(string message) => ThemedMessageDialog.ShowAlert(FindForm(), "Author Management", message, danger: true);

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (_loaded || _author is null) return;
        _loaded = true;
        await LoadBooksAsync();
    }

    public Task RefreshBooksAsync() => LoadBooksAsync();

    private async Task LoadBooksAsync()
    {
        if (_author is null || _loadingCovers) return;
        _loadingCovers = true;
        try
        {
            int authorId = _author.AuthorId;
            var all = await _books.GetAllAsync(true);
            if (IsDisposed || Disposing || _author?.AuthorId != authorId) return;
            var related = all.Where(b => b.AuthorIds.Contains(authorId))
                .OrderBy(b => b.Title, StringComparer.OrdinalIgnoreCase).ToArray();
            _associatedBooks.SetBooks(related, "No books associated with this author.");
        }
        catch (Exception)
        {
            if (!IsDisposed && !Disposing)
                _associatedBooks.ShowError("Unable to load associated books. Try reopening this author.");
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
        _gender.ApplyTheme(); // ThemedTextField applies themes via its own lifecycle.
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
        StyleButton(_calendar, c.SurfaceAlt, c.Text, c.Border);
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


    // Shared footer owns geometry; page keeps all button behavior and styling.
    private void PositionFooterActions() => _actions.ArrangeActions();

    // The same icon strokes, placement, foreground selection, and labels
    // as BookEditorPage.PaintActionIcon (Author wording substituted).
    private void PaintActionIcon(PaintEventArgs e, Button button, int kind)
    {
        if (button.Width < 90 || button.Height < 24) return;
        var c = Theme.Colors;
        var g = e.Graphics;
        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Color color = kind == 2 ? Color.White :
            kind == 0 ? Color.FromArgb(118, 235, 173) :
            (_author?.IsActive == false ? Color.FromArgb(118, 235, 173) : c.TextMuted);
        if (!button.Enabled) color = c.TextMuted;
        string label = kind == 0 ? (_editing ? "Save Author" : "Edit Author") : kind == 2 ? "Delete Author" :
            _author?.IsActive == false ? "Restore Author" : "Archive Author";
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
            bool restoring = _author?.IsActive == false;
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tips.Dispose();
            _validation.Dispose();
        }
        base.Dispose(disposing);
    }
}

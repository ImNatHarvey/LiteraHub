
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using LiteraHub.Core.Interfaces;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

public sealed class BookEditorPage : ThemedUserControl
{
    private readonly Book? _existing;

    private readonly TableLayoutPanel _layout = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 3,
        Margin = Padding.Empty
    };

    private readonly TableLayoutPanel _header = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Margin = Padding.Empty
    };

    private readonly Label _heading = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = Padding.Empty
    };

    private readonly BackButton _back = new()
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(8, 4, 2, 4)
    };

    private readonly Label _description = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly Panel _viewport = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = false,
        Margin = Padding.Empty
    };

    private readonly Panel _content = new()
    {
        Margin = Padding.Empty
    };

    private readonly EditorScrollBar _scrollbar = new()
    {
        Dock = DockStyle.Right,
        Width = 12,
        Visible = false
    };

    private readonly CoverPreview _cover = new();

    private readonly ThemedTextField _isbn = new()
    {
        MaxLength = 20
    };

    private readonly ThemedTextField _title = new()
    {
        MaxLength = 255
    };

    private readonly ThemedTextField _bookDescription = new()
    {
        Multiline = true
    };

    private readonly ThemedTextField _authors = new();

    // Reuse the same themed popup dropdown as the Books toolbar.
    // Category options remain placeholders until database integration.
    private ThemedFilterDropdown _category =
        new(new[] { "Select category" });

    private readonly ThemedFilterDropdown _condition =
        new(new[] { "Good", "Damaged" });

    private readonly NumberField _total = new();
    private readonly NumberField _available = new();

    private readonly LookupButton _lookup = new() { Enabled = false };

    private readonly Button _save = new()
    {
        Text = "Save Book",
        FlatStyle = FlatStyle.Flat,
        Enabled = false
    };

    private readonly Label _coverLabel;
    private readonly Label _bookSection;
    private readonly Label _classificationSection;
    private readonly Label _inventorySection;

    private readonly Label _isbnLabel;
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly Label _authorsLabel;
    private readonly Label _categoryLabel;
    private readonly Label _conditionLabel;
    private readonly Label _totalLabel;
    private readonly Label _availableLabel;

    private ICategoryService? _categoryService;
    private IBookMetadataService? _metadataService;
    private bool _lookupBusy;
    private CancellationTokenSource? _coverLoad;
    private static readonly HttpClient CoverHttp = new() { Timeout = TimeSpan.FromSeconds(12) };
    private bool _categoriesLoading;
    private int[] _categoryIds = Array.Empty<int>();

    // This is populated from the database. Zero means no selection.
    public int SelectedCategoryId =>
        _category.SelectedIndex >= 0 &&
        _category.SelectedIndex < _categoryIds.Length
            ? _categoryIds[_category.SelectedIndex]
            : 0;

    public void ConfigureServices(
        ICategoryService categories,
        IBookMetadataService metadata)
    {
        _categoryService = categories ??
            throw new ArgumentNullException(nameof(categories));
        _metadataService = metadata ??
            throw new ArgumentNullException(nameof(metadata));
        _lookup.Enabled = true;

        if (IsHandleCreated)
            _ = LoadCategoriesAsync();
        else
            HandleCreated += HandleEditorCreated;
    }

    private void HandleEditorCreated(object? sender, EventArgs e)
    {
        HandleCreated -= HandleEditorCreated;
        _ = LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        if (_categoriesLoading || _categoryService is null)
            return;
        _categoriesLoading = true;
        try
        {
            var items = await _categoryService.GetAllAsync(false);
            if (IsDisposed || Disposing)
                return;

            var active = items.Where(c => c.IsActive)
                .OrderBy(c => c.Name).ToArray();
            string[] options = new string[active.Length + 1];
            int[] ids = new int[active.Length + 1];
            options[0] = "Select category";
            for (int i = 0; i < active.Length; i++)
            {
                options[i + 1] = active[i].Name;
                ids[i + 1] = active[i].CategoryId;
            }

            var previous = _category;
            var replacement = new ThemedFilterDropdown(options);
            _category = replacement;
            _categoryIds = ids;
            _content.Controls.Add(replacement);
            previous.Dispose();
            if (_existing is not null)
            {
                int index = Array.IndexOf(ids, _existing.CategoryId);
                if (index > 0)
                    replacement.SelectedIndex = index;
            }
            replacement.ApplyTheme();
            ArrangeForm();
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                _description.Text = "Unable to load categories: " + ex.Message;
        }
        finally
        {
            _categoriesLoading = false;
        }
    }

    private async Task LookupIsbnAsync()
    {
        if (_lookupBusy || _metadataService is null)
            return;
        string isbn = _isbn.Text.Trim();
        if (isbn.Length == 0)
        {
            _description.Text = "Enter an ISBN before searching.";
            return;
        }

        _lookupBusy = true;
        _lookup.IsBusy = true;
        _lookup.Enabled = false;
        _description.Text = "Looking up ISBN...";
        try
        {
            var result = await _metadataService.LookupByIsbnAsync(isbn);
            if (IsDisposed || Disposing)
                return;
            if (!result.Success || result.Metadata is null)
            {
                _description.Text = result.Error ?? "ISBN not found.";
                return;
            }
            var book = result.Metadata;
            _title.Text = book.Title;
            _bookDescription.Text = book.Description ?? string.Empty;
            _authors.Text = string.Join(", ", book.Authors);
            _ = LoadCoverAsync(book.CoverUrl);
            _description.Text = "Metadata found. Review the fields before saving.";
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                _description.Text = "ISBN lookup failed: " + ex.Message;
        }
        finally
        {
            _lookupBusy = false;
            _lookup.IsBusy = false;
            if (!IsDisposed && !Disposing)
                _lookup.Enabled = _metadataService is not null;
        }
    }

    private async Task LoadCoverAsync(string? coverUrl)
    {
        _coverLoad?.Cancel();
        _coverLoad?.Dispose();
        _coverLoad = new CancellationTokenSource();
        var token = _coverLoad.Token;
        _cover.CoverImage = null;

        // Only fetch the cover from the trusted Open Library image host.
        if (!Uri.TryCreate(coverUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !uri.Host.Equals("covers.openlibrary.org", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            using var response = await CoverHttp.GetAsync(uri,
                HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > 4_000_000)
                return;
            var bytes = await response.Content.ReadAsByteArrayAsync(token);
            if (bytes.Length > 4_000_000 || token.IsCancellationRequested)
                return;
            using var stream = new MemoryStream(bytes);
            using var loaded = Image.FromStream(stream);
            var bitmap = new Bitmap(loaded);
            if (IsDisposed || Disposing || token.IsCancellationRequested)
            {
                bitmap.Dispose();
                return;
            }
            _cover.CoverImage = bitmap;
        }
        catch (Exception ex) when (ex is HttpRequestException or
            TaskCanceledException or ArgumentException or OutOfMemoryException)
        {
            // Keep the first-letter placeholder if the cover is unavailable.
        }
    }

    private int _scrollOffset;
    private int _contentHeight;
    private bool _arranging;
    private bool _syncingInventory;
    private int _reservedCopies;

    public event EventHandler? BackRequested;

    public BookEditorPage(Book? existing = null)
    {
        _existing = existing;

        Dock = DockStyle.Fill;
        Padding = new Padding(Space.L);

        _heading.Text = existing is null
            ? "Add Book"
            : "Edit Book";

        _description.Text = existing is null
            ? "Create a new book record."
            : $"Editing: {existing.Title}";

        _bookSection = MakeLabel("Book Information", true);
        _classificationSection =
            MakeLabel("Classification", true);
        _inventorySection = MakeLabel("Inventory", true);

        _coverLabel = MakeLabel("Cover Preview");
        _isbnLabel = MakeLabel("ISBN");
        _titleLabel = MakeLabel("Book Title");
        _descriptionLabel = MakeLabel("Description");
        _authorsLabel = MakeLabel("Authors");
        _categoryLabel = MakeLabel("Category");
        _conditionLabel = MakeLabel("Condition");
        _totalLabel = MakeLabel("Total Copies");
        _availableLabel = MakeLabel("Available Copies");

        BuildLayout();
        PopulateExistingBook();
        WireEvents();
        ApplyTheme();
    }

    private static Label MakeLabel(
        string text,
        bool section = false)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = section ? "section" : "field"
        };
    }

    private void BuildLayout()
    {
        _layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 56));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 42));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _header.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _header.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 100));

        _header.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _header.Controls.Add(_heading, 0, 0);
        _header.Controls.Add(_back, 1, 0);

        _content.Controls.AddRange(new Control[]
        {
            _bookSection,
            _coverLabel,
            _cover,
            _isbnLabel,
            _isbn,
            _lookup,
            _titleLabel,
            _title,
            _descriptionLabel,
            _bookDescription,
            _authorsLabel,
            _authors,
            _classificationSection,
            _categoryLabel,
            _category,
            _conditionLabel,
            _condition,
            _inventorySection,
            _totalLabel,
            _total,
            _availableLabel,
            _available,
            _save
        });

        _viewport.Controls.Add(_content);
        _viewport.Controls.Add(_scrollbar);

        _layout.Controls.Add(_header, 0, 0);
        _layout.Controls.Add(_description, 0, 1);
        _layout.Controls.Add(_viewport, 0, 2);

        Controls.Add(_layout);

    }

    private void WireEvents()
    {
        _back.Click += (_, _) =>
            BackRequested?.Invoke(this, EventArgs.Empty);

        _lookup.Click += async (_, _) => await LookupIsbnAsync();

        _title.TextChanged += (_, _) =>
        {
            string title = _title.Text.Trim();

            _cover.Letter = title.Length == 0
                ? "?"
                : title[..1].ToUpperInvariant();
        };

        _total.ValueChanged += (_, _) => SyncInventory();
        _available.ValueChanged += (_, _) => SyncInventory();

        Resize += (_, _) => ArrangeForm();
        _viewport.Resize += (_, _) => ArrangeForm();

        _scrollbar.OffsetRequested += SetScrollOffset;

        _viewport.MouseWheel += HandleWheel;
        _content.MouseWheel += HandleWheel;

        foreach (Control control in _content.Controls)
            control.MouseWheel += HandleWheel;
    }

    private void PopulateExistingBook()
    {
        _total.Value = 1;
        _available.Value = 1;

        if (_existing is null)
        {
            _reservedCopies = 0;
            SyncInventory();
            _cover.Letter = "?";
            return;
        }

        _isbn.Text = _existing.Isbn;
        _title.Text = _existing.Title;

        _bookDescription.Text =
            _existing.Description ?? "";

        _authors.Text = _existing.Authors;

        _reservedCopies = Math.Max(0,
            _existing.TotalCopies - _existing.AvailableCopies);
        _total.Value = _existing.TotalCopies;
        _available.Value = _existing.AvailableCopies;
        SyncInventory();

        // Existing category is displayed as read-only text until
        // category lookup is connected in the next checkpoint.
        // The themed category dropdown currently has one option.
        _condition.SelectedIndex =
            string.Equals(_existing.StatusName, "Damaged",
                StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        string title = _existing.Title?.Trim() ?? "";

        _cover.Letter = title.Length == 0
            ? "?"
            : title[..1].ToUpperInvariant();
    }

    // Protect copies that are currently unavailable when editing.
    // This is a UI guard; the service must recheck persisted loans on save.
    private void SyncInventory()
    {
        if (_syncingInventory)
            return;

        _syncingInventory = true;
        try
        {
            _total.Minimum = _reservedCopies;
            int maximumAvailable = Math.Max(
                0, _total.Value - _reservedCopies);
            _available.Maximum = maximumAvailable;
        }
        finally
        {
            _syncingInventory = false;
        }
    }

    private void ArrangeForm()
    {
        if (_arranging ||
            _viewport.ClientSize.Width < 1 ||
            _viewport.ClientSize.Height < 1)
            return;

        _arranging = true;

        try
        {
            bool portrait = Width < 700;

            _header.ColumnStyles[1].Width =
                portrait ? 88 : 100;

            _heading.Font = portrait
                ? Theme.BodyStrong
                : Theme.Title;

            int width = _viewport.ClientSize.Width;

            int height = portrait
                ? ArrangePortrait(width)
                : ArrangeDesktop(width);

            // Desktop layouts are designed to fit
            // without scrolling at 1280x720 and above.
            bool needsScroll =
                height > _viewport.ClientSize.Height;

            _scrollbar.Visible = needsScroll;

            if (needsScroll)
            {
                width = Math.Max(
                    1,
                    _viewport.ClientSize.Width -
                    _scrollbar.Width);

                height = portrait
                    ? ArrangePortrait(width)
                    : ArrangeDesktop(width);
            }

            _contentHeight = height;

            _scrollOffset = Math.Clamp(
                _scrollOffset,
                0,
                Math.Max(
                    0,
                    height - _viewport.ClientSize.Height));

            _content.SetBounds(
                0,
                -_scrollOffset,
                width,
                height);

            _scrollbar.SetMetrics(
                height,
                _viewport.ClientSize.Height,
                _scrollOffset);
        }
        finally
        {
            _arranging = false;
        }
    }

    private int ArrangeDesktop(int width)
    {
        const int pad = 16;
        const int gap = 22;
        int viewportHeight = _viewport.ClientSize.Height;
        int innerWidth = Math.Max(1, width - pad * 2);
        int columnWidth = Math.Max(80, (innerWidth - gap) / 2);
        int leftX = pad;
        int rightX = pad + columnWidth + gap;
        int actionY = Math.Max(pad, viewportHeight - pad - 40);
        int fieldBottom = actionY - 12;
        const int labelGap = 6;
        const int fieldGap = 7;
        bool wide = width >= 1200;

        if (wide)
        {
            // Wide layout: Book Information and Classification are
            // stacked in the FIRST column; Description is in the
            // SECOND column. Avoid the old column reversal.
            int leftY = pad;
            int rightY = pad;

            Section(_bookSection, leftX, ref leftY, columnWidth);

            // Keep the large cover, then place Classification
            // beneath Book Information in the first column.
            int fixedAfterCover =
                (19 + labelGap + 36 + fieldGap) + // ISBN
                (34 + fieldGap) +                 // lookup
                (19 + labelGap + 36 + fieldGap) + // title
                33 +                              // classification heading
                (19 + labelGap + 40 + fieldGap) * 2;
            int coverHeight = Math.Clamp(
                fieldBottom - leftY - (19 + labelGap + fieldGap)
                - fixedAfterCover,
                85, 230);
            int coverWidth = Math.Max(55,
                (int)Math.Round(coverHeight * 0.72));

            Field(_coverLabel, _cover, leftX, ref leftY,
                coverWidth, coverHeight, labelGap, fieldGap);
            Field(_isbnLabel, _isbn, leftX, ref leftY,
                columnWidth, 36, labelGap, fieldGap);
            _lookup.SetBounds(leftX, leftY, columnWidth, 34);
            leftY += 34 + fieldGap;
            Field(_titleLabel, _title, leftX, ref leftY,
                columnWidth, 36, labelGap, fieldGap);
            Section(_classificationSection, leftX,
                ref leftY, columnWidth);
            Field(_categoryLabel, _category, leftX,
                ref leftY, columnWidth, 40, labelGap, fieldGap);
            Field(_conditionLabel, _condition, leftX,
                ref leftY, columnWidth, 40, labelGap, fieldGap);

            // Second column: Inventory, Authors, and Description.
            // Description can leave some blank space below it.
            Section(_inventorySection, rightX,
                ref rightY, columnWidth);
            int numberWidth = Math.Max(40, (columnWidth - 12) / 2);
            int totalY = rightY;
            int availableY = rightY;
            Field(_totalLabel, _total, rightX,
                ref totalY, numberWidth, 40, labelGap, fieldGap);
            Field(_availableLabel, _available,
                rightX + numberWidth + 12,
                ref availableY, numberWidth, 40, labelGap, fieldGap);
            rightY = Math.Max(totalY, availableY);
            Field(_authorsLabel, _authors, rightX,
                ref rightY, columnWidth, 40, labelGap, fieldGap);
            Field(_descriptionLabel, _bookDescription,
                rightX, ref rightY, columnWidth,
                Math.Min(280, Math.Max(120, viewportHeight / 3)),
                labelGap, fieldGap);

            _save.SetBounds(Math.Max(pad, width - pad - 124),
                actionY, 124, 40);
            return Math.Max(viewportHeight,
                Math.Max(leftY, rightY) + pad);
        }

        // Preserve the already-approved 1280x720 arrangement exactly.
        int bookX = leftX;
        int classificationX = rightX;
        int bookY = pad;
        int classificationY = pad;

        Section(_classificationSection, classificationX,
            ref classificationY, columnWidth);
        Field(_categoryLabel, _category, classificationX,
            ref classificationY, columnWidth, 40, labelGap, fieldGap);
        Field(_conditionLabel, _condition, classificationX,
            ref classificationY, columnWidth, 40, labelGap, fieldGap);
        Section(_inventorySection, classificationX,
            ref classificationY, columnWidth);

        int numericWidth = Math.Max(40, (columnWidth - 12) / 2);
        int totalRowY = classificationY;
        int availableRowY = classificationY;
        Field(_totalLabel, _total, classificationX,
            ref totalRowY, numericWidth, 40, labelGap, fieldGap);
        Field(_availableLabel, _available,
            classificationX + numericWidth + 12,
            ref availableRowY, numericWidth, 40, labelGap, fieldGap);
        classificationY = Math.Max(totalRowY, availableRowY);
        Field(_authorsLabel, _authors, classificationX,
            ref classificationY, columnWidth, 40, labelGap, fieldGap);

        int descriptionHeight = Math.Max(48,
            fieldBottom - classificationY - 19 - labelGap);
        Field(_descriptionLabel, _bookDescription,
            classificationX, ref classificationY, columnWidth,
            descriptionHeight, labelGap, 0);

        Section(_bookSection, bookX, ref bookY, columnWidth);
        int afterCoverHeight =
            (19 + labelGap + 36 + fieldGap) +
            (34 + fieldGap) +
            (19 + labelGap + 36 + fieldGap);
        int coverLabelAndGap = 19 + labelGap + fieldGap;
        int coverHeight1280 = Math.Max(78,
            fieldBottom - bookY - coverLabelAndGap - afterCoverHeight);
        int coverWidth1280 = Math.Max(55,
            (int)Math.Round(coverHeight1280 * 0.72));

        Field(_coverLabel, _cover, bookX, ref bookY,
            coverWidth1280, coverHeight1280, labelGap, fieldGap);
        Field(_isbnLabel, _isbn, bookX, ref bookY,
            columnWidth, 36, labelGap, fieldGap);
        _lookup.SetBounds(bookX, bookY, columnWidth, 34);
        bookY += 34 + fieldGap;
        Field(_titleLabel, _title, bookX, ref bookY,
            columnWidth, 36, labelGap, fieldGap);

        _save.SetBounds(Math.Max(pad, width - pad - 124),
            actionY, 124, 40);
        return Math.Max(viewportHeight,
            Math.Max(bookY, classificationY) + pad);
    }

    private int ArrangePortrait(int width)
    {
        const int pad = 12;
        int x = pad;
        int fieldWidth = Math.Max(80, width - pad * 2);
        int y = pad;

        Section(_bookSection, x, ref y, fieldWidth);
        Field(_coverLabel, _cover, x, ref y, 88, 124);
        Field(_isbnLabel, _isbn, x, ref y, fieldWidth, 40);
        _lookup.SetBounds(x, y, fieldWidth, 36);
        y += 46;
        Field(_titleLabel, _title, x, ref y, fieldWidth, 40);

        Section(_classificationSection, x, ref y, fieldWidth);
        Field(_categoryLabel, _category, x, ref y,
            fieldWidth, 40);
        Field(_conditionLabel, _condition, x, ref y,
            fieldWidth, 40);
        Section(_inventorySection, x, ref y, fieldWidth);

        int numericWidth = Math.Max(40, (fieldWidth - 12) / 2);
        int totalY = y;
        int availableY = y;
        Field(_totalLabel, _total, x, ref totalY,
            numericWidth, 40);
        Field(_availableLabel, _available,
            x + numericWidth + 12, ref availableY,
            numericWidth, 40);
        y = Math.Max(totalY, availableY) + 8;

        Field(_authorsLabel, _authors, x, ref y,
            fieldWidth, 40);
        Field(_descriptionLabel, _bookDescription,
            x, ref y, fieldWidth, 90);
        y += 8;
        _save.SetBounds(x, y, fieldWidth, 40);
        return y + 40 + pad;
    }

    private static void Section(
        Label label,
        int x,
        ref int y,
        int width)
    {
        label.SetBounds(
            x, y, width, 25);

        y += 33;
    }

    private static void Field(
        Label label,
        Control input,
        int x,
        ref int y,
        int width,
        int height,
        int labelGap = 7,
        int fieldGap = 10)
    {
        label.SetBounds(
            x, y, width, 19);

        y += 19 + labelGap;

        input.SetBounds(
            x, y, width, height);

        y += height + fieldGap;
    }

    private void HandleWheel(
        object? sender,
        MouseEventArgs e)
    {
        if (!_scrollbar.Visible)
            return;

        int step = Math.Max(
            24,
            SystemInformation.MouseWheelScrollLines * 20);

        SetScrollOffset(
            _scrollOffset +
            (e.Delta > 0 ? -step : step));
    }

    private void SetScrollOffset(int offset)
    {
        int max = Math.Max(
            0,
            _contentHeight -
            _viewport.ClientSize.Height);

        _scrollOffset = Math.Clamp(
            offset, 0, max);

        _content.Top = -_scrollOffset;

        _scrollbar.SetMetrics(
            _contentHeight,
            _viewport.ClientSize.Height,
            _scrollOffset);
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();

        var c = Theme.Colors;

        BackColor = c.Background;
        ForeColor = c.Text;

        _layout.BackColor = c.Background;
        _header.BackColor = c.Background;

        _heading.BackColor = c.Background;
        _heading.ForeColor = c.Text;
        _heading.Font = Theme.Title;

        _description.BackColor = c.Background;
        _description.ForeColor = c.TextMuted;
        _description.Font = Theme.Body;

        _viewport.BackColor = c.Surface;
        _content.BackColor = c.Surface;

        foreach (Control control in _content.Controls)
        {
            if (control is not Label label)
                continue;

            label.BackColor = c.Surface;
            label.ForeColor = c.Text;

            label.Font = Equals(label.Tag, "section")
                ? Theme.BodyStrong
                : Theme.Body;
        }

        _category.ApplyTheme();
        _condition.ApplyTheme();

        _save.BackColor =
            Color.FromArgb(34, 139, 86);

        _save.ForeColor = Color.White;
        _save.Font = Theme.BodyStrong;
        _save.FlatAppearance.BorderSize = 0;

        _cover.RefreshTheme();
        _lookup.RefreshTheme();
        _total.RefreshTheme();
        _available.RefreshTheme();
        _back.RefreshTheme();
        _scrollbar.RefreshTheme();

        ArrangeForm();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _coverLoad?.Cancel();
            _coverLoad?.Dispose();
            _cover.CoverImage = null;
        }
        base.Dispose(disposing);
    }

    private sealed class CoverPreview : Control
    {
        private string _letter = "?";
        private Image? _coverImage;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image? CoverImage
        {
            get => _coverImage;
            set
            {
                if (ReferenceEquals(_coverImage, value)) return;
                var old = _coverImage;
                _coverImage = value;
                old?.Dispose();
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public string Letter
        {
            get => _letter;
            set
            {
                _letter = string.IsNullOrWhiteSpace(value)
                    ? "?"
                    : value;

                Invalidate();
            }
        }

        public CoverPreview()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        public void RefreshTheme()
        {
            BackColor = Theme.Colors.AccentSoft;
            ForeColor = Theme.Colors.Accent;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var c = Theme.Colors;
            var g = e.Graphics;

            g.Clear(c.AccentSoft);

            using var spine = new SolidBrush(
                Color.FromArgb(35, c.Accent));

            g.FillRectangle(
                spine,
                0, 0, 7, Height);

            if (_coverImage is not null)
            {
                var destination = new Rectangle(7, 0,
                    Math.Max(1, Width - 7), Math.Max(1, Height));
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_coverImage, destination);
                return;
            }

            float fontSize = Math.Clamp(
                Height * 0.30f,
                22f,
                58f);

            using var font = new Font(
                Theme.Title.FontFamily,
                fontSize,
                FontStyle.Bold);

            TextRenderer.DrawText(
                g,
                _letter,
                font,
                ClientRectangle,
                c.Accent,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine);
        }
    }

    // The cover image belongs to the preview and must be released.
    private sealed class LookupButton : Control
    {
        private bool _hovered;
        private bool _pressed;
        private bool _busy;

        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public bool IsBusy
        {
            get => _busy;
            set
            {
                _busy = value;
                Invalidate();
            }
        }

        public LookupButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e)
        { base.OnMouseLeave(e); _hovered = false; _pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e)
        { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e)
        { base.OnMouseUp(e); _pressed = false; Invalidate(); }

        public void RefreshTheme()
        {
            BackColor = Theme.Colors.SurfaceAlt;
            ForeColor = Theme.Colors.TextMuted;
            Font = Theme.BodyStrong;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var c = Theme.Colors;
            var g = e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            g.Clear(_busy || _pressed ? c.AccentSoft :
                _hovered && Enabled ? c.AccentSoft : c.SurfaceAlt);

            using var border = new Pen(_hovered || _pressed ? c.Accent : c.Border);

            g.DrawRectangle(
                border,
                0, 0,
                Math.Max(0, Width - 1),
                Math.Max(0, Height - 1));

            string caption = _busy ? "Looking up..." : "Lookup ISBN";

            Size textSize = TextRenderer.MeasureText(
                g, caption, Font);

            int startX = Math.Max(
                8,
                (Width - textSize.Width - 27) / 2);

            int centerY = Height / 2;

            using var pen = new Pen(
                _hovered || _busy ? c.Accent : c.TextMuted, 1.8f);

            g.DrawEllipse(
                pen,
                startX,
                centerY - 8,
                11,
                11);

            g.DrawLine(
                pen,
                startX + 10,
                centerY + 2,
                startX + 17,
                centerY + 9);

            TextRenderer.DrawText(
                g,
                caption,
                Font,
                new Rectangle(
                    startX + 27,
                    0,
                    Math.Max(1, Width - startX - 30),
                    Height),
                _hovered || _busy ? c.Text : c.TextMuted,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter);
        }
    }

    private sealed class NumberField : Control
    {
        private readonly TextBox _input = new()
        {
            BorderStyle = BorderStyle.None,
            Text = "0"
        };

        private readonly Button _up = new()
        {
            Text = "▲",
            FlatStyle = FlatStyle.Flat,
            TabStop = false
        };

        private readonly Button _down = new()
        {
            Text = "▼",
            FlatStyle = FlatStyle.Flat,
            TabStop = false
        };

        private int _lastValidValue;
        private int _minimum;
        private int _maximum = 100000;
        private bool _normalizing;

        public event EventHandler? ValueChanged;

        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public int Minimum
        {
            get => _minimum;
            set
            {
                _minimum = Math.Clamp(value, 0, 100000);
                if (_maximum < _minimum)
                    _maximum = _minimum;
                Value = Value;
            }
        }

        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public int Maximum
        {
            get => _maximum;
            set
            {
                _maximum = Math.Clamp(value, 0, 100000);
                if (_minimum > _maximum)
                    _minimum = _maximum;
                Value = Value;
            }
        }

        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public int Value
        {
            get => _lastValidValue;
            set
            {
                int normalized = Math.Clamp(
                    value, _minimum, _maximum);
                bool changed = _lastValidValue != normalized;
                _lastValidValue = normalized;
                string text = normalized.ToString();
                if (_input.Text != text)
                {
                    _normalizing = true;
                    try { _input.Text = text; }
                    finally { _normalizing = false; }
                }
                if (changed)
                    ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public NumberField()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            Controls.AddRange(new Control[]
            {
                _input, _up, _down
            });

            _up.Click += (_, _) =>
                Value = Math.Min(_maximum, Value + 1);

            _down.Click += (_, _) =>
                Value = Math.Max(_minimum, Value - 1);

            _input.KeyPress += (_, e) =>
            {
                if (!char.IsControl(e.KeyChar) &&
                    !char.IsDigit(e.KeyChar))
                    e.Handled = true;
            };

            _input.TextChanged += (_, _) =>
            {
                if (_normalizing)
                    return;
                if (int.TryParse(_input.Text, out int number))
                {
                    // Allow intermediate typing, but never expose an
                    // out-of-range value to the editor.
                    int normalized = Math.Clamp(
                        number, _minimum, _maximum);
                    if (number > _maximum)
                    {
                        Value = normalized;
                        return;
                    }
                    if (normalized != _lastValidValue)
                    {
                        _lastValidValue = normalized;
                        ValueChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            };

            _input.Leave += (_, _) => Value = Value;
            Resize += (_, _) => LayoutParts();
        }

        private void LayoutParts()
        {
            const int buttonWidth = 24;

            int half = Math.Max(1, Height / 2);

            _input.SetBounds(
                12,
                Math.Max(
                    0,
                    (Height - _input.PreferredHeight) / 2),
                Math.Max(
                    1,
                    Width - buttonWidth - 24),
                _input.PreferredHeight);

            _up.SetBounds(
                Math.Max(0, Width - buttonWidth),
                0,
                buttonWidth,
                half);

            _down.SetBounds(
                Math.Max(0, Width - buttonWidth),
                half,
                buttonWidth,
                Math.Max(1, Height - half));
        }

        public void RefreshTheme()
        {
            var c = Theme.Colors;

            BackColor = c.Surface;

            _input.BackColor = c.Surface;
            _input.ForeColor = c.Text;
            _input.Font = Theme.Body;

            foreach (var button in new[]
                     {
                         _up, _down
                     })
            {
                button.BackColor = c.SurfaceAlt;
                button.ForeColor = c.Text;
                button.Font = new Font(
                    Theme.Body.FontFamily,
                    7);

                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor =
                    c.AccentSoft;
            }

            LayoutParts();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using var pen = new Pen(
                Theme.Colors.Border);

            e.Graphics.DrawRectangle(
                pen,
                0, 0,
                Math.Max(0, Width - 1),
                Math.Max(0, Height - 1));
        }
    }

    private sealed class EditorScrollBar : Control
    {
        private int _contentHeight = 1;
        private int _viewportHeight = 1;
        private int _offset;

        private bool _dragging;
        private int _dragStartY;
        private int _dragStartOffset;

        public event Action<int>? OffsetRequested;

        public EditorScrollBar()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            Cursor = Cursors.Hand;
        }

        public void RefreshTheme()
        {
            BackColor = Theme.Colors.Surface;
            Invalidate();
        }

        public void SetMetrics(
            int contentHeight,
            int viewportHeight,
            int offset)
        {
            _contentHeight = Math.Max(
                1, contentHeight);

            _viewportHeight = Math.Max(
                1, viewportHeight);

            _offset = Math.Max(0, offset);

            Invalidate();
        }

        private Rectangle ThumbBounds()
        {
            int trackHeight = Math.Max(
                1, Height);

            int thumbHeight = Math.Clamp(
                (int)Math.Round(
                    trackHeight *
                    Math.Min(
                        1.0,
                        (double)_viewportHeight /
                        _contentHeight)),
                Math.Min(28, trackHeight),
                trackHeight);

            int travel = trackHeight - thumbHeight;

            int maxOffset = Math.Max(
                1,
                _contentHeight - _viewportHeight);

            int thumbY = (int)Math.Round(
                (double)_offset /
                maxOffset * travel);

            return new Rectangle(
                2,
                thumbY,
                Math.Max(1, Width - 4),
                thumbHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(
                Theme.Colors.Surface);

            using var brush = new SolidBrush(
                Theme.Colors.Border);

            e.Graphics.FillRectangle(
                brush,
                ThumbBounds());
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left)
                return;

            Rectangle thumb = ThumbBounds();

            if (!thumb.Contains(e.Location))
            {
                OffsetRequested?.Invoke(
                    _offset +
                    (e.Y < thumb.Top ? -1 : 1) *
                    _viewportHeight);

                return;
            }

            _dragging = true;
            _dragStartY = e.Y;
            _dragStartOffset = _offset;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!_dragging)
                return;

            Rectangle thumb = ThumbBounds();

            int travel = Math.Max(
                1,
                Height - thumb.Height);

            int maxOffset = Math.Max(
                0,
                _contentHeight - _viewportHeight);

            int delta = (int)Math.Round(
                (double)(e.Y - _dragStartY) *
                maxOffset / travel);

            OffsetRequested?.Invoke(
                _dragStartOffset + delta);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            _dragging = false;
            Capture = false;
        }
    }

    private sealed class BackButton : Control
    {
        private bool _hovered;

        public BackButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true);

            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleName = "Back to Books";
        }

        public void RefreshTheme()
        {
            BackColor = Theme.Colors.Surface;
            ForeColor = Theme.Colors.Text;
            Font = Theme.Body;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode is Keys.Enter or Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var c = Theme.Colors;
            var g = e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            using var background = new SolidBrush(
                _hovered ? c.SurfaceAlt : c.Surface);

            g.FillRectangle(
                background, ClientRectangle);

            using var border = new Pen(c.Border);

            g.DrawRectangle(
                border,
                0, 0,
                Math.Max(0, Width - 1),
                Math.Max(0, Height - 1));

            int iconX = 18;
            int iconY = Height / 2;

            using var pen = new Pen(
                c.Accent, 2.2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            g.DrawLines(
                pen,
                new[]
                {
                    new Point(iconX + 4, iconY - 6),
                    new Point(iconX - 2, iconY),
                    new Point(iconX + 4, iconY + 6)
                });

            TextRenderer.DrawText(
                g,
                "Back",
                Font,
                new Rectangle(
                    31,
                    0,
                    Math.Max(0, Width - 34),
                    Height),
                c.Text,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter);

            if (Focused && ShowFocusCues)
            {
                ControlPaint.DrawFocusRectangle(
                    g,
                    new Rectangle(
                        3,
                        3,
                        Math.Max(0, Width - 6),
                        Math.Max(0, Height - 6)));
            }
        }
    }
}

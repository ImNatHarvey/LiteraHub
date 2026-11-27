
using System;
using FontAwesome.Sharp;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Security;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

public sealed class BookEditorPage : ThemedUserControl
{
    private readonly Book? _existing;

    private readonly TableLayoutPanel _layout = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 4,
        Margin = Padding.Empty
    };

    // Shared header owns title, live subtitle/status messages and the Back button.
    private readonly DetailsPageHeader _detailsHeader = new();

    private readonly Panel _viewport = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = false,
        Margin = Padding.Empty
    };

    private readonly DetailsActionFooter _actionFooter = new()
    {
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
        Padding = Padding.Empty,
        UseBookSizing = true
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

    private readonly ThemedAuthorPicker _authors = new();
    private const string UnknownAuthorName = "Unknown Author";

    // Searchable single-category selection; reuses the existing CategoryDialog.
    private readonly ThemedCategoryPicker _category = new();

    private ThemedFilterDropdown _condition =
        new(new[] { "Good", "Damaged" })
        {
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 8, 0)
        };

    // F4.2: reusable validation keeps the accepted compact layout unchanged.
    private readonly CatalogValidationController _validation = new();
    private bool _validationReady;
    private bool _validationAttempted;

    private readonly NumberField _total = new();
    private readonly NumberField _available = new();

    private readonly LookupButton _lookup = new() { Enabled = false };

    private readonly Button _save = new()
    {
        Text = "Save Book",
        FlatStyle = FlatStyle.Flat,
        Enabled = false
    };

    private readonly Button _archive = new()
    {
        Text = "Archive Book",
        FlatStyle = FlatStyle.Flat,
        Visible = false
    };

    private readonly Button _delete = new()
    {
        Text = "Delete Book",
        FlatStyle = FlatStyle.Flat,
        Visible = false
    };

    // Existing books open in read-only View mode; new books open in Edit mode.
    private bool _editing;
    private readonly Dictionary<string, Label> _viewValues = new();
    private readonly Button _cancelEdit = new()
    {
        Text = "Cancel",
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        Visible = false
    };
    private bool _saving;
    private bool _canHardDelete;
    private IPermissionService? _permissions;
    private bool _statusesLoaded;
    private int[] _statusIds = Array.Empty<int>();
    private string? _coverPath;
    public event EventHandler? Saved;

    private ThemedSectionCard _bookCard = null!;
    private ThemedSectionCard _classificationCard = null!;
    private ThemedSectionCard _inventoryCard = null!;
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
    private IBookService? _bookService;
    private IAuthorCreationService? _authorCreationService;
    private bool _authorsLoading;
    private bool _authorsLoaded;
    // Cached lookup entries let Cancel restore the persisted author selection,
    // including clearing any staged/unsaved authors in the existing picker.
    private IReadOnlyList<BookLookupOption> _loadedAuthorOptions = Array.Empty<BookLookupOption>();
    private IReadOnlyList<string> _pendingMetadataAuthors = Array.Empty<string>();

    public IReadOnlyList<int> SelectedAuthorIds => _authors.SelectedAuthorIds;
    private bool _lookupBusy;
    private CancellationTokenSource? _coverLoad;
    private static readonly HttpClient CoverHttp = new() { Timeout = TimeSpan.FromSeconds(12) };
    private bool _categoriesLoading;

    // Category IDs are loaded from dbo.Categories. Zero means no selection.
    public int SelectedCategoryId => _category.SelectedCategoryId;

    public void ConfigureServices(
        ICategoryService categories,
        IBookMetadataService metadata,
        IBookService books,
        bool canHardDelete,
        IAuthorCreationService authorCreation,
        bool canCreateAuthors, IPermissionService permissions)
    {
        _permissions = permissions;
        _canHardDelete = canHardDelete;
        // ConfigureServices runs after the constructor populated the editor.
        // Keep Delete visible on existing books; permission controls whether it is enabled.
        _delete.Visible = _existing is not null;
        _delete.Enabled = _canHardDelete;
        _delete.Text = "";
        _delete.AccessibleName = "Delete Book";
        _categoryService = categories ??
            throw new ArgumentNullException(nameof(categories));
        _metadataService = metadata ??
            throw new ArgumentNullException(nameof(metadata));
        _bookService = books ??
            throw new ArgumentNullException(nameof(books));
        _authorCreationService = authorCreation ??
            throw new ArgumentNullException(nameof(authorCreation));

        _authors.AllowCreateAuthor = canCreateAuthors;
        _authors.AddAuthorRequested -= HandleAddAuthorRequested;
        _authors.AddAuthorRequested += HandleAddAuthorRequested;
        // BooksPage passes Permission.CreateLookups for this parameter.
        // It governs both Add Author and Add Category.
        _category.AllowCreateCategory = permissions.Can(RolePermission.CategoriesAdd);
        _category.AddCategoryRequested -= HandleAddCategoryRequested;
        _category.AddCategoryRequested += HandleAddCategoryRequested;
        _lookup.Enabled = _editing;

        RefreshEditorMode();
        if (IsHandleCreated)
        {
            _ = LoadCategoriesAsync();
            _ = LoadAuthorsAsync();
            _ = LoadStatusesAsync();
        }
        else
        {
            HandleCreated += HandleEditorCreated;
        }
    }

    private void HandleEditorCreated(object? sender, EventArgs e)
    {
        HandleCreated -= HandleEditorCreated;
        _ = LoadCategoriesAsync();
        _ = LoadAuthorsAsync();
        _ = LoadStatusesAsync();
    }

    private async Task LoadStatusesAsync()
    {
        if (_bookService is null || _statusesLoaded)
            return;

        try
        {
            var statuses = await _bookService.GetStatusesAsync();
            if (IsDisposed || Disposing) return;

            var options = statuses.Where(x => x.IsActive)
                .OrderBy(x => x.Name).ToArray();

            _statusIds = options.Select(x => x.Id).ToArray();
            if (options.Length == 0)
            {
                _detailsHeader.SubtitleText = "No active book statuses are available.";
                return;
            }
            var previous = _condition;
            var replacement = new ThemedFilterDropdown(
                options.Select(x => x.Name).ToArray())
            {
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 8, 0)
            };
            _condition = replacement;
            _classificationCard.Controls.Add(replacement);
            replacement.MouseWheel += HandleWheel;
            previous.Dispose();
            replacement.ApplyTheme();
            replacement.SelectedIndexChanged += (_, _) => RefreshValidationCaptions();
            replacement.Enabled = _editing;
            _statusesLoaded = true;
            // Default only new books to a Good condition; never override edits.
            if (_existing is null)
            {
                int goodIndex = Array.FindIndex(options,
                    option => option.Name.StartsWith("Good",
                        StringComparison.OrdinalIgnoreCase));
                if (goodIndex >= 0)
                    _condition.SelectedIndex = goodIndex;
            }
            ArrangeForm();

            if (_existing is not null)
            {
                int index = Array.IndexOf(_statusIds, _existing.StatusId);
                if (index >= 0) _condition.SelectedIndex = index;
            }
            _save.Enabled = !_editing || (_authorsLoaded && _category.HasCategories);
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                _detailsHeader.SubtitleText = "Unable to load book statuses: " + ex.Message;
        }
    }

    private async void HandleAddAuthorRequested(
        object? sender, EventArgs e)
    {
        try
        {
            await OpenAddAuthorAsync();
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                ThemedMessageDialog.ShowAlert(
                    FindForm(), "Add Author", ex.Message, danger: true);
        }
    }

    private async Task OpenAddAuthorAsync()
    {
        if (_saving || _authorCreationService is null ||
            !_authors.AllowCreateAuthor || _authorsLoading)
            return;

        _authors.Enabled = false;
        try
        {
            using var dialog = new AddAuthorDialog(
                _authorCreationService,
                suggestedName: _authors.RequestedAuthorName);

            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            if (dialog.CreatedAuthorId is int authorId)
                await LoadAuthorsAsync(authorId);
            else
            {
                await LoadAuthorsAsync();
                _detailsHeader.SubtitleText = "Author saved but its ID was not returned. Reopen author selection.";
            }
        }
        finally
        {
            if (!IsDisposed && !Disposing)
                _authors.Enabled = true;
        }
    }

    private async Task LoadAuthorsAsync(int? selectAuthorId = null)
    {
        if (_authorsLoading || _bookService is null)
            return;

        _authorsLoading = true;
        try
        {
            var records = await _bookService.GetAuthorsAsync();
            if (IsDisposed || Disposing)
                return;

            _loadedAuthorOptions = records;
            var selected = _authors.SelectedAuthorIds.ToHashSet();
            if (!_authorsLoaded && _existing is not null)
                selected.UnionWith(_existing.AuthorIds);
            if (selectAuthorId.HasValue)
                selected.Add(selectAuthorId.Value);

            _authors.SetAuthors(records, selected);
            _authorsLoaded = true;
            _save.Enabled = !_editing || (_statusesLoaded &&
                _category.HasCategories && !_saving);

            if (_pendingMetadataAuthors.Count > 0)
            {
                // No author field in ISBN metadata: select the shared fallback
                // only after the real author list has finished loading.
                bool unknownFallback = _pendingMetadataAuthors.Count == 1 &&
                    string.Equals(_pendingMetadataAuthors[0], UnknownAuthorName,
                        StringComparison.OrdinalIgnoreCase);
                var names = _pendingMetadataAuthors;
                _pendingMetadataAuthors = Array.Empty<string>();
                if (unknownFallback)
                    SelectUnknownAuthorFallback();
                else
                {
                    var unmatched = _authors.MatchMetadataNames(names);
                    if (unmatched.Count > 0)
                        await CreateMissingMetadataAuthorsAsync(unmatched);
                    else
                        _detailsHeader.SubtitleText = _authors.SelectedAuthorIds.Count > 0
                            ? "ISBN authors matched to existing records."
                            : "Metadata found, but no author was matched or created. Select an author manually.";
                }
            }

            if (selectAuthorId.HasValue)
            {
                _detailsHeader.SubtitleText =
                    _authors.SelectedAuthorIds.Contains(selectAuthorId.Value)
                    ? "Author created and selected. Continue editing the book."
                    : "Author saved but could not be selected. Verify that it is active.";
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                _detailsHeader.SubtitleText = "Unable to load authors: " + ex.Message;
        }
        finally
        {
            _authorsLoading = false;
        }
    }

    private async void HandleAddCategoryRequested(object? sender, EventArgs e)
    {
        try
        {
            await OpenAddCategoryAsync();
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                ThemedMessageDialog.ShowAlert(
                    FindForm(), "Add Category", ex.Message, danger: true);
        }
    }

    private async Task OpenAddCategoryAsync()
    {
        if (_saving || !_editing || _categoriesLoading ||
            _categoryService is null || !_category.AllowCreateCategory)
            return;

        _category.Enabled = false;
        try
        {
            using var dialog = new CategoryDialog(
                _categoryService,
                suggestedName: _category.RequestedCategoryName);

            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            // The category is created by CategoryDialog/CategoryService.
            // Reload persisted IDs and select the new category by its saved name.
            await LoadCategoriesAsync(selectName: dialog.SavedCategoryName);
            if (!IsDisposed && !Disposing)
                _detailsHeader.SubtitleText = SelectedCategoryId > 0
                    ? "Category created and selected."
                    : "Category saved. Choose it from the category selector.";
        }
        finally
        {
            if (!IsDisposed && !Disposing)
                _category.Enabled = _editing;
        }
    }

    private async Task LoadCategoriesAsync(string? selectName = null)
    {
        if (_categoriesLoading || _categoryService is null)
            return;

        _categoriesLoading = true;
        try
        {
            var items = await _categoryService.GetBookLookupAsync();
            if (IsDisposed || Disposing)
                return;

            var active = items.Where(c => c.IsActive)
                .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();

            int requested = _category.SelectedCategoryId;
            if (!string.IsNullOrWhiteSpace(selectName))
            {
                var newlyCreated = active.Where(c =>
                    string.Equals(c.Name.Trim(), selectName.Trim(),
                        StringComparison.OrdinalIgnoreCase)).ToArray();
                // Do not guess if duplicate data has somehow been introduced.
                requested = newlyCreated.Length == 1
                    ? newlyCreated[0].CategoryId : 0;
            }
            else if (requested == 0 && _existing is not null)
                requested = _existing.CategoryId;

            _category.SetCategories(active, requested);
            _category.ApplyTheme();
            _category.Enabled = _editing;
            _save.Enabled = !_editing || (_authorsLoaded &&
                _statusesLoaded && _category.HasCategories && !_saving);
            ArrangeForm();
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                _detailsHeader.SubtitleText = "Unable to load categories: " + ex.Message;
        }
        finally
        {
            _categoriesLoading = false;
        }
    }

    private Task CreateMissingMetadataAuthorsAsync(
        IReadOnlyList<string> missingNames)
    {
        if (missingNames.Count == 0)
        {
            _detailsHeader.SubtitleText = _authors.SelectedAuthorIds.Count > 0
                ? "Metadata found. Existing authors matched and selected."
                : "Metadata found, but no author was matched. Select an author manually.";
            return Task.CompletedTask;
        }

        if (!_authors.AllowCreateAuthor)
        {
            _detailsHeader.SubtitleText = "Author creation is unavailable. Select manually: " +
                string.Join(", ", missingNames);
            return Task.CompletedTask;
        }

        var unresolved = new System.Collections.Generic.List<string>();
        foreach (string rawName in missingNames)
        {
            string name = rawName.Trim();
            string[] parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 ||
                string.Join(" ", parts, 0, Math.Max(0, parts.Length - 1)).Length > 100 ||
                parts[^1].Length > 100)
            {
                unresolved.Add(name);
                continue;
            }
            _authors.StageAuthor(name);
        }

        _detailsHeader.SubtitleText = unresolved.Count > 0
            ? "Author(s) require manual selection: " + string.Join(", ", unresolved)
            : "Metadata found. New authors are pending and will only be created when you save the book.";
        return Task.CompletedTask;
    }

    // ISBN results may omit authors entirely. Do not create an author until
    // the book itself is saved; the existing repository resolves staged names
    // transactionally and reuses an active author with the same display name.
    private void SelectUnknownAuthorFallback()
    {
        if (_authors.SelectedAuthorIds.Count > 0 ||
            _authors.SelectedPendingAuthorNames.Count > 0)
        {
            _detailsHeader.SubtitleText = "No author metadata provided. Your selection was kept.";
            return;
        }

        var matches = _loadedAuthorOptions
            .Where(a => a.IsActive && string.Equals(a.Name.Trim(),
                UnknownAuthorName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length == 1)
        {
            _authors.SelectAuthor(matches[0].Id);
            _detailsHeader.SubtitleText = "No author metadata provided. Unknown Author was selected.";
        }
        else if (matches.Length > 1)
        {
            _detailsHeader.SubtitleText = "Multiple Unknown Author records exist. Select the correct one manually.";
        }
        else if (_authors.AllowCreateAuthor)
        {
            _authors.StageAuthor(UnknownAuthorName);
            _detailsHeader.SubtitleText = "No author metadata provided. Unknown Author is selected and will be created only when you save.";
        }
        else
        {
            _detailsHeader.SubtitleText = "No author metadata provided and author creation is restricted. Select an existing author.";
        }
    }

    private async Task LookupIsbnAsync()
    {
        if (!_editing || _lookupBusy || _metadataService is null)
            return;
        string isbn = _isbn.Text.Trim();
        if (isbn.Length == 0)
        {
            _detailsHeader.SubtitleText = "Enter an ISBN before searching.";
            return;
        }

        _lookupBusy = true;
        _lookup.IsBusy = true;
        _lookup.Enabled = false;
        _detailsHeader.SubtitleText = "Looking up ISBN...";
        try
        {
            var result = await _metadataService.LookupByIsbnAsync(isbn);
            if (IsDisposed || Disposing)
                return;
            if (!result.Success || result.Metadata is null)
            {
                _detailsHeader.SubtitleText = result.Error ?? "ISBN not found.";
                return;
            }
            var book = result.Metadata;
            _title.Text = book.Title;
            _bookDescription.Text = book.Description ?? string.Empty;
            _coverPath = book.CoverUrl;
            _ = LoadCoverAsync(book.CoverUrl);
            if (book.Authors.Count == 0)
            {
                if (_authorsLoaded)
                    SelectUnknownAuthorFallback();
                else if (_authors.SelectedAuthorIds.Count == 0 &&
                         _authors.SelectedPendingAuthorNames.Count == 0)
                {
                    _pendingMetadataAuthors = new[] { UnknownAuthorName };
                    _detailsHeader.SubtitleText = "No author metadata provided. Unknown Author will be selected after authors load.";
                }
                else
                    _detailsHeader.SubtitleText = "No author metadata provided. Your selection was kept.";
            }
            else if (_authorsLoaded)
            {
                var unmatched = _authors.MatchMetadataNames(book.Authors);
                await CreateMissingMetadataAuthorsAsync(unmatched);
            }
            else
            {
                _pendingMetadataAuthors = book.Authors.ToArray();
                _detailsHeader.SubtitleText = "Metadata found. Author matching will run after loading.";
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                _detailsHeader.SubtitleText = "ISBN lookup failed: " + ex.Message;
        }
        finally
        {
            _lookupBusy = false;
            _lookup.IsBusy = false;
            if (!IsDisposed && !Disposing)
                _lookup.Enabled = _editing && _metadataService is not null;
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
        _editing = existing is null;

        Dock = DockStyle.Fill;
        Padding = new Padding(Space.L, Space.L, 0, Space.L);
        _detailsHeader.AlignBackToRightEdge = true;

        _detailsHeader.HeadingText = existing is null ? "Add Book" : "View Book";

        _detailsHeader.SubtitleText = existing is null
            ? "Create a new book record."
            : $"Viewing: {existing.Title}";

        _bookSection = MakeLabel("Book Information", true);
        _classificationSection =
            MakeLabel("Classification & Authors", true);
        _inventorySection = MakeLabel("Inventory & Description", true);

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
        if (existing is not null)
            CreateViewValues();
        WireEvents();
        RefreshEditorMode();
        ApplyTheme();
    }

    private static Label MakeLabel(
        string text,
        bool section = false)
    {
        return new Label
        {
            Text = text,
            UseMnemonic = false,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = section ? "section" : "field"
        };
    }

    private void BuildLayout()
    {
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Space.L));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 56));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 42));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 60));


        _bookCard = new ThemedSectionCard(_bookSection, IconChar.Book);
        _classificationCard = new ThemedSectionCard(_classificationSection, IconChar.Tags);
        _inventoryCard = new ThemedSectionCard(_inventorySection, IconChar.BookOpen);
        _bookCard.AddFields(_coverLabel, _cover, _isbnLabel, _isbn, _lookup, _titleLabel, _title);
        _classificationCard.AddFields(_categoryLabel, _category, _conditionLabel, _condition, _authorsLabel, _authors);
        _inventoryCard.AddFields(_totalLabel, _total, _availableLabel, _available, _descriptionLabel, _bookDescription);
        _content.Controls.AddRange(new Control[] { _bookCard, _classificationCard, _inventoryCard });
        _actionFooter.Controls.AddRange(new Control[]
        {
            _delete, _archive, _save, _cancelEdit
        });
        _actionFooter.Configure(_delete, _archive, _save, _cancelEdit,
            () => _existing is null,
            () => _existing is not null && _editing);

        _viewport.Controls.Add(_content);
        _viewport.Controls.Add(_scrollbar);

        _layout.Controls.Add(_detailsHeader, 0, 0);
        _layout.SetRowSpan(_detailsHeader, 2);
        _layout.Controls.Add(_viewport, 0, 2);
        _layout.SetColumnSpan(_viewport, 2);
        _layout.Controls.Add(_actionFooter, 0, 3);

        Controls.Add(_layout);

    }

    private void WireEvents()
    {
        _detailsHeader.BackRequested += (_, _) =>
        {
            if (_saving) return;
            // Match Student Details: Back returns directly to the list.
            // Cancel below restores the saved record without leaving this page.
            BackRequested?.Invoke(this, EventArgs.Empty);
        };

        _lookup.Click += async (_, _) => await LookupIsbnAsync();
        _save.Click += async (_, _) =>
        {
            if (_saving) return;
            if (!_editing)
            {
                _editing = true;
                RefreshEditorMode();
                return;
            }
            await SaveAsync();
        };
        _cancelEdit.Click += (_, _) =>
        {
            if (_saving || _existing is null) return;
            // Restore every saved field, not just the fields visible in View mode.
            // No validation, confirmation, or database write on Cancel.
            ClearBookValidation();
            _editing = false;
            _pendingMetadataAuthors = Array.Empty<string>();
            PopulateExistingBook();
            _category.SelectCategory(_existing.CategoryId);
            if (_statusIds.Length > 0)
            {
                int condition = Array.IndexOf(_statusIds, _existing.StatusId);
                if (condition >= 0) _condition.SelectedIndex = condition;
            }
            if (_authorsLoaded)
                _authors.RestoreSavedSelection(_loadedAuthorOptions, _existing.AuthorIds);
            RefreshEditorMode();
        };
        _archive.Click += async (_, _) => await ChangeActiveAsync();
        _delete.Click += async (_, _) => await DeleteAsync();
        _save.Text = "";
        _save.AccessibleName = _editing ? "Save Book" : "Edit Book";
        _save.Paint += (_, e) => PaintActionIcon(e, _save, 0);
        _archive.Paint += (_, e) => PaintActionIcon(e, _archive, 1);
        _delete.Paint += (_, e) => PaintActionIcon(e, _delete, 2);

        _title.TextChanged += (_, _) =>
        {
            string title = _title.Text.Trim();

            _cover.Letter = title.Length == 0
                ? "?"
                : title[..1].ToUpperInvariant();
        };

        _title.TextChanged += (_, _) => RefreshValidationCaptions();
        _category.SelectionChanged += (_, _) => RefreshValidationCaptions();
        _authors.SelectionChanged += (_, _) => RefreshValidationCaptions();
        _total.ValueChanged += (_, _) => RefreshValidationCaptions();
        _available.ValueChanged += (_, _) => RefreshValidationCaptions();
        _total.ValueChanged += (_, _) => SyncInventory();
        _available.ValueChanged += (_, _) => SyncInventory();

        Resize += (_, _) => ArrangeForm();
        _viewport.Resize += (_, _) => ArrangeForm();
        _actionFooter.Resize += (_, _) => PositionFooterActions();

        _scrollbar.OffsetRequested += SetScrollOffset;

        _viewport.MouseWheel += HandleWheel;
        _content.MouseWheel += HandleWheel;

        foreach (Control card in _content.Controls)
        {
            card.MouseWheel += HandleWheel;
            foreach (Control control in card.Controls) control.MouseWheel += HandleWheel;
        }
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
        _coverPath = _existing.CoverPath;
        _ = LoadCoverAsync(_coverPath);
        _archive.Visible = true;
        _archive.Text = "";
        _archive.AccessibleName = _existing.IsActive ? "Archive Book" : "Restore Book";
        _delete.Visible = true;
        _delete.Enabled = _canHardDelete;

        // Author IDs are restored once the database author list loads.

        // Do not infer active loans from unavailable inventory.
        // The repository validates real outstanding loans on save.
        _reservedCopies = 0;
        _total.Value = _existing.TotalCopies;
        _available.Value = _existing.AvailableCopies;
        SyncInventory();

        // Existing category is displayed as read-only text until
        // category lookup is connected in the next checkpoint.
        // The themed category dropdown currently has one option.
        // The actual StatusId is selected after statuses load.

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
            // In portrait edit mode, give Cancel its own row above the three
            // existing actions. Never overlap Delete, Archive and Save.
            bool stackedActions = _actionFooter.ClientSize.Width > 0
                ? _actionFooter.ClientSize.Width < 700 : portrait;
            float footerHeight = stackedActions && _existing is not null && _editing
                ? 104 : 60;
            if (Math.Abs(_layout.RowStyles[3].Height - footerHeight) > 0.5f)
                _layout.RowStyles[3].Height = footerHeight;

            _detailsHeader.CompactMode = portrait;

            // Header, cards and footer share the first column. The scrollbar lives
            // in the existing outer page gutter and never reduces card width.
            int gutter = Math.Max(_scrollbar.Width, (int)_layout.ColumnStyles[1].Width);
            int width = Math.Max(1, Math.Min(_actionFooter.ClientSize.Width,
                _viewport.ClientSize.Width - gutter));
            int height = ArrangeCards(width);
            _scrollbar.Visible = height > _viewport.ClientSize.Height;

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
            PositionViewValues();
        }
        finally
        {
            _arranging = false;
        }
    }

    private int ArrangeCards(int width)
    {
        var layout = new ResponsiveSectionLayout(width);
        int pad = layout.Padding;
        int bookWidth = layout.WidthAt(0, 3);
        int classificationWidth = layout.WidthAt(1, 3);
        int inventoryWidth = layout.WidthAt(2, 3);
        int w = Math.Max(1, bookWidth - 2 * pad);
        int viewport = _viewport.ClientSize.Height;
        int fieldGap = 7, labelGap = 5;
        int a = _bookCard.ArrangeHeading(bookWidth, pad);
        int b = _classificationCard.ArrangeHeading(classificationWidth, pad);
        int c = _inventoryCard.ArrangeHeading(inventoryWidth, pad);

        _coverLabel.SetBounds(pad, a, w, 19);
        a += 19 + labelGap;
        int coverHeight = Math.Clamp(viewport - 255 - (_editing ? 40 : 0), 95, 235);
        int coverWidth = Math.Min(w, Math.Max(1, (int)Math.Round(coverHeight * .72)));
        _cover.SetBounds(pad + (w - coverWidth) / 2, a, coverWidth, coverHeight);
        a += coverHeight + fieldGap;
        Field(_isbnLabel, _isbn, pad, ref a, w, 36, labelGap, fieldGap);
        if (_editing)
        {
            _lookup.SetBounds(pad, a, w, 34);
            a += 34 + fieldGap;
        }
        else _lookup.Bounds = Rectangle.Empty;
        Field(_titleLabel, _title, pad, ref a, w, 40, labelGap, fieldGap);

        w = Math.Max(1, classificationWidth - 2 * pad);
        Field(_categoryLabel, _category, pad, ref b, w, 40, labelGap, fieldGap);
        Field(_conditionLabel, _condition, pad, ref b, w, 40, labelGap, fieldGap);
        Field(_authorsLabel, _authors, pad, ref b, w, ViewAuthorHeight(w), labelGap, fieldGap);

        w = Math.Max(1, inventoryWidth - 2 * pad);
        // Keep the numeric captions readable in compact columns.
        if (w >= 250)
        {
            int numberWidth = (w - 12) / 2;
            int otherY = c;
            Field(_totalLabel, _total, pad, ref c, numberWidth, 40, labelGap, fieldGap);
            Field(_availableLabel, _available, pad + numberWidth + 12, ref otherY, w - numberWidth - 12, 40, labelGap, fieldGap);
            c = Math.Max(c, otherY);
        }
        else
        {
            Field(_totalLabel, _total, pad, ref c, w, 40, labelGap, fieldGap);
            Field(_availableLabel, _available, pad, ref c, w, 40, labelGap, fieldGap);
        }
        int descriptionHeight = Math.Max(110, viewport - c - pad - 35);
        if (layout.Columns < 3) descriptionHeight = Math.Clamp(descriptionHeight, 110, 280);
        if (_existing is not null && !_editing)
        {
            var measured = TextRenderer.MeasureText(_existing.Description ?? "—", Theme.BodyStrong,
                new Size(Math.Max(1, w - 26), 10000), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            descriptionHeight = Math.Max(descriptionHeight, measured.Height + 16);
        }
        Field(_descriptionLabel, _bookDescription, pad, ref c, w, descriptionHeight, labelGap, fieldGap);
        PositionFooterActions();
        return layout.Place(new[] { _bookCard, _classificationCard, _inventoryCard },
            new[] { a + pad, b + pad, c + pad }, viewport);
    }

    private void PositionFooterActions()
    {
        _actionFooter.ArrangeActions();
    }

    private void PaintActionIcon(PaintEventArgs e, Button button, int kind)
    {
        bool restoring = _existing?.IsActive == false;
        string label = kind == 0
            ? (_editing ? "Save Book" : "Edit Book")
            : kind == 2 ? "Delete Book"
            : restoring ? "Restore Book" : "Archive Book";
        IconButtonContent.DrawAction(e, button, kind, label, restoring);
    }

    private void CreateViewValues()
    {
        if (_existing is null) return;
        foreach (string key in new[]
        {
            "ISBN", "Title", "Description", "Authors", "Category",
            "Condition", "Total", "Available"
        })
        {
            var label = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 3, 12, 3),
                UseMnemonic = false,
                AutoEllipsis = false,
                BorderStyle = BorderStyle.None
            };
            label.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Colors.Border);
                e.Graphics.DrawRectangle(pen, 0, 0,
                    Math.Max(0, label.Width - 1), Math.Max(0, label.Height - 1));
            };
            _viewValues[key] = label;
            var card = key switch
            {
                "ISBN" or "Title" => _bookCard,
                "Category" or "Condition" or "Authors" => _classificationCard,
                _ => _inventoryCard
            };
            card.Controls.Add(label);
        }
        _viewValues["ISBN"].Text = _existing.Isbn;
        _viewValues["Title"].Text = _existing.Title;
        _viewValues["Description"].TextAlign = ContentAlignment.TopLeft;
        _viewValues["Description"].Text = _existing.Description ?? "—";
        // Book.Authors is the complete joined author list returned by the repository.
        // Never replace this with the picker caption (e.g. '5 authors selected').
        _viewValues["Authors"].TextAlign = ContentAlignment.MiddleLeft;
        _viewValues["Authors"].Text = string.IsNullOrWhiteSpace(_existing.Authors)
            ? "No authors listed" : _existing.Authors;
        _viewValues["Category"].Text = _existing.CategoryName;
        _viewValues["Condition"].Text = _existing.StatusName;
        _viewValues["Total"].Text = _existing.TotalCopies.ToString();
        _viewValues["Available"].Text = _existing.AvailableCopies.ToString();
    }

    private int ViewAuthorHeight(int width)
    {
        if (_existing is null || _editing) return 40;
        var text = _existing.Authors;
        if (string.IsNullOrEmpty(text)) return 40;
        var measured = TextRenderer.MeasureText(text, Theme.BodyStrong,
            new Size(Math.Max(60, width - 26), 10000),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        return Math.Max(40, measured.Height + (_editing ? 18 : 14));
    }

    private void PositionViewValues()
    {
        if (_existing is null) return;
        var mapped = new (string Key, Control Target)[]
        {
            ("ISBN", _isbn), ("Title", _title),
            ("Description", _bookDescription), ("Authors", _authors),
            ("Category", _category), ("Condition", _condition),
            ("Total", _total), ("Available", _available)
        };
        foreach (var (key, target) in mapped)
        {
            Label overlay = _viewValues[key];
            overlay.Bounds = target.Bounds;
            overlay.Visible = !_editing;
            overlay.TabStop = false;
            target.Visible = _editing;
            if (!_editing) overlay.BringToFront();
        }
        _lookup.Visible = _editing;
        _cancelEdit.Visible = _existing is not null && _editing;
    }

    private void RefreshEditorMode()
    {
        _detailsHeader.HeadingText = _existing is null ? "Add Book" : _editing ? "Edit Book" : "View Book";
        _detailsHeader.SubtitleText = _existing is null ? "Create a new book record." :
            (_editing ? "Editing: " : "Viewing: ") + _existing.Title;
        _save.AccessibleName = _editing ? "Save Book" : "Edit Book";
        // The view action is always available. Saving requires loaded lookups.
        _save.Enabled = !_editing || (_authorsLoaded && _statusesLoaded && _category.HasCategories);
        _save.Visible = _permissions?.Can(_existing is null ? RolePermission.BooksAdd : RolePermission.BooksEdit) ?? true;
        _archive.Visible = _existing is not null && (_permissions?.Can(RolePermission.BooksArchiveRestore) ?? false);
        _delete.Visible = _existing is not null && _canHardDelete;
        _cancelEdit.Visible = _existing is not null && _editing;
        _lookup.Enabled = _editing && _metadataService is not null;
        if (_existing is not null)
        {
            _authors.Enabled = _editing;
            _category.Enabled = _editing;
            _condition.Enabled = _editing;
        }
        ArrangeForm();
        _save.Invalidate();
    }

    private void EnsureBookValidation()
    {
        if (_validationReady) return;
        // Conditions are loaded asynchronously and the placeholder control
        // is replaced. Register only after the final dropdown exists.
        _validation.Register(_title,
            () => !string.IsNullOrWhiteSpace(_title.Text),
            "Book title is required.");
        _validation.Register(_category,
            () => SelectedCategoryId > 0,
            "Select a category.");
        _validation.Register(_condition,
            () => _condition.SelectedIndex >= 0 &&
                  _condition.SelectedIndex < _statusIds.Length,
            "Select a valid book condition.");
        _validation.Register(_authors,
            () => _authors.SelectedAuthorIds.Count > 0 ||
                  _authors.SelectedPendingAuthorNames.Count > 0,
            "Select at least one author.");
        _validationReady = true;
    }

    private void RefreshValidationCaptions()
    {
        if (!_validationAttempted || !_validationReady || IsDisposed || Disposing)
            return;
        // Revalidation clears corrected borders immediately. Caption hints
        // occupy the EXISTING 19px label rows and never change field widths.
        _validation.Validate();
        SetValidationCaption(_titleLabel, "Book Title",
            !string.IsNullOrWhiteSpace(_title.Text), "Required");
        SetValidationCaption(_categoryLabel, "Category",
            SelectedCategoryId > 0, "Select a category");
        SetValidationCaption(_conditionLabel, "Condition",
            _condition.SelectedIndex >= 0 && _condition.SelectedIndex < _statusIds.Length,
            "Select a condition");
        SetValidationCaption(_authorsLabel, "Authors",
            _authors.SelectedAuthorIds.Count > 0 ||
            _authors.SelectedPendingAuthorNames.Count > 0, "Select an author");
    }

    private static void SetValidationCaption(Label label, string caption,
        bool valid, string guidance)
    {
        label.Text = valid ? caption : caption + "  •  " + guidance;
        label.ForeColor = valid ? Theme.Colors.Text : Theme.Colors.Danger;
        label.AutoEllipsis = true;
    }

    private void ClearBookValidation()
    {
        _validationAttempted = false;
        _validation.Clear();
        foreach (var (label, caption) in new[]
        {
            (_titleLabel, "Book Title"), (_categoryLabel, "Category"),
            (_conditionLabel, "Condition"), (_authorsLabel, "Authors")
        })
        {
            label.Text = caption;
            label.ForeColor = Theme.Colors.Text;
        }
    }

    private async Task SaveAsync()
    {
        if (!_editing || _saving || _bookService is null || !_authorsLoaded || !_statusesLoaded)
            return;

        EnsureBookValidation();
        _validationAttempted = true;
        var errors = _validation.Validate();
        RefreshValidationCaptions();
        if (errors.Count > 0)
        {
            _validation.ShowSummary(FindForm(), "this book");
            if (!IsDisposed && !Disposing)
                _validation.FocusFirstInvalid();
            return;
        }

        // Numeric controls already clamp invalid input. Keep the existing
        // reserved-copy protection and explicitly verify it before saving.
        if (_total.Value < _reservedCopies || _available.Value < 0 ||
            _available.Value > _total.Value - _reservedCopies)
        {
            ThemedMessageDialog.ShowAlert(FindForm(), "Check Inventory",
                "Available copies cannot exceed total copies minus reserved copies.", danger: true);
            return;
        }

        var input = new BookInput(
            _isbn.Text.Trim(),
            _title.Text.Trim(),
            _bookDescription.Text,
            SelectedCategoryId,
            _statusIds[_condition.SelectedIndex],
            _total.Value,
            _available.Value,
            _coverPath,
            _authors.SelectedAuthorIds,
            _authors.SelectedPendingAuthorNames);

        bool creating = _existing is null;
        _saving = true;
        _save.Enabled = false;
        _archive.Enabled = false;
        _delete.Enabled = false;
        _detailsHeader.SubtitleText = "Saving book...";
        try
        {
            var result = _existing is null
                ? await _bookService.CreateAsync(input)
                : await _bookService.UpdateAsync(_existing.BookId, input);
            if (IsDisposed || Disposing) return;
            if (!result.Success)
            {
                _detailsHeader.SubtitleText = result.Error ?? "Unable to save book.";
                ThemedMessageDialog.ShowAlert(FindForm(),
                    "Book Validation", _detailsHeader.SubtitleText);
                return;
            }
            // BooksPage closes this editor synchronously on Saved.
            // Notify the shell before that navigation disposes this page.
            CatalogActionFeedback.Notify(this,
                creating ? "Book added successfully." : "Book updated successfully.");
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                ThemedMessageDialog.ShowAlert(FindForm(), "Unable to save book", ex.Message, true);
        }
        finally
        {
            _saving = false;
            if (!IsDisposed && !Disposing)
            {
                _save.Enabled = !_editing || (_authorsLoaded && _statusesLoaded && _category.HasCategories);
                _archive.Enabled = true;
                _delete.Enabled = _canHardDelete;
            }
        }
    }

    private async Task ChangeActiveAsync()
    {
        if (_saving || _existing is null || _bookService is null) return;
        bool activate = !_existing.IsActive;
        string verb = activate ? "restore" : "archive";
        if (!ThemedMessageDialog.Confirm(FindForm(),
                activate ? "Restore Book" : "Archive Book",
                $"Are you sure you want to {verb} this book?",
                danger: !activate,
                confirmText: activate ? "Restore" : "Archive"))
            return;
        await RunLifecycleAsync(
            () => _bookService.SetActiveAsync(_existing.BookId, activate),
            activate ? "Book restored successfully." : "Book archived successfully.");
    }

    private async Task DeleteAsync()
    {
        if (_saving || _existing is null || _bookService is null || !_canHardDelete) return;
        if (!ThemedMessageDialog.Confirm(FindForm(),
                "Delete Book Permanently",
                "This action cannot be undone. Books with borrowing " +
                "history cannot be deleted.",
                danger: true,
                confirmText: "Delete"))
            return;
        await RunLifecycleAsync(
            () => _bookService.DeleteAsync(_existing.BookId),
            "Book deleted successfully.");
    }

    private async Task RunLifecycleAsync(Func<Task<ServiceResult>> action, string successMessage)
    {
        _saving = true;
        _save.Enabled = false;
        _archive.Enabled = false;
        _delete.Enabled = false;
        try
        {
            var result = await action();
            if (IsDisposed || Disposing) return;
            if (result.Success)
            {
                // Notify before Saved closes the editor.
                CatalogActionFeedback.Notify(this, successMessage);
                Saved?.Invoke(this, EventArgs.Empty);
            }
            else ThemedMessageDialog.ShowAlert(FindForm(),
                "Book Operation",
                result.Error ?? "The operation could not be completed.");
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                ThemedMessageDialog.ShowAlert(FindForm(), "LiteraHub", ex.Message, true);
        }
        finally
        {
            _saving = false;
            if (!IsDisposed && !Disposing)
            {
                _save.Enabled = !_editing || (_authorsLoaded && _statusesLoaded && _category.HasCategories);
                _archive.Enabled = true;
                _delete.Enabled = _canHardDelete;
            }
        }
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
        _detailsHeader.ApplyTheme();

        _viewport.BackColor = c.Background;
        _content.BackColor = c.Background;
        _actionFooter.BackColor = c.Background;

        foreach (Control card in _content.Controls)
            foreach (Control control in card.Controls)
            {
                if (control is not Label label)
                    continue;

                label.BackColor = c.Surface;
                label.ForeColor = c.Text;

                label.Font = Equals(label.Tag, "section")
                    ? Theme.BodyStrong
                    : Theme.Body;
            }

        foreach (var overlay in _viewValues.Values)
        {
            overlay.BackColor = c.SurfaceAlt;
            overlay.ForeColor = c.Text;
            overlay.Font = Theme.BodyStrong;
        }
        // Same themed Cancel button as StudentDetailsPage.
        _cancelEdit.BackColor = c.SurfaceAlt;
        _cancelEdit.ForeColor = c.Text;
        _cancelEdit.Font = Theme.Body;
        _cancelEdit.FlatAppearance.BorderColor = c.Border;
        _cancelEdit.FlatAppearance.BorderSize = 1;
        _cancelEdit.FlatAppearance.MouseOverBackColor = c.AccentSoft;
        _cancelEdit.FlatAppearance.MouseDownBackColor = c.AccentSoft;
        _category.ApplyTheme();
        _condition.ApplyTheme();
        _authors.ApplyTheme();

        _save.BackColor =
            Color.FromArgb(34, 139, 86);

        _save.ForeColor = Color.White;
        _save.Font = Theme.BodyStrong;
        _save.FlatAppearance.BorderSize = 0;

        foreach (var action in new[] { _archive, _delete })
        {
            action.BackColor = c.Surface;
            action.ForeColor = c.Text;
            action.Font = Theme.BodyStrong;
            action.FlatAppearance.BorderSize = 1;
            action.FlatAppearance.BorderColor = c.Border;
            action.Cursor = Cursors.Hand;
        }

        // Match the soft coral-red Delete confirmation button.
        Color deleteRed = Color.FromArgb(255, 143, 137);
        _delete.BackColor = deleteRed;
        _delete.ForeColor = Color.White;
        _delete.FlatAppearance.BorderSize = 0;
        _delete.FlatAppearance.MouseOverBackColor =
            ControlPaint.Dark(deleteRed, 0.12f);
        _delete.FlatAppearance.MouseDownBackColor =
            ControlPaint.Dark(deleteRed, 0.23f);

        _cover.RefreshTheme();
        _lookup.RefreshTheme();
        _total.RefreshTheme();
        _available.RefreshTheme();
        _scrollbar.RefreshTheme();

        ArrangeForm();
        PositionViewValues();
        RefreshValidationCaptions();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _validation.Dispose();
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
                double scale = Math.Min((double)destination.Width / _coverImage.Width,
                    (double)destination.Height / _coverImage.Height);
                int imageWidth = Math.Max(1, (int)Math.Round(_coverImage.Width * scale));
                int imageHeight = Math.Max(1, (int)Math.Round(_coverImage.Height * scale));
                g.DrawImage(_coverImage, new Rectangle(destination.X + (destination.Width - imageWidth) / 2,
                    destination.Y + (destination.Height - imageHeight) / 2, imageWidth, imageHeight));
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

}

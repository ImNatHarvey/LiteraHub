
using FontAwesome.Sharp;
using System.Runtime.InteropServices;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Pages;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class ShellForm : Form
{
    private sealed record PageDef(
        string Key,
        string Title,
        IconChar Icon,
        RolePermission? Permission,
        string Note);

    private static readonly PageDef[] AllPages =
    {
        new("home", "Home", AppIcons.Home,
            null,
            "Overview and quick stats. Built after the core modules."),

        new("books", "Books", AppIcons.Books,
            RolePermission.BooksView,
            "Books catalog and management."),

        new("authors", "Authors", AppIcons.Authors,
            RolePermission.AuthorsView,
            "Authors module. Step 5."),

        new("categories", "Categories", AppIcons.Categories,
            RolePermission.CategoriesView,
            "Categories management."),

        new("students", "Students", AppIcons.Students,
            RolePermission.StudentsView,
            "Student directory. Step 6."),

        new("borrow", "Borrow && Return", AppIcons.BorrowReturn,
            RolePermission.BorrowReturnView,
            "Borrow and return. Step 6."),

        new("reports", "Reports", AppIcons.Reports,
            RolePermission.ReportsView,
            "The four summary lists. Step 7."),

        new("users", "Users", AppIcons.Users,
            RolePermission.UsersView,
            "Staff accounts.") ,
        new("profile", "Profile && Settings", IconChar.UserAlt, null, "Personal account and security.")
    };

    private readonly ISessionContext _session;
    private readonly IPermissionService _permissions;
    private readonly IAuthService _auth;
    private readonly IProfileService _profile;
    private readonly ICategoryService _categories;
    private readonly IBookService _books;
    private readonly IAuthorCreationService _authors;
    private readonly IBookMetadataService _metadata;
    private readonly IStudentService _students;
    private readonly IStaffUserService _staffUsers;
    private readonly IRoleService _roles;

    private readonly List<PageDef> _pages;

    private readonly TableLayoutPanel _root = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1
    };

    private readonly TableLayoutPanel _main = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 2
    };

    // The top bar hosts the reusable window controls.
    // Its duplicate page-title label has been removed.
    private readonly TableLayoutPanel _topBar = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1
    };

    private readonly ThemedWindowControls _windowControls = new()
    {
        ShowMinimizeButton = true,
        ShowMaximizeButton = true,
        Anchor = AnchorStyles.Right,
        Margin = new Padding(0, 0, 4, 0)
    };


    private readonly SidebarNav _nav = new()
    {
        Dock = DockStyle.Fill
    };

    private readonly Panel _host = new()
    {
        Dock = DockStyle.Fill
    };

    private Control? _currentPage;
    private BookEditorPage? _authorBookOverlay;
    private AuthorsPage? _bookOrigin;
    private CategoriesPage? _categoryBookOrigin;

    private bool _isFullscreen;
    private bool _loadedWindow;
    private const int NormalWidth = 1280;
    private const int NormalHeight = 720;
    private const int WmNcLButtonDown = 0x00A1;
    private const int HtCaption = 2;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private bool _userCollapsedSidebar;

    public bool LoggedOut { get; private set; }

    public ShellForm(
        ISessionContext session,
        IPermissionService permissions,
        IAuthService auth,
        ICategoryService categories,
        IBookService books,
        IAuthorCreationService authors,
        IBookMetadataService metadata,
        IStudentService students,
        IStaffUserService staffUsers, IRoleService roles, IProfileService profile)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _session = session ??
            throw new ArgumentNullException(nameof(session));

        _permissions = permissions ??
            throw new ArgumentNullException(nameof(permissions));

        _auth = auth ??
            throw new ArgumentNullException(nameof(auth));

        _categories = categories ??
            throw new ArgumentNullException(nameof(categories));

        _books = books ??
            throw new ArgumentNullException(nameof(books));

        _authors = authors ??
            throw new ArgumentNullException(nameof(authors));

        _metadata = metadata ??
            throw new ArgumentNullException(nameof(metadata));

        _students = students ??
            throw new ArgumentNullException(nameof(students));

        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _staffUsers = staffUsers ??
            throw new ArgumentNullException(nameof(staffUsers));

        _pages = AllPages
            .Where(p => p.Permission is null || permissions.Can(p.Permission.Value) || (p.Key == "users" && permissions.Can(RolePermission.RolesView)))
            .Select(p => p.Key == "users" && !permissions.Can(RolePermission.UsersView) ? p with { Title = "Roles && Permissions" } : p)
            .ToList();

        SuspendLayout();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        // No native caption or window title; all window buttons are themed.
        Text = "LiteraHub"; // Accessible/taskbar title only, not drawn on the form.
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        KeyPreview = true;

        _root.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 220));

        _root.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _main.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _main.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 56));

        _main.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        // Flexible empty space on the left, window controls on the right.
        _topBar.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _topBar.ColumnStyles.Add(
            new ColumnStyle(SizeType.AutoSize));

        _topBar.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));


        var current = session.Current;

        _nav.SetUser(
            current?.Username ?? "",
            current?.RoleName ?? "");

        _nav.SetItems(
            _pages.Select(p =>
                new NavItem(p.Key, p.Title, p.Icon)));

        _nav.ItemSelected += (_, key) => ShowPage(key);
        _nav.ToggleRequested += (_, _) => ToggleNavigation();
        _nav.LogoutRequested += (_, _) => Logout();

        // No page title: right side has reusable themed caption buttons.
        _topBar.Controls.Add(_windowControls, 1, 0);
        _windowControls.Bind(this);
        _windowControls.MaximizeRequested += (_, _) => ToggleFullscreen();

        // Only unused header space drags; do not intercept caption buttons.
        _topBar.MouseDown += DragShellFromHeader;
        _topBar.DoubleClick += (_, _) => ToggleFullscreen();

        _main.Controls.Add(_topBar, 0, 0);
        _main.Controls.Add(_host, 0, 1);

        _root.Controls.Add(_nav, 0, 0);
        _root.Controls.Add(_main, 1, 0);

        _nav.Margin = Padding.Empty;
        _main.Margin = Padding.Empty;
        _topBar.Margin = Padding.Empty;
        _host.Margin = Padding.Empty;

        Controls.Add(_root);

        Theme.Changed += OnThemeChanged;

        ResumeLayout(performLayout: true);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        ApplyTheme();
        ApplyNormalWindowSize();
        _loadedWindow = true;

        if (_pages.Count > 0)
            ShowPage(_pages[0].Key);
    }

    // Fullscreen uses actual monitor bounds, including the taskbar area,
    // rather than a hardcoded 1920x1080 size. A second click restores 1280x720.
    private void ToggleFullscreen() => SetFullscreen(!_isFullscreen);

    private void SetFullscreen(bool fullscreen)
    {
        if (IsDisposed || Disposing || !_loadedWindow || _isFullscreen == fullscreen)
            return;

        _isFullscreen = fullscreen;
        SuspendLayout();
        try
        {
            if (fullscreen)
            {
                // A borderless Normal-state form can occupy the entire display.
                // Capture the display before changing this form's bounds.
                var display = Screen.FromControl(this).Bounds;
                WindowState = FormWindowState.Normal;
                Bounds = display;
            }
            else
            {
                ApplyNormalWindowSize();
            }

            _nav.Compact = _userCollapsedSidebar;
            _root.ColumnStyles[0].Width = Px(_userCollapsedSidebar ? 64 : 220);
            _main.RowStyles[0].Height = Px(56);
            _windowControls.SetMaximizedState(_isFullscreen);
        }
        finally { ResumeLayout(performLayout: true); }
    }

    private void DragShellFromHeader(object? sender, MouseEventArgs e)
    {
        if (_isFullscreen || e.Button != MouseButtons.Left)
            return;
        ReleaseCapture();
        SendMessage(Handle, WmNcLButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
    }

    private int Px(int designPixels) =>
        (int)Math.Round(designPixels * DeviceDpi / 96f);

    private void ApplyNormalWindowSize()
    {
        // Honor the 1280x720 design size, clamped on smaller displays.
        var work = Screen.FromControl(this).WorkingArea;
        ClientSize = new Size(
            Math.Min(Px(NormalWidth), work.Width),
            Math.Min(Px(NormalHeight), work.Height));
        CenterToScreen();
        _nav.Compact = _userCollapsedSidebar;
        _root.ColumnStyles[0].Width = Px(_userCollapsedSidebar ? 64 : 220);
        _main.RowStyles[0].Height = Px(56);
    }

    private void ToggleNavigation()
    {
        _userCollapsedSidebar = !_userCollapsedSidebar;
        _root.SuspendLayout();
        try
        {
            _nav.Compact = _userCollapsedSidebar;
            _root.ColumnStyles[0].Width = Px(_userCollapsedSidebar ? 64 : 220);
        }
        finally { _root.ResumeLayout(true); }
    }

    private void Logout()
    {
        _auth.Logout();
        LoggedOut = true;
        Close();
    }

    private void ShowPage(string key)
    {
        var page = _pages.FirstOrDefault(
            p => p.Key == key);

        if (page is null || (page.Permission is { } required && !_permissions.Can(required) && !(key == "users" && _permissions.Can(RolePermission.RolesView))))
            return;

        Control next;

        if (key == "profile")
        {
            var profilePage = new ProfilePage(_profile);
            profilePage.UsernameChanged += (_, _) =>
            {
                var account = _session.Current;
                _nav.SetUser(account?.Username ?? "", account?.RoleName ?? "");
            };
            next = profilePage;
        }
        else if (key == "categories")
        {
            var categoriesPage = new CategoriesPage(_categories, _books, _permissions);
            categoriesPage.BookRequested += async (_, bookId) =>
                await ShowCategoryBookAsync(categoriesPage, bookId);
            next = categoriesPage;
        }
        else if (key == "books")
        {
            next = new BooksPage(
                _books,
                _categories,
                _authors,
                _metadata,
                _permissions);
        }

        else if (key == "students")
        {
            next = new StudentsPage(_students, _permissions);
        }
        else if (key == "authors")
        {
            var authorsPage = new AuthorsPage(_books, _authors, _permissions);
            authorsPage.BookRequested += async (_, bookId) =>
                await ShowAuthorBookAsync(authorsPage, bookId);
            next = authorsPage;
        }

        else if (key == "users")
        {
            string actorRole = _session.Current?.RoleName ?? "";
            if (_permissions.Can(RolePermission.UsersView))
            {
                var usersPage = new UsersPage(_staffUsers, _permissions, actorRole, _session.Current?.UserId,
                    _permissions.Can(RolePermission.RolesView) ? _roles : null);
                usersPage.ProfileRequested += (_, _) => ShowPage("profile");
                next = usersPage;
            }
            else if (_permissions.Can(RolePermission.RolesView))
                next = new RolesPage(_roles, _permissions, actorRole, showBack: false);
            else return;
        }
        else
        {
            next = new PlaceholderPage(
                page.Title,
                page.Note);
        }

        next.Dock = DockStyle.Fill;

        SwapPage(next);
        _nav.SelectItem(page.Key);
    }

    private async Task ShowAuthorBookAsync(AuthorsPage origin, int bookId)
    {
        if (_authorBookOverlay is not null || origin.IsDisposed) return;
        // BookEditorPage exposes Edit controls; do not bypass ManageBooks authorization.
        if (!_permissions.Can(RolePermission.BooksView))
        {
            ThemedMessageDialog.ShowAlert(this, "View Book",
                "Book details navigation is unavailable for this role.");
            return;
        }
        Book? book;
        try { book = await _books.GetByIdAsync(bookId); }
        catch (Exception ex)
        {
            ThemedMessageDialog.ShowAlert(this, "View Book", ex.Message, danger: true);
            return;
        }
        if (book is null || origin.IsDisposed || _currentPage != origin) return;
        var editor = new BookEditorPage(book) { Dock = DockStyle.Fill };
        editor.ConfigureServices(_categories, _metadata, _books,
            _permissions.Can(RolePermission.BooksDelete), _authors,
            _permissions.Can(RolePermission.AuthorsAdd), _permissions);
        editor.BackRequested += (_, _) => CloseAuthorBookOverlay();
        editor.Saved += async (_, _) =>
        {
            CloseAuthorBookOverlay();
            if (!origin.IsDisposed) await origin.RefreshAuthorDetailsAsync();
        };
        _bookOrigin = origin;
        _authorBookOverlay = editor;
        _host.Controls.Add(editor);
        editor.BringToFront();
    }

    private async Task ShowCategoryBookAsync(CategoriesPage origin, int bookId)
    {
        if (_authorBookOverlay is not null || origin.IsDisposed) return;
        // BookEditorPage includes edit controls. Keep existing permission protections.
        if (!_permissions.Can(RolePermission.BooksView))
        {
            ThemedMessageDialog.ShowAlert(this, "View Book",
                "Book details navigation is unavailable for this role.");
            return;
        }
        Book? book;
        try { book = await _books.GetByIdAsync(bookId); }
        catch (Exception ex)
        {
            ThemedMessageDialog.ShowAlert(this, "View Book", ex.Message, danger: true);
            return;
        }
        if (book is null || origin.IsDisposed || _currentPage != origin ||
            _authorBookOverlay is not null) return;
        var editor = new BookEditorPage(book) { Dock = DockStyle.Fill };
        editor.ConfigureServices(_categories, _metadata, _books,
            _permissions.Can(RolePermission.BooksDelete), _authors,
            _permissions.Can(RolePermission.AuthorsAdd), _permissions);
        editor.BackRequested += (_, _) => CloseAuthorBookOverlay();
        editor.Saved += async (_, _) =>
        {
            CloseAuthorBookOverlay();
            if (!origin.IsDisposed) await origin.RefreshCategoryDetailsAsync();
        };
        _categoryBookOrigin = origin;
        _authorBookOverlay = editor;
        _host.Controls.Add(editor);
        editor.BringToFront();
    }

    private void CloseAuthorBookOverlay()
    {
        if (_authorBookOverlay is null) return;
        _host.Controls.Remove(_authorBookOverlay);
        _authorBookOverlay.Dispose();
        _authorBookOverlay = null;
        _bookOrigin?.BringToFront();
        _categoryBookOrigin?.BringToFront();
        _bookOrigin = null;
        _categoryBookOrigin = null;
    }

    private void SwapPage(Control next)
    {
        _host.SuspendLayout();
        CloseAuthorBookOverlay();

        if (_currentPage is not null)
        {
            _host.Controls.Remove(_currentPage);
            _currentPage.Dispose();
        }

        _host.Controls.Add(next);
        _currentPage = next;

        _host.ResumeLayout(performLayout: true);
    }

    private void OnThemeChanged(
        object? sender,
        EventArgs e)
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var c = Theme.Colors;

        SuspendLayout();

        BackColor = c.Background;
        ForeColor = c.Text;

        _root.BackColor = c.Background;
        _main.BackColor = c.Background;
        _host.BackColor = c.Background;
        _topBar.BackColor = c.SurfaceAlt;
        _windowControls.SetSurfaceColor(_topBar.BackColor);


        ResumeLayout(performLayout: true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }
}

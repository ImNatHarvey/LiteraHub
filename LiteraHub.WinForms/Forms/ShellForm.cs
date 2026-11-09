using FontAwesome.Sharp;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Security;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Pages;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class ShellForm : Form
{
    private sealed record PageDef(string Key, string Title, IconChar Icon, Permission Permission, string Note);

    private static readonly PageDef[] AllPages =
    {
        new("home", "Home", AppIcons.Home, Permission.ViewCatalog, "Overview and quick stats. Built after the core modules."),
        new("books", "Books", AppIcons.Books, Permission.ViewCatalog, "Books module: CRUD and search. Step 5."),
        new("authors", "Authors", AppIcons.Authors, Permission.ViewCatalog, "Authors module. Step 5."),
        new("categories", "Categories", AppIcons.Categories, Permission.ViewCatalog, "Categories module. Step 5."),
        new("students", "Students", AppIcons.Students, Permission.ManageStudents, "Students module. Step 5."),
        new("borrow", "Borrow & Return", AppIcons.BorrowReturn, Permission.ProcessBorrowReturn, "Borrow and return with auto-suggest. Step 6."),
        new("reports", "Reports", AppIcons.Reports, Permission.ViewReports, "The four summary lists. Step 7."),
        new("users", "Users", AppIcons.Users, Permission.ManageUsers, "Staff accounts (Admin only). Planned alongside Step 5.")
    };

    private readonly ISessionContext _session;
    private readonly IPermissionService _permissions;
    private readonly IAuthService _auth;
    private readonly List<PageDef> _pages;

    private readonly TableLayoutPanel _root = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
    private readonly TableLayoutPanel _main = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
    private readonly TableLayoutPanel _topBar = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
    private readonly Label _pageTitle = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(Space.XL, 0, 0, 0) };
    private readonly IconButton _demoButton = new();
    private readonly ContextMenuStrip _demoMenu = new();
    private readonly SidebarNav _nav = new() { Dock = DockStyle.Fill };
    private readonly Panel _host = new() { Dock = DockStyle.Fill };

    private Control? _currentPage;
    private int _presetWidth = 1280;
    private int _presetHeight = 720;

    public bool LoggedOut { get; private set; }

    public ShellForm(ISessionContext session, IPermissionService permissions, IAuthService auth)
    {
        _session = session;
        _permissions = permissions;
        _auth = auth;
        _pages = AllPages.Where(p => permissions.Can(p.Permission)).ToList();

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "LiteraHub";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _main.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        _main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _topBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _demoButton.Text = "  Demo";
        _demoButton.IconChar = AppIcons.Demo;
        _demoButton.IconSize = 18;
        _demoButton.Size = new Size(100, 36);
        _demoButton.Anchor = AnchorStyles.Right;
        _demoButton.Margin = new Padding(0, 0, Space.L, 0);
        _demoButton.Cursor = Cursors.Hand;
        _demoButton.FlatStyle = FlatStyle.Flat;
        _demoButton.TextImageRelation = TextImageRelation.ImageBeforeText;
        BuildDemoMenu();

        var current = session.Current;
        _nav.SetUser(current?.Username ?? "", current?.RoleName ?? "");
        _nav.SetItems(_pages.Select(p => new NavItem(p.Key, p.Title, p.Icon)));
        _nav.ItemSelected += (_, key) => ShowPage(key);
        _nav.LogoutRequested += (_, _) =>
        {
            _auth.Logout();
            LoggedOut = true;
            Close();
        };

        _topBar.Controls.Add(_pageTitle, 0, 0);
        _topBar.Controls.Add(_demoButton, 1, 0);
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
        ApplyWindowSize(_presetWidth, _presetHeight);
        if (_pages.Count > 0) ShowPage(_pages[0].Key);
    }


    private void BuildDemoMenu()
    {
        var sizeMenu = new ToolStripMenuItem("Window size");

        foreach (var (label, w, h) in new[]
                 {
                 ("1920 x 1080", 1920, 1080),
                 ("1280 x 720 (primary)", 1280, 720),
                 ("390 x 844 (portrait)", 390, 844)
             })
        {
            var item = new ToolStripMenuItem(label);

            item.Click += (_, _) =>
                ApplyWindowSize(w, h);

            sizeMenu.DropDownItems.Add(item);
        }

        var themeMenu = new ToolStripMenuItem("Appearance");

        var themeItems =
            new Dictionary<ThemePreset, ToolStripMenuItem>();

        foreach (var preset in Theme.AvailablePresets)
        {
            var item = new ToolStripMenuItem(
                Theme.GetDisplayName(preset));

            item.Click += (_, _) =>
            {
                Theme.SetPreset(preset);

                if (!ThemePreferences.TrySave(out var error))
                {
                    MessageBox.Show(
                        "The theme was applied, but could not be saved." +
                        Environment.NewLine + error,
                        "LiteraHub: appearance settings",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            };

            themeItems.Add(preset, item);
            themeMenu.DropDownItems.Add(item);
        }

        themeMenu.DropDownOpening += (_, _) =>
        {
            foreach (var (preset, item) in themeItems)
            {
                item.Checked = preset == Theme.Preset;
            }
        };

        _demoMenu.Items.AddRange(
            new ToolStripItem[]
            {
            sizeMenu,
            themeMenu
            });

        _demoButton.Click += (_, _) =>
            _demoMenu.Show(
                _demoButton,
                new Point(0, _demoButton.Height));
    }


    private int Px(int designPixels) => (int)Math.Round(designPixels * DeviceDpi / 96f);

    // Sizes are logical (96 DPI) pixels, scaled to the current monitor and clamped to the screen.
    private void ApplyWindowSize(int width, int height)
    {
        _presetWidth = width;
        _presetHeight = height;

        var work = Screen.FromControl(this).WorkingArea;
        var frame = new Size(Width - ClientSize.Width, Height - ClientSize.Height);
        var wanted = new Size(Px(width), Px(height));
        var maxClient = new Size(work.Width - frame.Width, work.Height - frame.Height);
        var actual = new Size(Math.Min(wanted.Width, maxClient.Width), Math.Min(wanted.Height, maxClient.Height));
        var clamped = actual != wanted;

        SuspendLayout();
        ClientSize = actual;

        var logicalWidth = actual.Width * 96f / DeviceDpi;
        var compact = logicalWidth < 700;
        _nav.Compact = compact;
        _root.ColumnStyles[0].Width = Px(compact ? 64 : 220);
        _main.RowStyles[0].Height = Px(56);
        ResumeLayout(performLayout: true);

        CenterToScreen();
        Text = $"LiteraHub - {width} x {height}{(clamped ? " (fit to screen)" : "")}";
    }

    private void ShowPage(string key)
    {
        var page = _pages.FirstOrDefault(p => p.Key == key);
        if (page is null) return;

        SwapPage(new PlaceholderPage(page.Title, page.Note) { Dock = DockStyle.Fill }, page.Title);
        _nav.SelectItem(page.Key);
    }

    private void SwapPage(Control next, string title)
    {
        _host.SuspendLayout();
        _host.Controls.Add(next);
        if (_currentPage is not null)
        {
            _host.Controls.Remove(_currentPage);
            _currentPage.Dispose();
        }
        _currentPage = next;
        _host.ResumeLayout(performLayout: true);

        _pageTitle.Text = title;
    }

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTheme();

    private void ApplyTheme()
    {
        var c = Theme.Colors;
        SuspendLayout();
        BackColor = c.Background;
        ForeColor = c.Text;
        _root.BackColor = c.Background;
        _main.BackColor = c.Background;
        _host.BackColor = c.Background;
        _topBar.BackColor = c.Surface;

        _pageTitle.BackColor = c.Surface;
        _pageTitle.ForeColor = c.Text;
        _pageTitle.Font = Theme.Title;

        _demoButton.BackColor = c.SurfaceAlt;
        _demoButton.ForeColor = c.Text;
        _demoButton.IconColor = c.Accent;
        _demoButton.Font = Theme.Body;
        _demoButton.FlatAppearance.BorderColor = c.Border;
        _demoButton.FlatAppearance.BorderSize = 1;

        _demoMenu.BackColor = c.Surface;
        _demoMenu.ForeColor = c.Text;
        ResumeLayout(performLayout: true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= OnThemeChanged;
            _demoMenu.Dispose();
        }
        base.Dispose(disposing);
    }
}
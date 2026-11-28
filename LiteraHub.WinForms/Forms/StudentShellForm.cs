using System.Runtime.InteropServices;
using FontAwesome.Sharp;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Security;
using LiteraHub.Data.Repositories;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Pages;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class StudentShellForm : Form
{
    private readonly ISessionContext _session;
    private readonly IAuthService _auth;
    private readonly IProfileService _profile;
    private readonly StudentPortalRepository _portal;
    private readonly SidebarNav _nav = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly TableLayoutPanel _root = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
    private readonly TableLayoutPanel _main = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
    private readonly TableLayoutPanel _topBar = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
    private readonly Panel _host = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly ThemedWindowControls _window = new() { ShowMaximizeButton = true };
    private readonly System.Windows.Forms.Timer _sessionTimer = new() { Interval = 3000 };
    private Control? _page;
    private string _key = "home";
    private bool _compact, _fullscreen, _validating, _closing;
    public bool LoggedOut { get; private set; }
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr handle, int msg, IntPtr wParam, IntPtr lParam);

    public StudentShellForm(ISessionContext session, IAuthService auth, IProfileService profile, StudentPortalRepository portal)
    {
        if (session.Current is not { } actor || !StudentAccountPolicy.IsStudentRole(actor.RoleName) || actor.StudentId is not > 0)
            throw new UnauthorizedAccessException("Student session required.");
        _session = session; _auth = auth; _profile = profile; _portal = portal;
        Text = "LiteraHub — Student"; FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterScreen;
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi; DoubleBuffered = true; MinimizeBox = true; MaximizeBox = false;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220)); _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); _main.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); _main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); _topBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); _topBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _topBar.Controls.Add(_window, 1, 0); _window.Bind(this); _window.MaximizeRequested += (_, _) => ToggleFullscreen();
        _topBar.MouseDown += (_, e) => { if (!_fullscreen && e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); } };
        _topBar.DoubleClick += (_, _) => ToggleFullscreen();
        _main.Controls.Add(_topBar, 0, 0); _main.Controls.Add(_host, 0, 1); _root.Controls.Add(_nav, 0, 0); _root.Controls.Add(_main, 1, 0); Controls.Add(_root);
        _nav.SetItems(new[] { new NavItem("home", "Home", AppIcons.Home), new NavItem("profile", "Profile && Settings", IconChar.UserAlt) });
        _nav.SetUser(actor.Username, "Student"); _nav.ItemSelected += async (_, key) => await NavigateAsync(key);
        _nav.ToggleRequested += (_, _) => { _compact = !_compact; ApplyNavigation(); };
        _nav.LogoutRequested += (_, _) => { if (_page is ProfilePage { IsBusy: true }) return; if (ThemedMessageDialog.Confirm(this, "Sign Out", "Sign out of your student account?", false, "Sign Out")) EndSession(); };
        Theme.Changed += ThemeChanged; _sessionTimer.Tick += async (_, _) => await ValidateSessionAsync();
        FormClosing += (_, e) => { if (!_closing && _page is ProfilePage { IsBusy: true }) { e.Cancel = true; return; } _closing = true; _sessionTimer.Stop(); };
    }
    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e); ApplyTheme(); RestoreWindow(); await NavigateAsync("home");
        if (!_closing && !IsDisposed) _sessionTimer.Start();
    }
    private int Px(int value) => (int)Math.Round(value * DeviceDpi / 96f);
    private void ApplyNavigation() { _nav.Compact = _compact; _root.ColumnStyles[0].Width = Px(_compact ? 64 : 220); _main.RowStyles[0].Height = Px(56); }
    private void RestoreWindow() { var work = Screen.FromControl(this).WorkingArea; WindowState = FormWindowState.Normal; ClientSize = new Size(Math.Min(Px(1280), work.Width), Math.Min(Px(720), work.Height)); CenterToScreen(); ApplyNavigation(); }
    private void ToggleFullscreen() { if (_closing) return; _fullscreen = !_fullscreen; if (_fullscreen) { var display = Screen.FromControl(this).Bounds; WindowState = FormWindowState.Normal; Bounds = display; ApplyNavigation(); } else RestoreWindow(); _window.SetMaximizedState(_fullscreen); }
    private async Task NavigateAsync(string key)
    {
        if (_closing || _validating || _page is ProfilePage { IsBusy: true }) { _nav.SelectItem(_key); return; }
        _validating = true;
        try
        {
            var before = _session.Current; var info = await _portal.GetOwnAsync();
            if (_closing || IsDisposed) return;
            if (_session.Current != before) { _nav.SelectItem(_key); return; }
            if (info is null) { DenySession(); return; }
            Control next;
            if (key == "profile")
            {
                var profilePage = new ProfilePage(_profile, studentMode: true);
                profilePage.UsernameChanged += (_, _) => _nav.SetUser(_session.Current?.Username ?? "", "Student"); next = profilePage;
            }
            else { key = "home"; next = new StudentHomePage(info); }
            if (_page is not null) { _host.Controls.Remove(_page); _page.Dispose(); }
            _page = next; next.Dock = DockStyle.Fill; _host.Controls.Add(next); _key = key; _nav.SelectItem(key); _nav.SetUser(info.Username, "Student");
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.GetType().Name); if (!_closing && !IsDisposed) DenySession(); }
        finally { _validating = false; }
    }
    private async Task ValidateSessionAsync()
    {
        if (_closing || _validating || !Enabled || _page is ProfilePage { IsBusy: true }) return;
        _validating = true;
        try
        {
            var before = _session.Current; var info = await _portal.GetOwnAsync();
            if (_closing || IsDisposed || _session.Current != before) return;
            if (info is null) DenySession();
            else if (_page is StudentHomePage home) home.UpdateInfo(info);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.GetType().Name); if (!_closing && !IsDisposed) DenySession(); }
        finally { _validating = false; }
    }
    private void DenySession()
    {
        _sessionTimer.Stop(); _auth.Logout(); _closing = true; LoggedOut = true; _host.Enabled = false;
        ThemedMessageDialog.ShowAlert(this, "Student Session Ended", "Your account is no longer available or its session has changed. Please sign in again or visit the library.", true); Close();
    }
    private void EndSession() { _closing = true; _sessionTimer.Stop(); _auth.Logout(); LoggedOut = true; Close(); }
    private void ThemeChanged(object? sender, EventArgs e) { if (!IsDisposed) ApplyTheme(); }
    private void ApplyTheme() { var c = Theme.Colors; BackColor = _root.BackColor = _main.BackColor = _host.BackColor = c.Background; ForeColor = c.Text; _topBar.BackColor = c.SurfaceAlt; _window.SetSurfaceColor(c.SurfaceAlt); }
    protected override void Dispose(bool disposing) { if (disposing) { _closing = true; _sessionTimer.Stop(); _sessionTimer.Dispose(); Theme.Changed -= ThemeChanged; } base.Dispose(disposing); }
}

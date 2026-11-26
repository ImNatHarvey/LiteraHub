using FontAwesome.Sharp;
using LiteraHub.Core.Interfaces;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class ChangeUsernameDialog : Form
{
    private readonly IProfileService _service;
    private readonly ThemedTextField _username = new() { MaxLength = 50 };
    private readonly ThemedTextField _password = new() { ShowPasswordToggle = true, MaxLength = 72 };
    private readonly Label _usernameLabel = new() { Text = "New username" };
    private readonly Label _passwordLabel = new() { Text = "Current password" };
    private readonly CenteredIconButton _save = new() { Text = "Save Username", IconChar = IconChar.UserAlt, IconSize = 18 };
    private readonly Button _cancel = new() { Text = "Cancel" };
    private readonly ThemedWindowControls _close = new() { ShowMinimizeButton = false, ShowMaximizeButton = false };
    private bool _busy;
    public ChangeUsernameDialog(IProfileService service, string username)
    {
        _service = service;
        var c = Theme.Colors;
        FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false; ClientSize = new Size(480, 316); BackColor = c.Background;
        Controls.Add(new Panel { Dock = DockStyle.Left, Width = 4, BackColor = c.Accent });
        var title = new Label { Text = "Change Username", Font = Theme.Title, ForeColor = c.Text, Bounds = new Rectangle(24, 16, 350, 32) };
        Controls.Add(title); title.MouseDown += BeginDrag; MouseDown += BeginDrag;
        _close.Location = new Point(ClientSize.Width - _close.Width - 12, 10);
        _close.Bind(this); _close.SetSurfaceColor(c.Background); Controls.Add(_close);
        var hint = new Label { Text = "Current username: " + username, Font = Theme.Body, ForeColor = c.TextMuted, Bounds = new Rectangle(24, 54, 432, 24) };
        Controls.Add(hint);
        _usernameLabel.SetBounds(24, 86, 432, 22); _username.SetBounds(24, 110, 432, 40);
        _passwordLabel.SetBounds(24, 158, 432, 22); _password.SetBounds(24, 182, 432, 40);
        foreach (var label in new[] { _usernameLabel, _passwordLabel }) { label.Font = Theme.Body; label.ForeColor = c.Text; }
        Controls.AddRange(new Control[] { _usernameLabel, _username, _passwordLabel, _password, _save, _cancel });
        _cancel.SetBounds(24, 252, 112, 40); _save.SetBounds(284, 252, 172, 40);
        foreach (Button b in new Button[] { _save, _cancel }) { b.FlatStyle = FlatStyle.Flat; b.Font = Theme.BodyStrong; b.Cursor = Cursors.Hand; b.FlatAppearance.BorderColor = c.Border; b.FlatAppearance.BorderSize = 1; }
        _save.BackColor = c.Accent; _save.ForeColor = _save.IconColor = c.OnAccent; _save.FlatAppearance.BorderSize = 0;
        _save.FlatAppearance.MouseOverBackColor = ControlPaint.Light(c.Accent, .08f); _save.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(c.Accent, .08f);
        _cancel.BackColor = c.Surface; _cancel.ForeColor = c.Text; _cancel.FlatAppearance.MouseOverBackColor = c.SurfaceAlt; _cancel.FlatAppearance.MouseDownBackColor = c.AccentSoft;
        _username.TextChanged += (_, _) => Error(_username, _usernameLabel, "New username", "");
        _password.TextChanged += (_, _) => Error(_password, _passwordLabel, "Current password", "");
        _cancel.Click += (_, _) => { if (!_busy) { DialogResult = DialogResult.Cancel; Close(); } };
        _save.Click += async (_, _) => await SaveAsync();
        CancelButton = _cancel;
        Shown += (_, _) => _username.FocusInput();
        FormClosing += (_, e) => { if (_busy) e.Cancel = true; };
    }
    private static void Error(ThemedTextField field, Label label, string name, string error)
    {
        field.HasValidationError = error.Length > 0;
        label.Text = name + (error.Length > 0 ? " • " + error : "");
        label.ForeColor = error.Length > 0 ? Theme.Colors.Danger : Theme.Colors.Text;
    }
    private async Task SaveAsync()
    {
        if (_busy) return;
        var username = _username.Text.Trim();
        bool validName = username.Length is >= 3 and <= 50 && username.All(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-');
        bool validPassword = _password.Text.Length > 0;
        Error(_username, _usernameLabel, "New username", validName ? "" : username.Length == 0 ? "Required" : "Invalid username");
        Error(_password, _passwordLabel, "Current password", validPassword ? "" : "Required");
        if (!validName || !validPassword) { if (!validName) _username.FocusInput(); else _password.FocusInput(); return; }
        _busy = true; _save.Enabled = _cancel.Enabled = _username.Enabled = _password.Enabled = false;
        try
        {
            var result = await _service.ChangeUsernameAsync(_password.Text, username);
            if (!result.Success)
            {
                if (result.Error?.StartsWith("Current password", StringComparison.Ordinal) == true) Error(_password, _passwordLabel, "Current password", "Incorrect or unavailable");
                ThemedMessageDialog.ShowAlert(this, "Change Username", result.Error ?? "Unable to change username.", true);
                return;
            }
            _password.Text = ""; _busy = false; DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); ThemedMessageDialog.ShowAlert(this, "Change Username", "Unable to change username.", true); }
        finally { _busy = false; if (!IsDisposed) _save.Enabled = _cancel.Enabled = _username.Enabled = _password.Enabled = true; }
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    private void BeginDrag(object? sender, MouseEventArgs e)
    { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0x00A1, (IntPtr)2, IntPtr.Zero); } }
}

using FontAwesome.Sharp;
using LiteraHub.Core.Interfaces;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Forms;
using LiteraHub.WinForms.Theming;
namespace LiteraHub.WinForms.Pages;

public sealed class ProfilePage : ThemedUserControl
{
    private readonly IProfileService _service;
    private readonly ScrollHost _surface = new() { Dock = DockStyle.Fill };
    private readonly Label _title = new() { Text = "Profile & Settings", UseMnemonic = false, Dock = DockStyle.Top, Height = 46, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _subtitle = new() { Text = "Manage your personal account and security.", Dock = DockStyle.Top, Height = 28 };
    private readonly TableLayoutPanel _grid = new() { Dock = DockStyle.None, ColumnCount = 2, RowCount = 1, Height = 548, Padding = new Padding(16, 12, 16, 12) };
    private readonly FlowLayoutPanel _left = Column(), _right = Column();
    private readonly Label[] _identity = Enumerable.Range(0, 5).Select(_ => ViewValue()).ToArray();
    private readonly Label _keyStatus = Caption("Recovery passkey status", 42);
    private readonly ThemedTextField _current = Secret(), _next = Secret(), _confirm = Secret(), _keyPassword = Secret();
    private readonly CenteredIconButton _save = Action("Change Password", IconChar.Lock), _generate = Action("Generate Passkey", IconChar.Key);
    private readonly CenteredIconButton _theme = Action("Choose Theme", IconChar.Palette);
    private readonly CenteredIconButton _editUsername = Action("Change Username", IconChar.UserAlt);
    private readonly CenteredIconButton _editName = Action("Edit Name", IconChar.UserAlt);
    public event EventHandler? UsernameChanged;
    private bool _busy, _ready;
    private readonly Dictionary<ThemedTextField, (Label Caption, string Name, string Error)> _captions = new();
    private bool _hasKey;
    public ProfilePage(IProfileService service)
    {
        _service = service;
        Padding = new Padding(Space.L);
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _grid.Controls.Add(_left, 0, 0); _grid.Controls.Add(_right, 1, 0);
        _surface.UseSurfaceBackground();
        _surface.SetContent(_grid);
        Controls.Add(_surface); Controls.Add(_subtitle); Controls.Add(_title);
        _left.Controls.Add(Heading("Account Information"));
        string[] names = { "Username", "Full Name", "Assigned role", "Account ID / Status", "Created" };
        for (int i = 0; i < names.Length; i++)
        {
            _left.Controls.Add(Caption(names[i])); _left.Controls.Add(_identity[i]);
            if (i == 0) _left.Controls.Add(_editUsername);
            if (i == 1) _left.Controls.Add(_editName);
        }
        _left.Controls.AddRange(new Control[] { Heading("Appearance"), _theme });
        _right.Controls.AddRange(new Control[] { Heading("Change Password"), FieldCaption(_current, "Current password"), _current, FieldCaption(_next, "New password"), _next, FieldCaption(_confirm, "Confirm new password"), _confirm, _save, Heading("Recovery Passkey"), _keyStatus, FieldCaption(_keyPassword, "Confirm current password"), _keyPassword, _generate });
        _grid.Height = 548;
        _surface.Resize += (_, _) => ResizeContent();
        _grid.Resize += (_, _) => { ResizeColumn(_left); ResizeColumn(_right); };
        _surface.RefreshMetrics();
        _left.Resize += (_, _) => ResizeColumn(_left); _right.Resize += (_, _) => ResizeColumn(_right);
        _editUsername.Click += async (_, _) =>
        {
            if (_busy || !_ready) return;
            using var dialog = new ChangeUsernameDialog(_service, _identity[0].Text);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
            await RefreshProfileAsync();
            if (IsDisposed) return;
            UsernameChanged?.Invoke(this, EventArgs.Empty);
            CatalogActionFeedback.Notify(this, "Username changed successfully.");
        };
        _editName.Click += async (_, _) =>
        {
            if (_busy || !_ready) return;
            SetBusy(true);
            try
            {
                var info = await _service.GetAsync();
                if (IsDisposed || info is null) return;
                using var dialog = new ChangeNameDialog(_service, info);
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                await RefreshProfileAsync();
                if (!IsDisposed) CatalogActionFeedback.Notify(this, "Name updated successfully.");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); if (!IsDisposed) Alert("Unable to load or update your name."); }
            finally { if (!IsDisposed) SetBusy(false); }
        };
        _save.Click += async (_, _) => await ChangeAsync();
        _generate.Click += async (_, _) => await GenerateAsync();
        _theme.Click += (_, _) => {
            using var dialog = new ThemePickerDialog();
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.SelectedPreset is not ThemePreset preset) return;
            Theme.SetPreset(preset);
            if (!ThemePreferences.TrySave(out var error)) ThemedMessageDialog.ShowAlert(FindForm(), "Appearance", error ?? "Unable to save theme preference.", true);
            else CatalogActionFeedback.Notify(this, "Appearance updated.");
        };
        foreach (var field in new[] { _current, _next, _confirm, _keyPassword })
            field.TextChanged += (_, _) => SetError(field, "");
        WireWheel(_grid);
        SetBusy(false);
        Load += async (_, _) => { ResizeContent(); ResizeColumn(_left); ResizeColumn(_right); await RefreshProfileAsync(); };
    }
    private static FlowLayoutPanel Column() => new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 0, 8, 0), AutoScroll = false };
    private static Label Caption(string text, int height = 22) => new() { Text = text, UseMnemonic = false, Height = height, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 0, 0, 1) };
    private static Label Heading(string text) { var label = Caption(text, 24); label.Tag = "heading"; return label; }
    private static ThemedTextField Secret() => new() { ShowPasswordToggle = true, Height = 36, MaxLength = 72, Margin = new Padding(0, 0, 0, 3) };
    private static CenteredIconButton Action(string text, IconChar icon) => new() { Text = text, IconChar = icon, IconSize = 18, Height = 40, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 3, 0, 3) };
    private static void ResizeColumn(FlowLayoutPanel column)
    { foreach (Control control in column.Controls) control.Width = Math.Max(80, column.ClientSize.Width - control.Margin.Horizontal); }
    private void ResizeContent()
    {
        if (_surface.ClientSize.Height <= 0) return;
        _grid.Height = Math.Clamp(_surface.ClientSize.Height, 548, 600);
        _surface.RefreshMetrics();

    }
    private static Label ViewValue()
    {
        var label = Caption("—", 40);
        label.Padding = new Padding(12, 0, 12, 0);
        label.AutoEllipsis = true;
        label.Tag = "value";
        label.Paint += (_, e) => { using var pen = new Pen(Theme.Colors.Border); e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, label.Width - 1), Math.Max(0, label.Height - 1)); };
        return label;
    }
    private Label FieldCaption(ThemedTextField field, string name)
    {
        var label = Caption(name);
        _captions[field] = (label, name, "");
        return label;
    }
    private void SetError(ThemedTextField field, string error)
    {
        var entry = _captions[field];
        _captions[field] = (entry.Caption, entry.Name, error);
        field.HasValidationError = error.Length > 0;
        entry.Caption.Text = entry.Name + (error.Length > 0 ? " • " + error : "");
        entry.Caption.ForeColor = error.Length > 0 ? Theme.Colors.Danger : Theme.Colors.Text;
    }
    private void WireWheel(Control control)
    {
        control.MouseWheel += (_, e) => { _surface.ScrollByPixels(e.Delta > 0 ? -60 : 60); if (e is HandledMouseEventArgs handled) handled.Handled = true; };
        foreach (Control child in control.Controls) WireWheel(child);
    }
    private void SetBusy(bool busy)
    { _busy = busy; _save.Enabled = _generate.Enabled = _editUsername.Enabled = _editName.Enabled = _ready && !busy; foreach (var f in new[] { _current, _next, _confirm, _keyPassword }) f.Enabled = _ready && !busy; }
    private async Task RefreshProfileAsync()
    {
        try
        {
            var info = await _service.GetAsync(); if (IsDisposed || Disposing) return;
            _ready = info is not null;
            if (info is null) { _identity[0].Text = "Account unavailable. Sign in again."; return; }
            _identity[0].Text = info.Username;
            _identity[1].Text = string.Join(" ", new[] { info.FirstName, info.MiddleName, info.LastName }.Where(n => !string.IsNullOrWhiteSpace(n)));
            if (_identity[1].Text.Length == 0) _identity[1].Text = "—";
            _identity[2].Text = info.RoleName;
            _identity[3].Text = $"{info.UserId} · Active";
            _identity[4].Text = info.CreatedAt.ToString("MMMM d, yyyy");
            _hasKey = info.HasRecoveryPasskey;
            _keyStatus.Text = _hasKey ? $"Passkey configured · {info.RecoveryPasskeyCreatedAt:MMMM d, yyyy}. The secret cannot be displayed again." : "No recovery passkey configured. Generate one and keep it somewhere safe.";
            _generate.Text = _hasKey ? "Regenerate Passkey" : "Generate Passkey";
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); if (!IsDisposed) { _ready = false; _identity[0].Text = "Unable to load account information."; } }
        finally { if (!IsDisposed) SetBusy(_busy); }
    }
    private bool Required(ThemedTextField field) { bool valid = !string.IsNullOrEmpty(field.Text); SetError(field, valid ? "" : "Required"); return valid; }
    private void Alert(string text) => ThemedMessageDialog.ShowAlert(FindForm(), "Profile & Settings", text, true);
    private async Task ChangeAsync()
    {
        if (_busy || !_ready) return;
        if (!(Required(_current) & Required(_next) & Required(_confirm))) { Alert("Complete all password fields."); return; }
        if (_next.Text != _confirm.Text) { SetError(_confirm, "Passwords do not match"); Alert("The new passwords do not match."); return; }
        SetBusy(true);
        try
        {
            var result = await _service.ChangePasswordAsync(_current.Text, _next.Text);
            if (IsDisposed) return;
            if (!result.Success)
            {
                if (result.Error?.StartsWith("Current password is incorrect", StringComparison.Ordinal) == true)
                    SetError(_current, "Incorrect or unavailable");
                Alert(result.Error ?? "Unable to change password."); return;
            }
            _current.Text = _next.Text = _confirm.Text = "";
            foreach (var field in new[] { _current, _next, _confirm }) field.HidePassword();
            CatalogActionFeedback.Notify(this, "Password changed successfully.");
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); if (!IsDisposed) Alert("Unable to change password."); }
        finally { if (!IsDisposed) SetBusy(false); }
    }
    private async Task GenerateAsync()
    {
        if (_busy || !_ready) return;
        if (!Required(_keyPassword)) { Alert("Enter your current password."); return; }
        if (_hasKey && !ThemedMessageDialog.Confirm(FindForm(), "Regenerate Passkey", "The previous passkey will become invalid. Continue?", true, "Regenerate")) return;
        SetBusy(true);
        try
        {
            var result = await _service.RegeneratePasskeyAsync(_keyPassword.Text);
            if (IsDisposed) return;
            if (!result.Success || result.Passkey is null)
            {
                if (result.Error?.StartsWith("Current password is incorrect", StringComparison.Ordinal) == true)
                    SetError(_keyPassword, "Incorrect or unavailable");
                Alert(result.Error ?? "Unable to generate passkey."); return;
            }
            _keyPassword.Text = "";
            _keyPassword.HidePassword();
            ThemedMessageDialog.ShowRecoveryPasskey(FindForm(), result.Passkey);
            await RefreshProfileAsync();
            if (!IsDisposed) CatalogActionFeedback.Notify(this, "Recovery passkey generated.");
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); if (!IsDisposed) Alert("Unable to generate recovery passkey."); }
        finally { if (!IsDisposed) SetBusy(false); }
    }
    protected override void ApplyTheme()
    {
        base.ApplyTheme(); var c = Theme.Colors;
        _title.Font = Theme.Title; _title.ForeColor = c.Text; _subtitle.Font = Theme.Body; _subtitle.ForeColor = c.TextMuted;
        _grid.BackColor = _left.BackColor = _right.BackColor = c.Surface;
        foreach (var label in _left.Controls.OfType<Label>().Concat(_right.Controls.OfType<Label>()))
        { label.ForeColor = c.Text; label.Font = Equals(label.Tag, "heading") || Equals(label.Tag, "value") ? Theme.BodyStrong : Theme.Body; if (Equals(label.Tag, "value")) { label.BackColor = c.SurfaceAlt; label.Invalidate(); } }
        foreach (var b in new[] { _save, _generate, _theme, _editUsername, _editName })
        { b.Font = Theme.BodyStrong; b.BackColor = c.Accent; b.ForeColor = b.IconColor = c.OnAccent; b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(c.Accent, .08f); b.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(c.Accent, .08f); }
        _theme.Text = Theme.GetDisplayName(Theme.Preset) + " · Change Theme";
        foreach (var field in _captions.Keys.ToArray()) SetError(field, _captions[field].Error);
        ResizeColumn(_left); ResizeColumn(_right);
    }
}

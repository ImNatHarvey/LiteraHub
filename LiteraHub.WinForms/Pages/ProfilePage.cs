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
    private readonly Panel _grid = new() { Height = 548 };
    private readonly List<(ThemedSectionCard Card, Control[] Fields)> _sections = new();
    private bool _arranging;
    private readonly Label[] _identity = Enumerable.Range(0, 5).Select(_ => ViewValue()).ToArray();
    private readonly Label _keyStatus = Caption("Recovery passkey status", 42);
    private readonly ThemedTextField _current = Secret(), _next = Secret(), _confirm = Secret(), _keyPassword = Secret();
    private readonly CenteredIconButton _save = Action("Change Password", IconChar.Lock), _generate = Action("Generate Passkey", IconChar.Key);
    private readonly CenteredIconButton _theme = Action("Choose Theme", IconChar.Palette);
    private readonly CenteredIconButton _editUsername = Action("Change Username", IconChar.UserAlt);
    private readonly CenteredIconButton _editName = Action("Edit Name", IconChar.UserAlt);
    public event EventHandler? UsernameChanged;
    private bool _busy, _ready;
    public bool IsBusy => _busy;
    private readonly Dictionary<ThemedTextField, (Label Caption, string Name, string Error)> _captions = new();
    private bool _hasKey;
    public ProfilePage(IProfileService service, bool studentMode = false)
    {
        _service = service;
        if (studentMode) Disposed += (_, _) => _editName.Dispose();
        Padding = new Padding(Space.L, Space.L, 0, Space.L);
        _surface.ReservedRightGutter = Space.L;
        _surface.SetContent(_grid);
        Controls.Add(_surface); Controls.Add(_subtitle); Controls.Add(_title);
        var identityFields = new List<Control>();
        string[] names = { "Username", "Full Name", "Assigned role", "Account ID / Status", "Created" };
        for (int i = 0; i < names.Length; i++)
        {
            identityFields.Add(Caption(names[i])); identityFields.Add(_identity[i]);
            if (i == 0) identityFields.Add(_editUsername);
            if (i == 1 && !studentMode) identityFields.Add(_editName);
            else if (i == 1) { _editName.Visible = false; }
        }
        AddSection("Account Information", IconChar.UserAlt, identityFields.ToArray());
        AddSection("Appearance", IconChar.Palette, _theme);
        AddSection("Change Password", IconChar.Lock, FieldCaption(_current, "Current password"), _current, FieldCaption(_next, "New password"), _next, FieldCaption(_confirm, "Confirm new password"), _confirm, _save);
        AddSection("Recovery Passkey", IconChar.Key, _keyStatus, FieldCaption(_keyPassword, "Confirm current password"), _keyPassword, _generate);
        _surface.Resize += (_, _) => ResizeContent();
        _grid.Resize += (_, _) => ResizeContent();
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
        Load += async (_, _) => { ResizeContent(); await RefreshProfileAsync(); };
    }
    private void AddSection(string title, IconChar icon, params Control[] fields)
    {
        var heading = Heading(title);
        var card = new ThemedSectionCard(heading, icon);
        card.AddFields(fields);
        _sections.Add((card, fields));
        _grid.Controls.Add(card);
    }
    private static Label Caption(string text, int height = 22) => new() { Text = text, UseMnemonic = false, Height = height, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 0, 0, 1) };
    private static Label Heading(string text) { var label = Caption(text, 24); label.Tag = "heading"; return label; }
    private static ThemedTextField Secret() => new() { ShowPasswordToggle = true, Height = 36, MaxLength = 72, Margin = new Padding(0, 0, 0, 3) };
    private static CenteredIconButton Action(string text, IconChar icon) => new() { Text = text, IconChar = icon, IconSize = 18, Height = 40, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 3, 0, 3) };
    private void ResizeContent()
    {
        if (_arranging || _sections.Count != 4 || _surface.ClientSize.Width <= Space.L) return;
        _arranging = true;
        try
        {
            int w = Math.Max(1, _surface.ClientSize.Width - Space.L);
            var layout = new ResponsiveSectionLayout(w);
            int gap = layout.Gap, pad = layout.Padding;
            // Three independent columns: account, password, appearance/recovery.
            // Retain a narrower fallback for high DPI or smaller content hosts.
            int columns = w >= 3 * 280 + 2 * gap ? 3 : w >= 2 * 280 + gap ? 2 : 1;
            int usable = w - (columns - 1) * gap;
            int ColumnX(int c) => c * gap + usable * c / columns;
            int ColumnWidth(int c) => usable * (c + 1) / columns - usable * c / columns;
            int[][] order = columns == 3
                ? new[] { new[] { 0 }, new[] { 2 }, new[] { 1, 3 } }
                : columns == 2
                    ? new[] { new[] { 0, 2 }, new[] { 1, 3 } }
                    : new[] { new[] { 0, 1, 2, 3 } };
            int end = 0;
            for (int c = 0; c < columns; c++)
            {
                int width = ColumnWidth(c);
                int inner = Math.Max(1, width - 2 * pad);
                int top = 0;
                foreach (int i in order[c])
                {
                    var section = _sections[i];
                    int y = section.Card.ArrangeHeading(width, pad);
                    foreach (Control field in section.Fields)
                    {
                        int height = field.Height;
                        if (field == _keyStatus)
                            height = Math.Max(42, TextRenderer.MeasureText(field.Text, Theme.Body,
                                new Size(inner, int.MaxValue), TextFormatFlags.WordBreak).Height + 8);
                        else if (field is Label label && _captions.Values.Any(entry => entry.Caption == label))
                            height = Math.Max(22, TextRenderer.MeasureText(label.Text, Theme.Body,
                                new Size(inner, int.MaxValue), TextFormatFlags.WordBreak).Height + 2);
                        field.SetBounds(pad, y + field.Margin.Top, inner, height);
                        y += height + field.Margin.Vertical;
                    }
                    int cardHeight = y + pad;
                    section.Card.SetBounds(ColumnX(c), top, width, cardHeight);
                    top += cardHeight + gap;
                }
                end = Math.Max(end, top - gap);
            }
            _grid.Height = Math.Max(1, end + 2);
            _surface.RefreshMetrics();
        }
        finally { _arranging = false; }
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
        ResizeContent();
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
            _identity[2].Text = LiteraHub.Core.Security.StudentAccountPolicy.DisplayRole(info.RoleName);
            _identity[3].Text = $"{info.UserId} · Active";
            _identity[4].Text = info.CreatedAt.ToString("MMMM d, yyyy");
            _hasKey = info.HasRecoveryPasskey;
            _keyStatus.Text = _hasKey ? $"Passkey configured · {info.RecoveryPasskeyCreatedAt:MMMM d, yyyy}. The secret cannot be displayed again." : "No recovery passkey configured. Generate one and keep it somewhere safe.";
            _generate.Text = _hasKey ? "Regenerate Passkey" : "Generate Passkey";
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); if (!IsDisposed) { _ready = false; _identity[0].Text = "Unable to load account information."; } }
        finally { if (!IsDisposed) { SetBusy(_busy); ResizeContent(); } }
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
        _grid.BackColor = c.Background;
        foreach (var label in _sections.SelectMany(section => section.Fields).OfType<Label>())
        { label.ForeColor = c.Text; label.Font = Equals(label.Tag, "heading") || Equals(label.Tag, "value") ? Theme.BodyStrong : Theme.Body; if (Equals(label.Tag, "value")) { label.BackColor = c.SurfaceAlt; label.Invalidate(); } }
        foreach (var b in new[] { _save, _generate, _theme, _editUsername, _editName })
        { b.Font = Theme.BodyStrong; b.BackColor = c.Accent; b.ForeColor = b.IconColor = c.OnAccent; b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(c.Accent, .08f); b.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(c.Accent, .08f); }
        _theme.Text = Theme.GetDisplayName(Theme.Preset) + " · Change Theme";
        foreach (var field in _captions.Keys.ToArray()) SetError(field, _captions[field].Error);
        ResizeContent();
    }
}

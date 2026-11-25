using System.Drawing.Drawing2D;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

/// <summary>In-shell Add / View / Edit staff account, never a modal editor.</summary>
public sealed class UserDetailsPage : ThemedUserControl
{
    private readonly IStaffUserService _service;
    private StaffUser? _user;
    private readonly int? _currentUserId;
    private bool IsOwnAccount => _user is not null && _user.UserId == _currentUserId;
    private readonly Label _securitySection = new() { Text = "Account Security" };
    private readonly Label _createdLabel = new() { Text = "Created" };
    private readonly Label _createdValue = MakeView();
    private readonly Label _viewUsername = MakeView();
    private readonly Label _viewRole = MakeView();
    private readonly Label _passwordLabel = new() { Text = "Password status" };
    private readonly Label _passwordValue = MakeView();
    private readonly TableLayoutPanel _root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly DetailsPageHeader _header = new() { Dock = DockStyle.Fill };
    private readonly Panel _surface = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly DetailsActionFooter _footer = new() { Dock = DockStyle.Fill, UseStudentSizing = true };
    private readonly Label _section = new() { Text = "Account Information" };
    private readonly Label _usernameLabel = new() { Text = "Username" };
    private readonly CatalogValidationController _validation = new();
    private bool _validationAttempted;
    private readonly ThemedTextField _username = new() { MaxLength = 50 };
    private readonly Label _roleLabel = new() { Text = "Role" };
    private readonly ThemedFilterDropdown _role = new(new[] { "Select Role", "Admin", "Librarian" })
    {
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(12, 0, 8, 0)
    };

    private readonly Label _status = new() { Text = "Status" };
    private readonly Label _statusValue = MakeView();
    private readonly Label _passwordHint = new()
    {
        Text = "New accounts receive a secure temporary password. The staff member must change it upon first sign-in.",
        AutoSize = false
    };
    private readonly Button _cancel = MakeButton("Cancel");
    private readonly Button _save = MakeButton("Save User");
    private readonly Button _archive = MakeButton("Archive");
    private readonly Button _delete = MakeButton("Delete");
    private bool _editing;
    private bool _busy;
    public event EventHandler? BackRequested;

    public UserDetailsPage(IStaffUserService service, StaffUser? user, int? currentUserId = null)
    {
        _service = service;
        _currentUserId = currentUserId;
        _user = user;
        _editing = user is null;
        Dock = DockStyle.Fill;
        Padding = new Padding(Space.L);
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        _root.Controls.Add(_header, 0, 0);
        _root.Controls.Add(_surface, 0, 1);
        _root.Controls.Add(_footer, 0, 2);
        _surface.Controls.AddRange(new Control[] { _section, _usernameLabel, _username,
            _roleLabel, _role, _status, _statusValue, _passwordHint, _securitySection, _createdLabel, _createdValue, _passwordLabel, _passwordValue, _viewUsername, _viewRole });
        _footer.Controls.AddRange(new Control[] { _cancel, _save, _archive, _delete });
        _footer.Configure(_delete, _archive, _save, _cancel, () => _user is null);
        _delete.Visible = false;
        Controls.Add(_root);
        _username.Text = user?.Username ?? "";
        _role.SelectedIndex = user is null ? 0 : user.RoleName == "Admin" ? 1 : 2;
        _statusValue.Text = user is null ? "Active on creation" : (user.IsActive ? "Active" : "Inactive");
        _header.BackRequested += (_, _) => { if (!_busy) BackRequested?.Invoke(this, EventArgs.Empty); };
        _validation.Register(_username, () => IsValidUsername(_username.Text.Trim()),
            "Username is required and must contain 3–50 letters, digits, periods, underscores or hyphens.");
        _validation.Register(_role, () => _role.SelectedIndex > 0, "Role is required.");
        _username.TextChanged += (_, _) => RefreshValidationCaptions();
        _role.SelectedIndexChanged += (_, _) => RefreshValidationCaptions();
        Disposed += (_, _) => _validation.Dispose();
        _cancel.Click += (_, _) =>
        {
            if (_busy) return;
            if (_user is null) BackRequested?.Invoke(this, EventArgs.Empty);
            else { Restore(); SetEditing(false); }
        };
        _save.Click += async (_, _) =>
        {
            if (_busy || IsOwnAccount) return;
            if (!_editing) { SetEditing(true); return; }
            await SaveAsync();
        };
        _archive.Click += async (_, _) => await ChangeStatusAsync();
        _delete.Click += async (_, _) => await DeleteAsync();
        _delete.Paint += (_, e) => DrawAction(e, _delete, 2);
        _delete.AccessibleName = "Delete User";
        _delete.Text = "";
        _save.Paint += (_, e) => DrawAction(e, _save, 0);
        _archive.Paint += (_, e) => DrawAction(e, _archive, 1);
        _surface.Resize += (_, _) => Arrange();
        SetEditing(_editing);
        ApplyTheme();
        Arrange();
    }

    private static Label MakeView()
    {
        var view = new Label
        {
            AutoSize = false,
            TabStop = false,
            UseMnemonic = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 3, 12, 3),
            BorderStyle = BorderStyle.None,
            AutoEllipsis = true
        };
        view.Paint += (_, e) => {
            using var pen = new Pen(Theme.Colors.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, view.Width - 1), Math.Max(0, view.Height - 1));
        };
        return view;
    }

    private static Button MakeButton(string text) => new()
    {
        Text = text,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        Margin = Padding.Empty,
        TextAlign = ContentAlignment.MiddleCenter
    };

    private void Restore()
    {
        _username.Text = _user?.Username ?? "";
        _role.SelectedIndex = _user is null ? 0 : _user.RoleName == "Admin" ? 1 : 2;
        _validationAttempted = false;
        _validation.Clear();
        RefreshValidationCaptions();
    }

    private void SetEditing(bool editing)
    {
        _editing = editing;
        _username.ReadOnly = false;
        _username.Visible = editing;
        _username.TabStop = editing;
        _role.Visible = editing;
        _role.Enabled = editing;
        _viewUsername.Text = _username.Text;
        _viewRole.Text = _user?.RoleName ?? "Not specified";
        _viewUsername.Visible = !editing;
        _viewRole.Visible = !editing;
        if (!editing) { _viewUsername.BringToFront(); _viewRole.BringToFront(); }
        _save.Text = "";
        _save.AccessibleName = editing ? "Save User" : "Edit User";
        _archive.Visible = _user is not null && !IsOwnAccount;
        _save.Visible = !IsOwnAccount;
        _delete.Visible = _user is not null && !IsOwnAccount;
        _archive.Text = "";
        _archive.AccessibleName = _user?.IsActive == false ? "Restore User" : "Archive User";
        _cancel.Visible = editing;
        _passwordHint.Visible = _user is null || IsOwnAccount;
        _passwordHint.Text = IsOwnAccount ? "This is your signed-in account. Account changes will be available in Profile Settings." : "New accounts receive a secure temporary password. The staff member must change it upon first sign-in.";
        _createdValue.Text = _user is null ? "On creation" : _user.CreatedAt.ToString("MMMM d, yyyy");
        _passwordValue.Text = _user is null || _user.MustChangePassword ? "Change required on first sign-in" : "Password configured";
        _statusValue.Text = _user is null ? "Active on creation" : _user.IsActive ? "Active" : "Inactive";
        _header.HeadingText = _user is null ? "Add User" : editing ? "Edit User" : "User Details";
        _header.SubtitleText = _user is null ? "Create a staff account." : $"User ID: {_user.UserId} · {_user.RoleName}";
        _footer.ArrangeActions();
        ApplyTheme();
    }

    private static bool IsValidUsername(string username) =>
        username.Length is >= 3 and <= 50 &&
        username.All(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-');

    private void RefreshValidationCaptions()
    {
        if (IsDisposed || Disposing) return;
        bool badName = _validationAttempted && _username.HasValidationError;
        bool badRole = _validationAttempted && _role.SelectedIndex == 0;
        _usernameLabel.Text = badName ? (_username.Text.Trim().Length == 0 ? "Username • Required" : "Username • Invalid username") : "Username";
        _roleLabel.Text = badRole ? "Role • Required" : "Role";
        _usernameLabel.ForeColor = badName ? Theme.Colors.Danger : Theme.Colors.Text;
        _roleLabel.ForeColor = badRole ? Theme.Colors.Danger : Theme.Colors.Text;
        _usernameLabel.AutoEllipsis = true;
        _roleLabel.AutoEllipsis = true;
        _role.FlatAppearance.BorderSize = badRole ? 2 : 1;
        _role.FlatAppearance.BorderColor = badRole ? Theme.Colors.Danger : Theme.Colors.Border;
        _role.ForeColor = badRole ? Theme.Colors.Danger : Theme.Colors.Text;
        _role.Invalidate();
    }

    private async Task SaveAsync()
    {
        string username = _username.Text.Trim();
        _validationAttempted = true;
        var errors = _validation.Validate();
        RefreshValidationCaptions();
        if (errors.Count > 0)
        {
            _validation.ShowSummary(FindForm(), "this user");
            if (!IsDisposed && !Disposing)
            {
                if (!IsValidUsername(username)) _username.FocusInput();
                else _validation.FocusFirstInvalid();
            }
            return;
        }
        _busy = true;
        _save.Enabled = false;
        _header.BackEnabled = false;
        try
        {
            var input = new StaffUserInput(username, _role.SelectedIndex == 1 ? "Admin" : "Librarian");
            if (_user is null)
            {
                StaffCreationResult result = await _service.CreateAsync(input);
                if (!result.Success) { ThemedMessageDialog.ShowAlert(FindForm(), "Add User", result.Message, true); return; }
                // The generated temporary password is shown only here and is not persisted in plaintext.
                ThemedMessageDialog.ShowTemporaryPassword(FindForm(), username, result.TemporaryPassword ?? "");
                CatalogActionFeedback.Notify(this, "User added successfully.");
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ServiceResult result = await _service.UpdateAsync(_user.UserId, input);
                if (!result.Success) { ThemedMessageDialog.ShowAlert(FindForm(), "Edit User", result.Error ?? "Unable to update this account.", true); return; }
                _user = await _service.GetByIdAsync(_user.UserId);
                Restore();
                SetEditing(false);
                CatalogActionFeedback.Notify(this, "User updated successfully.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ThemedMessageDialog.ShowAlert(FindForm(), "Users", "Unable to save the account.", true);
        }
        finally { _busy = false; if (!IsDisposed) { _save.Enabled = true; _header.BackEnabled = true; } }
    }

    private async Task ChangeStatusAsync()
    {
        if (_busy || _user is null || IsOwnAccount) return;
        bool desired = !_user.IsActive;
        string action = desired ? "Restore" : "Archive";
        if (!ThemedMessageDialog.Confirm(FindForm(), action + " account",
            $"{action} staff account '{_user.Username}'?", danger: !desired, confirmText: action)) return;
        _busy = true;
        try
        {
            ServiceResult result = await _service.SetActiveAsync(_user.UserId, desired);
            if (!result.Success) { ThemedMessageDialog.ShowAlert(FindForm(), "Users", result.Error ?? "Unable to change account status.", true); return; }
            _user = await _service.GetByIdAsync(_user.UserId);
            Restore();
            SetEditing(false);
            CatalogActionFeedback.Notify(this, desired ? "User restored successfully." : "User archived successfully.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ThemedMessageDialog.ShowAlert(FindForm(), "Users", "Unable to change account status.", true);
        }
        finally { _busy = false; }
    }

    private async Task DeleteAsync()
    {
        if (_busy || _user is null || IsOwnAccount) return;
        if (_user.IsActive) { ThemedMessageDialog.ShowAlert(FindForm(), "Delete User", "Archive this account before permanently deleting it.", true); return; }
        if (!ThemedMessageDialog.Confirm(FindForm(), "Delete User", $"Permanently delete '{_user.Username}'? This cannot be undone.", true, "Delete")) return;
        _busy = true;
        try
        {
            var result = await _service.DeleteAsync(_user.UserId);
            if (!result.Success) { ThemedMessageDialog.ShowAlert(FindForm(), "Delete User", result.Error ?? "Unable to delete account.", true); return; }
            CatalogActionFeedback.Notify(this, "User deleted successfully.");
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); ThemedMessageDialog.ShowAlert(FindForm(), "Users", "Unable to delete account.", true); }
        finally { _busy = false; }
    }

    private void Arrange()
    {
        if (_surface.Width < 1) return;
        int left = 16;
        int gap = 24;
        int column = Math.Max(1, (_surface.ClientSize.Width - 2 * left - gap) / 2);
        int right = left + column + gap;
        _section.SetBounds(left + 4, 16, column - 4, 24);
        _securitySection.SetBounds(right + 4, 16, column - 4, 24);
        _usernameLabel.SetBounds(left + 4, 52, column - 4, 22);
        _username.SetBounds(left, 76, column, 40);
        _roleLabel.SetBounds(left + 4, 122, column - 4, 22);
        _role.SetBounds(left, 146, column, 40);
        _viewUsername.Bounds = _username.Bounds;
        _viewRole.Bounds = _role.Bounds;
        _status.SetBounds(left + 4, 192, column, 22);
        _statusValue.SetBounds(left, 216, column, 40);
        _createdLabel.SetBounds(right + 4, 52, column, 22);
        _createdValue.SetBounds(right, 76, column, 40);
        _passwordLabel.SetBounds(right + 4, 122, column, 22);
        _passwordValue.SetBounds(right, 146, column, 40);
        _passwordHint.SetBounds(right + 4, 200, column - 4, 90);
    }

    private void DrawAction(PaintEventArgs e, Button button, int kind)
    {
        if (button.Width < 90 || button.Height < 24) return;
        var c = Theme.Colors;
        var g = e.Graphics;
        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color iconColor = kind == 2 ? Color.White
            : kind == 0 ? Color.FromArgb(118, 235, 173)
            : (_user?.IsActive == false ? Color.FromArgb(118, 235, 173) : c.TextMuted);
        if (!button.Enabled) iconColor = c.TextMuted;

        string label = kind == 0 ? (_editing ? "Save User" : "Edit User")
            : kind == 2 ? "Delete User"
            : (_user?.IsActive == false ? "Restore User" : "Archive User");
        if (button.Width < 130)
            label = kind == 0 ? (_editing ? "Save" : "Edit")
                : kind == 2 ? "Delete" : (_user?.IsActive == false ? "Restore" : "Archive");

        Size labelSize = TextRenderer.MeasureText(label, button.Font);
        int cx = Math.Max(13, (button.Width - labelSize.Width - 30) / 2 + 9);
        int cy = button.Height / 2;
        using var pen = new Pen(iconColor, 2f);
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
            bool restoring = _user?.IsActive == false;
            g.DrawLine(pen, cx, restoring ? cy + 5 : cy - 2, cx, restoring ? cy - 2 : cy + 5);
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

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        BackColor = c.Background;
        _root.BackColor = c.Background;
        _surface.BackColor = c.Surface;
        _section.ForeColor = c.Text;
        _section.Font = Theme.BodyStrong;
        _securitySection.ForeColor = c.Text;
        _securitySection.Font = Theme.BodyStrong;
        foreach (var label in new[] { _usernameLabel, _roleLabel, _status, _statusValue, _createdLabel, _createdValue, _passwordLabel, _passwordValue })
        {
            label.ForeColor = c.Text;
            label.Font = Theme.Body;
        }
        foreach (var view in new[] { _viewUsername, _viewRole, _statusValue, _createdValue, _passwordValue })
        { view.BackColor = c.SurfaceAlt; view.ForeColor = c.Text; view.Font = Theme.BodyStrong; view.Invalidate(); }
        _role.ApplyTheme();
        RefreshValidationCaptions();
        _delete.BackColor = Color.FromArgb(255, 143, 137);
        _delete.ForeColor = Color.White;
        _delete.Font = Theme.BodyStrong;
        _delete.FlatAppearance.BorderSize = 0;
        _delete.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(_delete.BackColor, 0.12f);
        _delete.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(_delete.BackColor, 0.23f);
        _passwordHint.ForeColor = c.TextMuted;
        _passwordHint.Font = Theme.Body;
        foreach (var b in new[] { _save, _archive, _cancel })
        {
            b.Font = Theme.BodyStrong;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = c.Border;
        }
        _save.BackColor = Color.FromArgb(34, 139, 86);
        _save.ForeColor = Color.White;
        _save.FlatAppearance.BorderSize = 0;
        _save.FlatAppearance.MouseOverBackColor = ControlPaint.Light(_save.BackColor, 0.08f);
        _save.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(_save.BackColor, 0.08f);
        _archive.BackColor = c.Surface;
        _archive.ForeColor = c.Text;
        _archive.FlatAppearance.MouseOverBackColor = c.SurfaceAlt;
        _archive.FlatAppearance.MouseDownBackColor = c.AccentSoft;
        _cancel.BackColor = c.SurfaceAlt;
        _cancel.FlatAppearance.MouseOverBackColor = c.AccentSoft;
        _footer.BackColor = c.Background;
        _cancel.ForeColor = c.Text;
        _header.ApplyTheme();
        _footer.ArrangeActions();
        if (IsOwnAccount) { _save.Visible = false; _archive.Visible = false; _delete.Visible = false; }
        _save.Invalidate();
        _archive.Invalidate();
        _delete.Invalidate();
    }
}

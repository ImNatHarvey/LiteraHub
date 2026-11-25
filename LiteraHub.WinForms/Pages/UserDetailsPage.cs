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
    private readonly TableLayoutPanel _root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
    private readonly DetailsPageHeader _header = new();
    private readonly Panel _surface = new() { Dock = DockStyle.Fill };
    private readonly DetailsActionFooter _footer = new() { Dock = DockStyle.Fill, UseStudentSizing = true };
    private readonly Label _section = new() { Text = "Account Information" };
    private readonly Label _usernameLabel = new() { Text = "Username" };
    private readonly Label _usernameError = new() { Text = "Required", Visible = false, TextAlign = ContentAlignment.MiddleRight };
    private readonly ThemedTextField _username = new() { MaxLength = 50 };
    private readonly Label _roleLabel = new() { Text = "Role" };
    private readonly ThemedFilterDropdown _role = new(new[] { "Select Role", "Admin", "Librarian" }) { TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _roleError = new() { Text = "Required", Visible = false, TextAlign = ContentAlignment.MiddleRight };
    private readonly Label _status = new() { Text = "Status" };
    private readonly Label _statusValue = new() { Text = "Active" };
    private readonly Label _passwordHint = new()
    {
        Text = "New accounts receive a secure temporary password. The staff member must change it upon first sign-in.",
        AutoSize = false
    };
    private readonly Button _cancel = MakeButton("Cancel");
    private readonly Button _save = MakeButton("Save User");
    private readonly Button _archive = MakeButton("Archive");
    private readonly Button _hiddenDelete = MakeButton("Delete");
    private bool _editing;
    private bool _busy;
    public event EventHandler? BackRequested;

    public UserDetailsPage(IStaffUserService service, StaffUser? user)
    {
        _service = service;
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
        _surface.Controls.AddRange(new Control[] { _section, _usernameLabel, _usernameError, _username,
            _roleLabel, _roleError, _role, _status, _statusValue, _passwordHint });
        _footer.Controls.AddRange(new Control[] { _cancel, _save, _archive, _hiddenDelete });
        _footer.Configure(_hiddenDelete, _archive, _save, _cancel, () => _user is null);
        _hiddenDelete.Visible = false; // No hard delete for staff accounts.
        Controls.Add(_root);
        _username.Text = user?.Username ?? "";
        _role.SelectedIndex = user is null ? 0 : user.RoleName == "Admin" ? 1 : 2;
        _statusValue.Text = user is null ? "Active on creation" : (user.IsActive ? "Active" : "Inactive");
        _header.BackRequested += (_, _) => { if (!_busy) BackRequested?.Invoke(this, EventArgs.Empty); };
        _username.TextChanged += (_, _) => { _username.HasValidationError = false; _usernameError.Visible = false; };
        _role.SelectedIndexChanged += (_, _) => _roleError.Visible = false;
        _cancel.Click += (_, _) =>
        {
            if (_busy) return;
            if (_user is null) BackRequested?.Invoke(this, EventArgs.Empty);
            else { Restore(); SetEditing(false); }
        };
        _save.Click += async (_, _) =>
        {
            if (_busy) return;
            if (!_editing) { SetEditing(true); return; }
            await SaveAsync();
        };
        _archive.Click += async (_, _) => await ChangeStatusAsync();
        _surface.Resize += (_, _) => Arrange();
        SetEditing(_editing);
        ApplyTheme();
        Arrange();
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
        _username.HasValidationError = false;
        _usernameError.Visible = false;
        _roleError.Visible = false;
    }

    private void SetEditing(bool editing)
    {
        _editing = editing;
        _username.ReadOnly = !editing;
        _role.Enabled = editing;
        _save.Text = _user is null ? "Save User" : editing ? "Save Changes" : "Edit User";
        _archive.Visible = _user is not null && !editing;
        _archive.Text = _user?.IsActive == false ? "Restore" : "Archive";
        _cancel.Visible = editing;
        _passwordHint.Visible = _user is null;
        _statusValue.Text = _user is null ? "Active on creation" : _user.IsActive ? "Active" : "Inactive";
        _header.HeadingText = _user is null ? "Add User" : editing ? "Edit User" : "View User";
        _header.SubtitleText = _user is null ? "Create a staff account" : $"User ID: {_user.UserId} · {_user.RoleName}";
        _footer.ArrangeActions();
        ApplyTheme();
    }

    private async Task SaveAsync()
    {
        string username = _username.Text.Trim();
        bool badName = username.Length is < 3 or > 50 || username.Any(c => !char.IsLetterOrDigit(c) && c is not ('.' or '_' or '-'));
        _username.HasValidationError = badName;
        _usernameError.Text = username.Length == 0 ? "Required" : "Invalid username";
        _usernameError.Visible = badName;
        _roleError.Visible = _role.SelectedIndex == 0;
        if (badName || _roleError.Visible) return;
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
                ThemedMessageDialog.ShowAlert(FindForm(), "Staff account created",
                    $"Username: {username}\nTemporary password: {result.TemporaryPassword}\n\nGive this password securely to the account owner. It will not be shown again. The account requires a password change at first sign-in.");
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ServiceResult result = await _service.UpdateAsync(_user.UserId, input);
                if (!result.Success) { ThemedMessageDialog.ShowAlert(FindForm(), "Edit User", result.Message, true); return; }
                _user = await _service.GetByIdAsync(_user.UserId);
                Restore();
                SetEditing(false);
                ThemedMessageDialog.ShowAlert(FindForm(), "User updated", "Staff account changes saved.");
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
        if (_busy || _user is null) return;
        bool desired = !_user.IsActive;
        string action = desired ? "Restore" : "Archive";
        if (!ThemedMessageDialog.Confirm(FindForm(), action + " account",
            $"{action} staff account '{_user.Username}'?", danger: !desired, confirmText: action)) return;
        _busy = true;
        try
        {
            ServiceResult result = await _service.SetActiveAsync(_user.UserId, desired);
            if (!result.Success) { ThemedMessageDialog.ShowAlert(FindForm(), "Users", result.Message, true); return; }
            _user = await _service.GetByIdAsync(_user.UserId);
            SetEditing(false);
            ThemedMessageDialog.ShowAlert(FindForm(), "Users", $"Account {action.ToLowerInvariant()}d successfully.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ThemedMessageDialog.ShowAlert(FindForm(), "Users", "Unable to change account status.", true);
        }
        finally { _busy = false; }
    }

    private void Arrange()
    {
        if (_surface.Width < 1) return;
        int available = Math.Max(200, _surface.ClientSize.Width - 40);
        int column = Math.Min(420, available);
        int left = 16;
        int top = 18;
        _section.SetBounds(left, top, column, 30);
        top += 50;
        _usernameLabel.SetBounds(left, top, column / 2, 22);
        _usernameError.SetBounds(left + column / 2, top, column / 2, 22);
        top += 27;
        _username.SetBounds(left, top, column, 40);
        top += 62;
        _roleLabel.SetBounds(left, top, column / 2, 22);
        _roleError.SetBounds(left + column / 2, top, column / 2, 22);
        top += 27;
        _role.SetBounds(left, top, column, 40);
        top += 64;
        _status.SetBounds(left, top, 85, 24);
        _statusValue.SetBounds(left + 95, top, column - 95, 24);
        _passwordHint.SetBounds(left, top + 50, column, 72);
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        BackColor = c.Background;
        _root.BackColor = c.Background;
        _surface.BackColor = c.Surface;
        _section.ForeColor = c.Text;
        _section.Font = Theme.Title;
        foreach (var label in new[] { _usernameLabel, _roleLabel, _status, _statusValue })
        {
            label.ForeColor = c.Text;
            label.Font = Theme.Body;
        }
        foreach (var label in new[] { _usernameError, _roleError })
        {
            label.ForeColor = c.Danger;
            label.Font = Theme.Caption;
        }
        _passwordHint.ForeColor = c.TextMuted;
        _passwordHint.Font = Theme.Body;
        foreach (var b in new[] { _save, _archive, _cancel })
        {
            b.Font = Theme.BodyStrong;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = c.Border;
        }
        _save.BackColor = c.Accent;
        _save.ForeColor = c.OnAccent;
        _archive.BackColor = c.Surface;
        _archive.ForeColor = c.Text;
        _cancel.BackColor = c.Surface;
        _cancel.ForeColor = c.Text;
        _header.ApplyTheme();
        _footer.ArrangeActions();
    }
}

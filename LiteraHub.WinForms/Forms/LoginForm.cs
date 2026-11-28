using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;
using LiteraHub.Data.Repositories;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

/// <summary>
/// Two flush themed panels with an inline password recovery view.
/// Forgot Password swaps the form and carousel using the existing animation.
/// </summary>
public sealed class LoginForm : Form
{
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    private readonly IAuthService _auth;
    private readonly IRecoveryService _recovery;
    private readonly StudentAccountRepository? _studentAccounts;
    private bool _registrationMode;
    private readonly Panel _registrationView = new();
    private readonly Button _register = new() { Text = "Student Registration", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
    private readonly Label _registerTitle = new() { Text = "Student Registration", TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _registerDescription = new() { Text = "Enter the library-issued Student ID (e.g. STU-123456789). If you do not know it, ask library staff. Identity and enrollment must be verified before credentials are issued." };
    private readonly Label _registerStudentLabel = new() { Text = "Student ID" };
    private readonly ThemedTextField _registerStudent = new() { MaxLength = 30, LeadingIcon = IconChar.UserAlt };
    private readonly Label _registerStatus = new() { AutoSize = false, UseMnemonic = false };
    private readonly Button _registerSubmit = new() { Text = "Submit Request", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
    private readonly Button _registerBack = new() { Text = "Back to Sign In", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
    private readonly string[] _recoveryNames = { "Username or Student ID", "Recovery passkey", "New password", "Confirm new password" };
    private readonly Label[] _recoveryLabels = new Label[4];
    private readonly ThemedTextField[] _recoveryFields = new ThemedTextField[4];
    private bool _unlockMode;
    private readonly Button _recoveryToggle = new() { Text = "Unlock instead", Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
    private readonly System.Windows.Forms.Timer _lockoutTimer = new() { Interval = 250 };
    private readonly System.Diagnostics.Stopwatch _lockoutClock = new();
    private int _lockoutSeconds;
    private readonly Button _reset = new() { Text = "Reset Password", Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
    private readonly Label _recoveryStatus = new() { TextAlign = ContentAlignment.MiddleLeft };
    private bool _recoveryError;
    private readonly Panel _accountPanel = new();
    private readonly LoginCarouselControl _carousel = new();
    private readonly ThemedWindowControls _windowControls = new() { ShowMaximizeButton = false, ShowMinimizeButton = false };
    private readonly Panel _loginView = new();
    private readonly Panel _recoveryView = new();
    private readonly PictureBox _brandLogo = new() { SizeMode = PictureBoxSizeMode.Zoom, TabStop = false };
    private readonly Label _welcome = new() { Text = "LiteraHub", TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _subtitle = new() { Text = "Sign in to LiteraHub.", TextAlign = ContentAlignment.MiddleCenter };
    private Image? _logoOriginal;
    private Image? _logoThemed;
    private readonly Label _usernameTitle = new() { Text = "Username or Student ID" };
    private readonly Label _usernameHint = new() { Text = "Required", Visible = false, TextAlign = ContentAlignment.MiddleRight };
    private readonly Label _passwordTitle = new() { Text = "Password" };
    private readonly Label _passwordHint = new() { Text = "Required", Visible = false, TextAlign = ContentAlignment.MiddleRight };
    private readonly ThemedTextField _username = new() { TabIndex = 0, LeadingIcon = IconChar.UserAlt };
    private readonly Panel _passwordFrame = new();
    private readonly IconPictureBox _passwordLeadingIcon = new()
    {
        IconChar = IconChar.Lock,
        IconSize = 12,
        TabStop = false,
        SizeMode = PictureBoxSizeMode.CenterImage
    };
    private readonly TextBox _password = new() { BorderStyle = BorderStyle.None, UseSystemPasswordChar = true, TabIndex = 1 };
    private readonly IconButton _eye = new()
    {
        IconChar = IconChar.Eye,
        IconSize = 17,
        TabStop = false,
        Cursor = Cursors.Hand,
        FlatStyle = FlatStyle.Flat,
        AccessibleName = "Show password"
    };
    private readonly Button _forgot = new() { Text = "Recover account?", Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };

    private readonly Button _signIn = new() { Text = "Sign In", Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
    private readonly Label _footer = new() { Text = "LiteraHub  •  STI Balagtas", TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _recoveryTitle = new() { Text = "Reset Password", TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _recoveryDescription = new()
    {
        Text = "Use your recovery passkey to reset your password.",
        TextAlign = ContentAlignment.MiddleCenter
    };
    private readonly Button _back = new() { Text = "Back to Sign In", Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
    private readonly System.Windows.Forms.Timer _slide = new() { Interval = 15 };

    private readonly Panel _changeView = new();
    private readonly Label _changeTitle = new() { Text = "Set a new password", TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _changeIntro = new() { TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _changePolicy = new() { Text = "At least 8 characters, with a letter and a number.", TextAlign = ContentAlignment.TopLeft };
    private readonly Label _changeStatus = new() { TextAlign = ContentAlignment.MiddleLeft };
    private readonly string[] _changeNames = { "Current password", "New password", "Confirm new password" };
    private readonly Label[] _changeLabels = new Label[3];
    private readonly ThemedTextField[] _changeFields = new ThemedTextField[3];
    private readonly Button _changeSave = new() { Text = "Save Password", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
    private readonly Button _changeCancel = new() { Text = "Sign Out", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
    private AuthResult? _pendingLogin;
    private bool _changeError;
    private bool _recoveryMode;
    private bool _busy;
    private bool _usernameInvalid;
    private bool _passwordInvalid;
    private float _position = 1f; // 0: form left, illustration right; 1: illustration left, form right.
    private float _from;
    private float _to;
    private float _elapsed;
    private bool _layoutBusy;

    public AuthResult? Result { get; private set; }

    public LoginForm(IAuthService auth, IRecoveryService recovery, StudentAccountRepository? studentAccounts = null)
    {
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
        _studentAccounts = studentAccounts;
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        Text = "LiteraHub — Sign In";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1080, 630);
        MinimumSize = new Size(900, 580);
        MaximizeBox = false;
        MinimizeBox = true;
        DoubleBuffered = true;
        BackColor = Theme.Colors.Background;

        Controls.Add(_accountPanel);
        Controls.Add(_carousel);
        // Keep Close above BOTH moving panels and anchored to the physical
        // upper-right corner, regardless of which view is on that side.
        Controls.Add(_windowControls);
        _windowControls.Bind(this);
        _windowControls.BringToFront();

        _accountPanel.Controls.Add(_loginView);
        _accountPanel.Controls.Add(_recoveryView);
        _loginView.Controls.AddRange(new Control[]
        {
            _brandLogo, _welcome, _subtitle, _usernameTitle, _usernameHint, _username,
            _passwordTitle, _passwordHint, _passwordFrame, _forgot, _register, _signIn, _footer
        });
        _passwordFrame.Controls.Add(_password);
        _passwordFrame.Controls.Add(_passwordLeadingIcon);
        _passwordFrame.Controls.Add(_eye);
        _recoveryView.Controls.AddRange(new Control[] { _recoveryTitle, _recoveryDescription, _back, _reset, _recoveryStatus, _recoveryToggle });
        BuildRecoveryFields();
        BuildChangeView();
        _accountPanel.Controls.Add(_registrationView);
        _registrationView.Controls.AddRange(new Control[] { _registerTitle, _registerDescription, _registerStudentLabel,
            _registerStudent, _registerStatus, _registerSubmit, _registerBack });
        _registrationView.Visible = false;
        _register.Visible = _studentAccounts is not null;
        _registerStudent.TabIndex = 0; _registerSubmit.TabIndex = 1; _registerBack.TabIndex = 2;
        _username.TabIndex = 0; _passwordFrame.TabIndex = 1; _password.TabIndex = 0;
        _signIn.TabIndex = 2; _register.TabIndex = 3; _forgot.TabIndex = 4;
        _register.Click += (_, _) => SwitchRegistration(true);
        _registerBack.Click += (_, _) => SwitchRegistration(false);
        _registerSubmit.Click += async (_, _) => await SubmitRegistrationAsync();
        _registerStudent.TextChanged += (_, _) => { _registerStudent.HasValidationError = false; _registerStatus.Text = ""; };
        _registrationView.MouseDown += BeginDrag;
        _registerTitle.MouseDown += BeginDrag;
        _recoveryView.Visible = false;

        _welcome.Font = new Font("Segoe UI", 27f, FontStyle.Bold);
        _recoveryTitle.Font = new Font("Segoe UI", 24f, FontStyle.Bold);
        _subtitle.Font = Theme.Body;
        _usernameTitle.Font = Theme.Body;
        _passwordTitle.Font = Theme.Body;
        _usernameHint.Font = Theme.Caption;
        _passwordHint.Font = Theme.Caption;
        _password.Font = Theme.Body;
        _forgot.Font = Theme.Caption;

        _signIn.Font = Theme.BodyStrong;
        _back.Font = Theme.BodyStrong;
        _footer.Font = Theme.Caption;
        _recoveryDescription.Font = Theme.Body;

        _passwordFrame.Paint += PaintPasswordFrame;
        _passwordLeadingIcon.Click += (_, _) => _password.Focus();
        _username.TextChanged += (_, _) => { ClearUsernameValidation(); ClearLockout(); };
        _lockoutTimer.Tick += (_, _) => UpdateLockout();
        _recoveryToggle.Click += (_, _) => SetRecoveryMode(!_unlockMode);
        _password.TextChanged += (_, _) => ClearPasswordValidation();
        _password.Enter += (_, _) => { _passwordFrame.Invalidate(); UpdateLabels(); };
        _password.Leave += (_, _) => { _passwordFrame.Invalidate(); UpdateLabels(); };
        _eye.Click += (_, _) =>
        {
            _password.UseSystemPasswordChar = !_password.UseSystemPasswordChar;
            _eye.IconChar = _password.UseSystemPasswordChar ? IconChar.Eye : IconChar.EyeSlash;
            _eye.AccessibleName = _password.UseSystemPasswordChar ? "Show password" : "Hide password";
            _password.Focus();
        };
        _signIn.Click += async (_, _) => await SignInAsync();
        _forgot.Click += (_, _) => SwitchMode(true);
        _reset.Click += async (_, _) => await ResetPasswordAsync();
        FormClosing += (_, e) =>
        {
            if (_busy && DialogResult != DialogResult.OK) { e.Cancel = true; return; }
            if (DialogResult != DialogResult.OK) { _auth.Logout(); _pendingLogin = null; }
        };
        _back.Click += (_, _) => SwitchMode(false);
        _slide.Tick += (_, _) =>
        {
            _elapsed = Math.Min(1f, _elapsed + 0.065f);
            // Smoothstep avoids an abrupt stop at the end of the slide.
            float t = _elapsed * _elapsed * (3f - 2f * _elapsed);
            _position = _from + (_to - _from) * t;
            LayoutPanels();
            if (_elapsed >= 1f)
            {
                _slide.Stop();
                if (_registrationMode) _registerStudent.FocusInput();
                else if (_recoveryMode) _recoveryFields[0].FocusInput();
                else _username.FocusInput();
            }
        };
        _accountPanel.MouseDown += BeginDrag;
        _loginView.MouseDown += BeginDrag;
        _recoveryView.MouseDown += BeginDrag;
        _welcome.MouseDown += BeginDrag;
        _brandLogo.MouseDown += BeginDrag;
        _subtitle.MouseDown += BeginDrag;
        _recoveryTitle.MouseDown += BeginDrag;

        Resize += (_, _) => LayoutPanels();
        Theme.Changed += ThemeChanged;
        AcceptButton = _signIn;
        LoadLogo();
        ApplyTheme();
        LayoutPanels();
        Shown += (_, _) => _username.FocusInput();
    }

    private void BuildChangeView()
    {
        _accountPanel.Controls.Add(_changeView);
        _changeView.Visible = false;
        _changeTitle.Font = new Font("Segoe UI", 24f, FontStyle.Bold);
        _changeIntro.Font = Theme.Body;
        _changePolicy.Font = Theme.Caption;
        _changeStatus.Font = Theme.Caption;
        _changeStatus.TextChanged += (_, _) => LayoutPanels();
        _changeSave.Font = _changeCancel.Font = Theme.BodyStrong;
        _changeView.Controls.AddRange(new Control[] { _changeTitle, _changeIntro, _changePolicy, _changeStatus, _changeSave, _changeCancel });
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            _changeLabels[i] = new Label { Text = _changeNames[i], Font = Theme.Body };
            _changeFields[i] = new ThemedTextField { LeadingIcon = IconChar.Lock, ShowPasswordToggle = true, MaxLength = 72, TabIndex = i };
            _changeView.Controls.Add(_changeLabels[i]);
            _changeView.Controls.Add(_changeFields[i]);
            _changeFields[i].TextChanged += (_, _) => ChangeValidation(index, "");
        }
        _changeSave.TabIndex = 3;
        _changeCancel.TabIndex = 4;
        _changeSave.Click += async (_, _) => await SaveRequiredPasswordAsync();
        // This is an in-form navigation action, not a modal Cancel button.
        _changeCancel.DialogResult = DialogResult.None;
        _changeCancel.Click += (_, _) => RequestRequiredPasswordSignOut();
        _changeView.MouseDown += BeginDrag;
        _changeTitle.MouseDown += BeginDrag;
        _changeIntro.MouseDown += BeginDrag;
    }

    private void ChangeValidation(int i, string message)
    {
        _changeFields[i].HasValidationError = message.Length > 0;
        _changeLabels[i].Text = _changeNames[i] + (message.Length > 0 ? " • " + message : "");
        _changeLabels[i].ForeColor = message.Length > 0 ? Theme.Colors.Danger : Theme.Colors.Text;
    }

    private void ClearChangeFields()
    {
        for (int i = 0; i < 3; i++)
        {
            _changeFields[i].Text = "";
            _changeFields[i].HidePassword();
            ChangeValidation(i, "");
        }
        _changeError = false;
        _changeStatus.Text = "";
        _changeStatus.ForeColor = Theme.Colors.TextMuted;
    }

    private void ShowChangeView(AuthResult result)
    {
        _pendingLogin = result;
        _password.Clear();
        _password.UseSystemPasswordChar = true;
        _eye.IconChar = IconChar.Eye;
        _eye.AccessibleName = "Show password";
        ClearChangeFields();
        _changeIntro.Text = $"Signed in as {result.Session?.Username}. Choose a new password before continuing.";
        _loginView.Visible = false;
        _recoveryView.Visible = false;
        _changeView.Visible = true;
        _changeView.BringToFront();
        AcceptButton = _changeSave;
        CancelButton = null;
        _changeCancel.DialogResult = DialogResult.None;
        _changeFields[0].FocusInput();
    }

    private void RequestRequiredPasswordSignOut()
    {
        if (_busy || _pendingLogin is null) return;
        bool confirmed = ThemedMessageDialog.Confirm(this, "Sign out?",
            "Your password has not been changed. You will return to Sign In and must set a new password the next time you sign in. Continue?",
            danger: true, confirmText: "Sign Out");
        if (confirmed)
            CancelRequiredPassword();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape && _registrationMode) { SwitchRegistration(false); return true; }
        // Preserve Escape as a sign-out request without assigning Form.CancelButton.
        if (keyData == Keys.Escape && _pendingLogin is not null)
        {
            RequestRequiredPasswordSignOut();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void CancelRequiredPassword()
    {
        if (_busy) return;
        _auth.Logout();
        _pendingLogin = null;
        Result = null;
        ClearChangeFields();
        _changeView.Visible = false;
        _loginView.Visible = true;
        _loginView.BringToFront();
        AcceptButton = _signIn;
        CancelButton = null;
        _password.Clear();
        _username.FocusInput();
    }

    private async Task SaveRequiredPasswordAsync()
    {
        if (_busy || _pendingLogin?.Session is not { } account) return;
        bool valid = true;
        for (int i = 0; i < 3; i++)
        {
            bool missing = string.IsNullOrEmpty(_changeFields[i].Text);
            ChangeValidation(i, missing ? "Required" : "");
            valid &= !missing;
        }
        if (!valid) return;
        if (_changeFields[1].Text != _changeFields[2].Text)
        {
            ChangeValidation(2, "Does not match");
            return;
        }
        _busy = true;
        _changeSave.Enabled = _changeCancel.Enabled = false;
        foreach (var field in _changeFields) field.Enabled = false;
        _changeSave.Text = "Saving…";
        try
        {
            var result = await _auth.ChangePasswordAsync(account.UserId, _changeFields[0].Text, _changeFields[1].Text);
            if (result.Success)
            {
                // Reauthenticate with the new password to obtain a fresh, unrestricted result.
                var fresh = await _auth.LoginAsync(account.Username, _changeFields[1].Text);
                if (fresh.Success && fresh.Session is not null && !fresh.MustChangePassword)
                {
                    Result = fresh;
                    _pendingLogin = null;
                    ClearChangeFields();
                    _busy = false;
                    DialogResult = DialogResult.OK;
                    return;
                }
                _auth.Logout();
                _busy = false;
                CancelRequiredPassword();
                CatalogActionFeedback.Notify(this, "Password saved. Please sign in again.");
                return;
            }
            _changeError = true;
            _changeStatus.ForeColor = Theme.Colors.Danger;
            _changeStatus.Text = result.Error ?? "Unable to change password.";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            _auth.Logout();
            _changeError = true;
            _changeStatus.ForeColor = Theme.Colors.Danger;
            _changeStatus.Text = "Unable to complete password change. Please try again or sign out.";
        }
        finally
        {
            _busy = false;
            if (!IsDisposed && !Disposing)
            {
                _changeSave.Enabled = _changeCancel.Enabled = true;
                _changeSave.Text = "Save Password";
                foreach (var field in _changeFields) field.Enabled = true;
            }
        }
    }

    private void BuildRecoveryFields()
    {
        IconChar[] icons = { IconChar.UserAlt, IconChar.Key, IconChar.Lock, IconChar.Lock };
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            _recoveryLabels[i] = new Label { Text = _recoveryNames[i], Font = Theme.Body };
            _recoveryFields[i] = new ThemedTextField
            {
                TabIndex = i,
                MaxLength = i == 0 ? 50 : i == 1 ? 16 : 72,
                LeadingIcon = icons[i],
                ShowPasswordToggle = i > 0
            };
            _recoveryView.Controls.Add(_recoveryLabels[i]);
            _recoveryView.Controls.Add(_recoveryFields[i]);
            _recoveryFields[i].TextChanged += (_, _) => RecoveryValidation(index, "");
        }
        _reset.TabIndex = 4;
        _back.TabIndex = 5;
        RecoveryStatus("Your recovery passkey can only be used once.");
    }

    private void SetRecoveryMode(bool unlock)
    {
        if (_busy) return;
        _unlockMode = unlock;
        _recoveryTitle.Text = unlock ? "Unlock Account" : "Reset Password";
        _recoveryDescription.Text = unlock ? "Use your passkey to unlock login without changing your password." : "Use your recovery passkey to reset your password.";
        _recoveryToggle.Text = unlock ? "Reset password instead" : "Unlock instead";
        _reset.Text = unlock ? "Unlock Account" : "Reset Password";
        for (int i = 2; i < 4; i++)
        {
            _recoveryFields[i].Text = "";
            RecoveryValidation(i, "");
            _recoveryFields[i].Visible = _recoveryLabels[i].Visible = !unlock;
        }
        RecoveryStatus("Your recovery passkey can only be used once.");
        LayoutPanels();
    }

    private void ClearLockout()
    {
        _lockoutTimer.Stop();
        _lockoutClock.Reset();
        _lockoutSeconds = 0;
        _subtitle.Text = "Sign in to LiteraHub.";
        _subtitle.ForeColor = Theme.Colors.TextMuted;
        if (!_busy) { _signIn.Enabled = true; _signIn.Text = "Sign In"; }
    }

    private void UpdateLockout()
    {
        int remaining = Math.Max(0, _lockoutSeconds - (int)_lockoutClock.Elapsed.TotalSeconds);
        if (remaining == 0) { ClearLockout(); return; }
        _subtitle.Text = "Login temporarily restricted. Recover account or wait.";
        _subtitle.ForeColor = Theme.Colors.Danger;
        _signIn.Enabled = false;
        _signIn.Text = $"Try again in {remaining / 60:00}:{remaining % 60:00}";
    }

    private void RecoveryValidation(int index, string message)
    {
        _recoveryFields[index].HasValidationError = message.Length > 0;
        _recoveryLabels[index].Text = _recoveryNames[index] + (message.Length > 0 ? " • " + message : "");
        _recoveryLabels[index].ForeColor = message.Length > 0 ? Theme.Colors.Danger : Theme.Colors.Text;
    }

    private void RecoveryStatus(string message, bool error = false)
    {
        _recoveryError = error;
        _recoveryStatus.Text = message;
        _recoveryStatus.ForeColor = error ? Theme.Colors.Danger : Theme.Colors.TextMuted;
    }

    private void ClearRecovery()
    {
        for (int i = 0; i < 4; i++)
        {
            _recoveryFields[i].Text = "";
            _recoveryFields[i].HidePassword();
            RecoveryValidation(i, "");
        }
        RecoveryStatus("Your recovery passkey can only be used once.");
    }

    private async Task ResetPasswordAsync()
    {
        if (_busy || !_recoveryMode || _slide.Enabled) return;
        bool valid = true;
        for (int i = 0; i < (_unlockMode ? 2 : 4); i++)
        {
            bool missing = string.IsNullOrWhiteSpace(_recoveryFields[i].Text);
            RecoveryValidation(i, missing ? "Required" : "");
            valid &= !missing;
        }
        if (!valid) return;
        if (!_unlockMode && _recoveryFields[2].Text != _recoveryFields[3].Text)
        {
            RecoveryValidation(3, "Does not match");
            return;
        }
        _busy = true;
        _reset.Enabled = _back.Enabled = _recoveryToggle.Enabled = false;
        _reset.Text = _unlockMode ? "Unlocking…" : "Resetting…";
        foreach (var field in _recoveryFields) field.Enabled = false;
        string username = _recoveryFields[0].Text;
        bool success = false;
        try
        {
            var result = _unlockMode
                ? await _recovery.UnlockAsync(username, _recoveryFields[1].Text)
                : await _recovery.ResetAsync(username, _recoveryFields[1].Text, _recoveryFields[2].Text);
            if (IsDisposed || Disposing) return;
            success = result.Success;
            if (!success) RecoveryStatus(result.Error ?? "Recovery unavailable.", true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            if (!IsDisposed && !Disposing) RecoveryStatus("Recovery unavailable. Please retry.", true);
        }
        finally
        {
            _busy = false;
            if (!IsDisposed && !Disposing)
            {
                _reset.Enabled = _back.Enabled = _recoveryToggle.Enabled = true;
                _reset.Text = _unlockMode ? "Unlock Account" : "Reset Password";
                foreach (var field in _recoveryFields) field.Enabled = true;
            }
        }
        if (success && !IsDisposed && !Disposing)
        {
            bool unlocked = _unlockMode;
            ClearLockout();
            _username.Text = username;
            _password.Clear();
            SwitchMode(false);
            CatalogActionFeedback.Notify(this, unlocked
                ? "Account unlocked. Sign in with your current password and generate a new passkey."
                : "Password reset. Generate a new passkey after signing in.");
        }
    }

    private void LoadLogo()
    {
        string file = Path.Combine(AppContext.BaseDirectory, "Assets", "Brand", "LiteraHub-Logo.png");
        if (!File.Exists(file)) return;
        try
        {
            using var image = Image.FromFile(file);
            _logoOriginal = new Bitmap(image);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException)
        {
            System.Diagnostics.Debug.WriteLine("Login brand logo unavailable: " + ex.Message);
        }
    }

    private void RetintLogo()
    {
        _brandLogo.Image = null;
        _logoThemed?.Dispose();
        _logoThemed = null;
        if (_logoOriginal is null) return;
        Color accent = Theme.Colors.Accent;
        var bitmap = new Bitmap(_logoOriginal.Width, _logoOriginal.Height);
        using (var g = Graphics.FromImage(bitmap))
        using (var attributes = new ImageAttributes())
        {
            var matrix = new ColorMatrix(new[]
            {
                new[] { accent.R / 255f, 0f, 0f, 0f, 0f },
                new[] { 0f, accent.G / 255f, 0f, 0f, 0f },
                new[] { 0f, 0f, accent.B / 255f, 0f, 0f },
                new[] { 0f, 0f, 0f, 1f, 0f },
                new[] { 0f, 0f, 0f, 0f, 1f }
            });
            attributes.SetColorMatrix(matrix);
            g.DrawImage(_logoOriginal, new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, attributes);
        }
        _logoThemed = bitmap;
        _brandLogo.Image = bitmap;
    }

    private void BeginDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || WindowState != FormWindowState.Normal) return;
        ReleaseCapture();
        SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
    }

    private void SwitchMode(bool recovery)
    {
        if (_registrationMode || _recoveryMode == recovery || _busy || _pendingLogin is not null || _slide.Enabled) return;
        _recoveryMode = recovery;
        _loginView.Visible = !recovery;
        _recoveryView.Visible = recovery;
        AcceptButton = recovery ? _reset : _signIn;
        if (recovery)
        {
            _password.Clear();
            _password.UseSystemPasswordChar = true;
            _eye.IconChar = IconChar.Eye;
            _eye.AccessibleName = "Show password";
            ClearUsernameValidation(); ClearPasswordValidation();
            ClearRecovery();
            SetRecoveryMode(_lockoutSeconds > 0);
            _recoveryFields[0].Text = _username.Text;
        }
        else ClearRecovery();
        _accountPanel.BringToFront();
        _windowControls.BringToFront();
        _from = _position;
        _to = recovery ? 0f : 1f;
        _elapsed = 0f;
        _slide.Start();

    }

    private void SwitchRegistration(bool registration)
    {
        if (_studentAccounts is null || _busy || _recoveryMode || _pendingLogin is not null || _slide.Enabled || _registrationMode == registration) return;
        _registrationMode = registration;
        _loginView.Visible = !registration;
        _registrationView.Visible = registration;
        _password.Clear(); _password.UseSystemPasswordChar = true;
        _eye.IconChar = IconChar.Eye; _eye.AccessibleName = "Show password";
        _registerStudent.Text = ""; _registerStatus.Text = ""; _registerStudent.HasValidationError = false;
        AcceptButton = registration ? _registerSubmit : _signIn;
        _accountPanel.BringToFront(); _windowControls.BringToFront();
        _from = _position; _to = registration ? 0f : 1f; _elapsed = 0f; _slide.Start();
    }

    private async Task SubmitRegistrationAsync()
    {
        if (_studentAccounts is null || !_registrationMode || _busy || _slide.Enabled) return;
        if (!StudentAccountPolicy.IsValidStudentNo(_registerStudent.Text))
        {
            _registerStudent.HasValidationError = true;
            _registerStatus.Text = "Enter a valid student ID."; _registerStatus.ForeColor = Theme.Colors.Danger;
            _registerStudent.FocusInput(); return;
        }
        _busy = true; _registerStudent.Enabled = _registerSubmit.Enabled = _registerBack.Enabled = false;
        _registerSubmit.Text = "Submitting…";
        try
        {
            string response = await _studentAccounts.SubmitAsync(_registerStudent.Text);
            if (IsDisposed || Disposing) return;
            _registerStudent.Text = "";
            _registerStatus.ForeColor = Theme.Colors.Text;
            _registerStatus.Text = response;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.GetType().Name);
            if (!IsDisposed) { _registerStatus.ForeColor = Theme.Colors.Danger; _registerStatus.Text = "Registration is temporarily unavailable. Please try again or visit the library."; }
        }
        finally
        {
            _busy = false;
            if (!IsDisposed) { _registerStudent.Enabled = _registerSubmit.Enabled = _registerBack.Enabled = true; _registerSubmit.Text = "Submit Request"; }
        }
    }

    private void LayoutPanels()
    {
        if (_layoutBusy || IsDisposed) return;
        _layoutBusy = true;
        try
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            int accountWidth = (int)Math.Round(w * 0.48);
            int illustrationWidth = w - accountWidth;
            int accountX = (int)Math.Round(_position * illustrationWidth);
            int illustrationX = (int)Math.Round((1f - _position) * accountWidth);
            _accountPanel.SetBounds(accountX, 0, accountWidth, h);
            _carousel.SetBounds(illustrationX, 0, illustrationWidth, h);
            _windowControls.Location = new Point(
                Math.Max(0, w - _windowControls.Width + 7), 4);
            // Only update the caption surface when the color actually changes.
            // This prevents a dark rectangle over the illustration when
            // Account Recovery moves the carousel to the right.
            Color captionSurface = _position < 0.5f
                ? Theme.Colors.AccentSoft
                : Theme.Colors.Surface;
            _windowControls.SetSurfaceColor(captionSurface);
            _windowControls.BringToFront();

            _loginView.Bounds = _accountPanel.ClientRectangle;
            _recoveryView.Bounds = _accountPanel.ClientRectangle;
            _registrationView.Bounds = _accountPanel.ClientRectangle;
            _changeView.Bounds = _accountPanel.ClientRectangle;
            int fw = Math.Min(365, Math.Max(260, accountWidth - 84));
            int fx = (accountWidth - fw) / 2;
            // The heading, fields, forgot link, error, action and footer share
            // measured anchors rather than separate arbitrary row heights.
            int startY = Math.Max(20, (h - 490) / 2);
            // Enlarge only the logo upward; preserve the title, fields and buttons.
            _brandLogo.SetBounds(fx + (fw - 94) / 2, startY - 20, 94, 94);
            _welcome.SetBounds(fx, startY + 78, fw, 50);
            _subtitle.SetBounds(fx, startY + 130, fw, 30);
            _usernameTitle.SetBounds(fx, startY + 185, fw, 22);
            _usernameHint.SetBounds(fx + fw / 2, startY + 185, fw / 2, 22);
            _username.SetBounds(fx, startY + 212, fw, 42);
            _passwordTitle.SetBounds(fx, startY + 278, fw, 22);
            _passwordHint.SetBounds(fx + fw / 2, startY + 278, fw / 2, 22);
            _passwordFrame.SetBounds(fx, startY + 305, fw, 42);
            _register.SetBounds(fx, startY + 352, fw / 2, 28);
            _forgot.SetBounds(fx + fw / 2, startY + 352, fw - fw / 2, 28);
            int regY = Math.Max(20, (h - 424) / 2);
            _registerTitle.SetBounds(fx, regY, fw, 48);
            _registerDescription.SetBounds(fx, regY + 58, fw, 88);
            _registerStudentLabel.SetBounds(fx, regY + 154, fw, 24);
            _registerStudent.SetBounds(fx, regY + 182, fw, 42);
            _registerStatus.SetBounds(fx, regY + 237, fw, 75);
            _registerSubmit.SetBounds(fx, regY + 324, fw, 44);
            _registerBack.SetBounds(fx, regY + 377, fw, 34);
            _signIn.SetBounds(fx, startY + 403, fw, 45);
            _footer.SetBounds(fx, h - 65, fw, 24);

            int ry = Math.Max(12, (h - 520) / 2);
            _recoveryTitle.SetBounds(fx, ry, fw, 46);
            _recoveryDescription.SetBounds(fx, ry + 46, fw, 38);
            for (int i = 0; i < 4; i++)
            {
                _recoveryLabels[i].SetBounds(fx, ry + 94 + i * 67, fw, 22);
                _recoveryFields[i].SetBounds(fx, ry + 118 + i * 67, fw, 40);
            }
            int statusY = ry + (_unlockMode ? 230 : 365);
            _recoveryStatus.SetBounds(fx, statusY, fw, 42);
            _reset.SetBounds(fx, statusY + 46, fw, 44);
            _recoveryToggle.SetBounds(fx, statusY + 94, fw, 27);
            _back.SetBounds(fx, statusY + 121, fw, 30);
            int errorHeight = string.IsNullOrWhiteSpace(_changeStatus.Text) ? 0 : 48;
            int cy = Math.Max(16, (h - (455 + errorHeight)) / 2);
            _changeTitle.SetBounds(fx, cy, fw, 48);
            _changeIntro.SetBounds(fx, cy + 52, fw, 44);
            for (int i = 0; i < 3; i++)
            {
                _changeLabels[i].SetBounds(fx, cy + 112 + i * 72, fw, 22);
                _changeFields[i].SetBounds(fx, cy + 136 + i * 72, fw, 40);
            }
            _changePolicy.SetBounds(fx, cy + 328, fw, 36);
            _changeStatus.Visible = errorHeight > 0;
            _changeStatus.SetBounds(fx, cy + 364, fw, errorHeight);
            _changeSave.SetBounds(fx, cy + 372 + errorHeight, fw, 45);
            _changeCancel.SetBounds(fx, cy + 425 + errorHeight, fw, 30);
            LayoutPasswordInput();
        }
        finally { _layoutBusy = false; }
    }

    private void LayoutPasswordInput()
    {
        int width = _passwordFrame.Width;
        int height = _passwordFrame.Height;
        _eye.SetBounds(Math.Max(0, width - 44), 2, 40, Math.Max(1, height - 4));
        _passwordLeadingIcon.SetBounds(9, 6, 28, Math.Max(1, height - 10));
        _password.SetBounds(43, Math.Max(0, (height - _password.PreferredHeight) / 2),
            Math.Max(10, width - 95), _password.PreferredHeight);
    }

    private async Task SignInAsync()
    {
        if (_busy || _lockoutSeconds > 0 || _recoveryMode || _registrationMode || _pendingLogin is not null || _slide.Enabled) return;
        bool missingUser = string.IsNullOrWhiteSpace(_username.Text);
        bool missingPassword = string.IsNullOrEmpty(_password.Text);
        if (missingUser || missingPassword)
        {
            ShowValidation("Required", missingUser, missingPassword);
            if (missingUser) _username.FocusInput(); else _password.Focus();
            return;
        }
        SetBusy(true);
        try
        {
            AuthResult result = await _auth.LoginAsync(_username.Text, _password.Text);
            if (IsDisposed || Disposing) return;
            if (result.Success)
            {
                if (result.MustChangePassword)
                {
                    ShowChangeView(result);
                    return;
                }
                Result = result;
                DialogResult = DialogResult.OK;
                return;
            }
            if (result.RetryAfterSeconds > 0)
            {
                _password.Clear();
                _lockoutSeconds = result.RetryAfterSeconds;
                _lockoutClock.Restart();
                _lockoutTimer.Start();
                UpdateLockout();
            }
            else ShowValidation(result.Message, true, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Login failed: " + ex);
            if (!IsDisposed && !Disposing) ShowValidation("Sign-in is unavailable. Please try again.", true, true);
        }
        finally
        {
            if (!IsDisposed && !Disposing) SetBusy(false);
        }
    }

    private void ShowValidation(string message, bool badUser, bool badPassword)
    {
        _password.Clear();
        _subtitle.Text = message == "Required" ? "Enter your username and password." : message;
        _subtitle.ForeColor = Theme.Colors.Danger;
        _usernameInvalid = badUser;
        _passwordInvalid = badPassword;
        _username.HasValidationError = badUser;
        _usernameHint.Text = badUser ? (message == "Required" ? "Required" : "Invalid credentials") : "";
        _passwordHint.Text = badPassword ? (message == "Required" ? "Required" : "Invalid credentials") : "";
        _usernameHint.Visible = false;
        _passwordHint.Visible = false;
        UpdateLabels();
        _passwordFrame.Invalidate();
    }

    private void ClearUsernameValidation()
    {
        if (!_usernameInvalid) return;
        _usernameInvalid = false;
        _username.HasValidationError = false;
        _usernameHint.Visible = false;
        UpdateLabels();
    }

    private void ClearPasswordValidation()
    {
        if (!_passwordInvalid) return;
        _passwordInvalid = false;
        _passwordHint.Visible = false;
        UpdateLabels();
        _passwordFrame.Invalidate();
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _username.Enabled = !busy;
        _password.Enabled = !busy;
        _eye.Enabled = !busy;
        _forgot.Enabled = !busy;
        _register.Enabled = !busy;
        _signIn.Enabled = !busy;
        _signIn.Text = busy ? "Signing in…" : "Sign In";
        if (!busy && _lockoutSeconds > 0) UpdateLockout();
        UseWaitCursor = busy;
    }

    private void PaintPasswordFrame(object? sender, PaintEventArgs e)
    {
        Color stroke = _passwordInvalid ? Theme.Colors.Danger : _password.Focused ? Theme.Colors.Accent : Theme.Colors.Border;
        using var pen = new Pen(stroke, _passwordInvalid ? 2 : 1);
        e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, _passwordFrame.Width - 1), Math.Max(0, _passwordFrame.Height - 1));
    }

    private void UpdateLabels()
    {
        _usernameTitle.Text = _usernameInvalid ? "Username or Student ID • " + _usernameHint.Text : "Username or Student ID";
        _passwordTitle.Text = _passwordInvalid ? "Password • " + _passwordHint.Text : "Password";
        _usernameTitle.ForeColor = _usernameInvalid ? Theme.Colors.Danger : Theme.Colors.Text;
        _passwordTitle.ForeColor = _passwordInvalid ? Theme.Colors.Danger : Theme.Colors.Text;
        _passwordLeadingIcon.IconColor = _passwordInvalid ? Theme.Colors.Danger : Theme.Colors.TextMuted;
        _eye.IconColor = _passwordInvalid ? Theme.Colors.Danger : Theme.Colors.TextMuted;
    }

    private void ThemeChanged(object? sender, EventArgs e)
    {
        if (!IsDisposed && !Disposing) ApplyTheme();
    }

    private void ApplyTheme()
    {
        var c = Theme.Colors;
        BackColor = c.Surface;
        _accountPanel.BackColor = c.Surface;
        _loginView.BackColor = c.Surface;
        _recoveryView.BackColor = c.Surface;
        _registrationView.BackColor = c.Surface;
        _registerTitle.Font = Theme.Title; _registerTitle.ForeColor = c.Text;
        _registerDescription.Font = Theme.Body; _registerDescription.ForeColor = c.TextMuted;
        _registerStudentLabel.Font = Theme.Body; _registerStudentLabel.ForeColor = c.Text;
        _registerStatus.Font = Theme.Body; _registerStatus.ForeColor = _registerStudent.HasValidationError ? c.Danger : c.Text;
        _registerStudent.BackColor = c.Surface;
        foreach (var link in new[] { _register, _registerBack })
        {
            link.Font = Theme.Caption; link.BackColor = c.Surface; link.ForeColor = c.Accent;
            link.FlatAppearance.BorderSize = 0; link.FlatAppearance.MouseOverBackColor = c.Surface; link.FlatAppearance.MouseDownBackColor = c.Surface;
        }
        _register.TextAlign = ContentAlignment.MiddleLeft;
        _registerSubmit.Font = Theme.BodyStrong; _registerSubmit.BackColor = c.Accent; _registerSubmit.ForeColor = c.OnAccent;
        _registerSubmit.FlatAppearance.BorderSize = 0;
        _changeView.BackColor = c.Surface;
        _changeTitle.ForeColor = c.Text;
        _changeIntro.ForeColor = c.TextMuted;
        _changePolicy.ForeColor = c.TextMuted;
        _changeStatus.ForeColor = _changeError ? c.Danger : c.TextMuted;
        _changeSave.BackColor = c.Accent;
        _changeSave.ForeColor = c.OnAccent;
        _changeSave.FlatAppearance.BorderSize = 0;
        _changeSave.FlatAppearance.MouseOverBackColor = ControlPaint.Light(c.Accent, .08f);
        _changeCancel.BackColor = c.Surface;
        _changeCancel.ForeColor = c.Accent;
        _changeCancel.FlatAppearance.BorderSize = 0;
        _changeCancel.FlatAppearance.MouseOverBackColor = c.Surface;
        _changeCancel.FlatAppearance.MouseDownBackColor = c.Surface;
        for (int i = 0; i < 3; i++)
        {
            _changeFields[i].BackColor = c.Surface;
            _changeLabels[i].ForeColor = _changeFields[i].HasValidationError ? c.Danger : c.Text;
        }
        _welcome.ForeColor = c.Text;
        RetintLogo();
        _subtitle.ForeColor = _lockoutSeconds > 0 || _usernameInvalid || _passwordInvalid ? c.Danger : c.TextMuted;
        _recoveryTitle.ForeColor = c.Text;
        _recoveryDescription.ForeColor = c.TextMuted;
        _username.BackColor = c.Surface;
        _passwordFrame.BackColor = c.Surface;
        _password.BackColor = c.Surface;
        _password.ForeColor = c.Text;
        _passwordLeadingIcon.BackColor = c.Surface;
        _passwordLeadingIcon.IconColor = c.TextMuted;
        _eye.BackColor = c.Surface;
        _eye.IconColor = c.TextMuted;
        _eye.FlatAppearance.BorderSize = 0;
        _eye.FlatAppearance.MouseOverBackColor = c.Surface;
        _eye.FlatAppearance.MouseDownBackColor = c.Surface;
        _forgot.ForeColor = c.Accent;
        _forgot.BackColor = c.Surface;
        _forgot.TextAlign = ContentAlignment.MiddleRight;
        _forgot.FlatAppearance.BorderSize = 0;
        _forgot.FlatAppearance.MouseOverBackColor = c.Surface;
        _forgot.FlatAppearance.MouseDownBackColor = c.Surface;
        _usernameHint.ForeColor = c.Danger;
        _passwordHint.ForeColor = c.Danger;
        _signIn.BackColor = c.Accent;
        _signIn.ForeColor = c.OnAccent;
        _signIn.FlatAppearance.BorderSize = 0;
        _signIn.FlatAppearance.MouseOverBackColor = c.Accent;
        _recoveryToggle.BackColor = c.Surface;
        _recoveryToggle.ForeColor = c.Accent;
        _recoveryToggle.Font = Theme.Caption;
        _recoveryToggle.FlatAppearance.BorderSize = 0;
        _recoveryToggle.FlatAppearance.MouseOverBackColor = c.Surface;
        _back.BackColor = c.Surface;
        _back.ForeColor = c.Accent;
        _back.FlatAppearance.BorderSize = 0;
        _back.FlatAppearance.MouseOverBackColor = c.Surface;
        _back.FlatAppearance.MouseDownBackColor = c.Surface;
        _reset.BackColor = c.Accent;
        _reset.ForeColor = c.OnAccent;
        _reset.FlatAppearance.BorderSize = 0;
        _reset.FlatAppearance.MouseOverBackColor = c.Accent;
        _reset.Font = Theme.BodyStrong;
        _recoveryStatus.Font = Theme.Caption;
        _recoveryStatus.ForeColor = _recoveryError ? c.Danger : c.TextMuted;
        for (int i = 0; i < 4; i++)
        {
            _recoveryLabels[i].ForeColor = _recoveryFields[i].HasValidationError ? c.Danger : c.Text;
            _recoveryFields[i].BackColor = c.Surface;
        }
        _footer.ForeColor = c.TextMuted;
        _windowControls.SetSurfaceColor(
            _position < 0.5f ? c.AccentSoft : c.Surface);
        UpdateLabels();
        _passwordFrame.Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= ThemeChanged;
            _slide.Stop();
            _slide.Dispose();
            _lockoutTimer.Stop();
            _lockoutTimer.Dispose();
            _logoThemed?.Dispose();
            _logoOriginal?.Dispose();
        }
        base.Dispose(disposing);
    }
}

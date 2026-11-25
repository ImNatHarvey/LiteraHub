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
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

/// <summary>
/// Two flush themed panels. The carousel starts on the left and Sign In on
/// the right. Forgot Password swaps their locations without exposing an
/// unfinished password-reset operation.
/// </summary>
public sealed class LoginForm : Form
{
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    private readonly IAuthService _auth;
    private readonly Panel _accountPanel = new();
    private readonly LoginCarouselControl _carousel = new();
    private readonly ThemedWindowControls _windowControls = new() { ShowMaximizeButton = false, ShowMinimizeButton = false };
    private readonly Panel _loginView = new();
    private readonly Panel _recoveryView = new();
    private readonly PictureBox _brandLogo = new() { SizeMode = PictureBoxSizeMode.Zoom, TabStop = false };
    private readonly Label _welcome = new() { Text = "LiteraHub", TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _subtitle = new() { Text = "Sign in to manage your library.", TextAlign = ContentAlignment.MiddleCenter };
    private Image? _logoOriginal;
    private Image? _logoThemed;
    private readonly Label _usernameTitle = new() { Text = "Username" };
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
    private readonly Button _forgot = new() { Text = "Forgot password?", Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };

    private readonly Button _signIn = new() { Text = "Sign In", Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
    private readonly Label _footer = new() { Text = "LiteraHub  •  STI Balagtas", TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _recoveryTitle = new() { Text = "Account recovery", TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _recoveryDescription = new()
    {
        Text = "Offline recovery passkeys are coming with User Management. Until then, please contact your system administrator.",
        TextAlign = ContentAlignment.MiddleCenter
    };
    private readonly Button _back = new() { Text = "Back to Sign In", Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
    private readonly System.Windows.Forms.Timer _slide = new() { Interval = 15 };

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

    public LoginForm(IAuthService auth)
    {
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
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
            _passwordTitle, _passwordHint, _passwordFrame, _forgot, _signIn, _footer
        });
        _passwordFrame.Controls.Add(_password);
        _passwordFrame.Controls.Add(_passwordLeadingIcon);
        _passwordFrame.Controls.Add(_eye);
        _recoveryView.Controls.AddRange(new Control[] { _recoveryTitle, _recoveryDescription, _back });
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
        _username.TextChanged += (_, _) => ClearUsernameValidation();
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
        _back.Click += (_, _) => SwitchMode(false);
        _slide.Tick += (_, _) =>
        {
            _elapsed = Math.Min(1f, _elapsed + 0.065f);
            // Smoothstep avoids an abrupt stop at the end of the slide.
            float t = _elapsed * _elapsed * (3f - 2f * _elapsed);
            _position = _from + (_to - _from) * t;
            LayoutPanels();
            if (_elapsed >= 1f) _slide.Stop();
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
        if (_recoveryMode == recovery || _busy) return;
        _recoveryMode = recovery;
        _loginView.Visible = !recovery;
        _recoveryView.Visible = recovery;
        AcceptButton = recovery ? _back : _signIn;
        _from = _position;
        _to = recovery ? 0f : 1f;
        _elapsed = 0f;
        _slide.Start();
        if (!recovery) BeginInvoke(new Action(() => { if (!IsDisposed) _username.FocusInput(); }));
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
            int fw = Math.Min(365, Math.Max(260, accountWidth - 84));
            int fx = (accountWidth - fw) / 2;
            // The heading, fields, forgot link, error, action and footer share
            // measured anchors rather than separate arbitrary row heights.
            int startY = Math.Max(20, (h - 490) / 2);
            // Enlarge only the logo upward; preserve the title, fields and buttons.
            _brandLogo.SetBounds(fx + (fw - 94) / 2, startY - 20, 94, 94);
            _welcome.SetBounds(fx, startY + 78, fw, 50);
            _subtitle.SetBounds(fx, startY + 130, fw, 30);
            _usernameTitle.SetBounds(fx, startY + 185, fw / 2, 22);
            _usernameHint.SetBounds(fx + fw / 2, startY + 185, fw / 2, 22);
            _username.SetBounds(fx, startY + 212, fw, 42);
            _passwordTitle.SetBounds(fx, startY + 278, fw / 2, 22);
            _passwordHint.SetBounds(fx + fw / 2, startY + 278, fw / 2, 22);
            _passwordFrame.SetBounds(fx, startY + 305, fw, 42);
            _forgot.SetBounds(fx + fw - 170, startY + 352, 170, 25);
            _signIn.SetBounds(fx, startY + 403, fw, 45);
            _footer.SetBounds(fx, h - 65, fw, 24);

            _recoveryTitle.SetBounds(fx, h / 2 - 142, fw, 54);
            _recoveryDescription.SetBounds(fx, h / 2 - 76, fw, 105);
            _back.SetBounds(fx, h / 2 + 56, fw, 44);
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
        if (_busy || _recoveryMode) return;
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
                Result = result;
                DialogResult = DialogResult.OK;
                return;
            }
            ShowValidation(result.Message, true, true);
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
        _usernameInvalid = badUser;
        _passwordInvalid = badPassword;
        _username.HasValidationError = badUser;
        _usernameHint.Text = badUser ? (message == "Required" ? "Required" : "Invalid credentials") : "";
        _passwordHint.Text = badPassword ? (message == "Required" ? "Required" : "Invalid credentials") : "";
        _usernameHint.Visible = badUser;
        _passwordHint.Visible = badPassword;
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
        _signIn.Enabled = !busy;
        _signIn.Text = busy ? "Signing in…" : "Sign In";
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
        _welcome.ForeColor = c.Text;
        RetintLogo();
        _subtitle.ForeColor = c.TextMuted;
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
        _back.BackColor = c.Accent;
        _back.ForeColor = c.OnAccent;
        _back.FlatAppearance.BorderSize = 0;
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
            _logoThemed?.Dispose();
            _logoOriginal?.Dispose();
        }
        base.Dispose(disposing);
    }
}

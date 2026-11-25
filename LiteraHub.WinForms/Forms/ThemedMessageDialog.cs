
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;
using LiteraHub.WinForms.Controls;
using FontAwesome.Sharp;

namespace LiteraHub.WinForms.Forms;

public sealed class ThemedMessageDialog : Form
{
    private readonly Label _titleLabel;
    private readonly Label _messageLabel;
    private readonly Label _symbol;
    private readonly Button _confirm;
    private readonly Button _cancel;

    private readonly ThemedWindowControls _window = new() { ShowMinimizeButton = false, ShowMaximizeButton = false };
    private IconButton? _copy;
    private Label? _secret;
    private readonly Panel _accentStripe = new() { Dock = DockStyle.Left, Width = 4, TabStop = false };

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private void BeginDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(Handle, 0x00A1, (IntPtr)2, IntPtr.Zero);
    }
    private readonly bool _danger;
    private readonly bool _confirmation;

    private ThemedMessageDialog(
        string title,
        string message,
        bool confirmation,
        bool danger,
        string confirmText, string? temporaryPassword = null)
    {
        _danger = danger;
        _confirmation = confirmation;

        Text = title;
        Font = Theme.Body;

        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;

        ClientSize = new Size(480, 230);
        MinimumSize = new Size(390, 230);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = new Padding(24, 18, 24, 18)
        };

        root.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 52));

        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 48));

        _titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Theme.Title,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = new Padding(0, 12, 0, 0)
        };

        content.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 42));

        content.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        content.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _symbol = new Label
        {
            Text = danger ? "!" : "?",
            Dock = DockStyle.Top,
            Height = 26,
            Font = Theme.Title,
            TextAlign = ContentAlignment.TopCenter,
            Margin = new Padding(0, 0, 10, 0)
        };

        _messageLabel = new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Font = Theme.Body,
            AutoEllipsis = false,
            Margin = Padding.Empty,
            Padding = new Padding(0, 2, 0, 0)
        };

        content.Controls.Add(_symbol, 0, 0);
        content.Controls.Add(_messageLabel, 1, 0);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        _confirm = new Button
        {
            Text = confirmText,
            Size = new Size(120, 38),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.OK,
            Margin = new Padding(8, 4, 0, 0)
        };

        _cancel = new Button
        {
            Text = "Cancel",
            Size = new Size(105, 38),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(0, 4, 0, 0),
            Visible = confirmation
        };

        footer.Controls.Add(_confirm);

        if (confirmation)
            footer.Controls.Add(_cancel);

        var heading = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        heading.Controls.Add(_titleLabel);
        heading.MouseDown += BeginDrag;
        _titleLabel.MouseDown += BeginDrag;
        _titleLabel.Padding = new Padding(0, 0, 48, 0);
        _window.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        heading.Controls.Add(_window);
        heading.Resize += (_, _) => _window.Location = new Point(Math.Max(0, heading.Width - _window.Width), 0);
        _window.Location = new Point(Math.Max(0, heading.Width - _window.Width), 0);
        _window.Bind(this);
        _window.BringToFront();
        root.Controls.Add(heading, 0, 0);
        root.Controls.Add(content, 0, 1);
        root.Controls.Add(footer, 0, 2);

        if (temporaryPassword is not null)
        {
            content.RowCount = 2;
            content.RowStyles.Clear();
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            var credential = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            credential.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
            credential.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            // Match the non-interactive read-only display fields in the details pages.
            _secret = new Label
            {
                Text = temporaryPassword,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TabStop = false,
                Margin = new Padding(4, 2, 0, 4),
                Padding = new Padding(12, 3, 12, 3),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                BorderStyle = BorderStyle.None,
                AccessibleName = "Temporary password",
                Cursor = Cursors.Default
            };
            _secret.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Colors.Border);
                if (_secret.Width > 0 && _secret.Height > 0)
                    e.Graphics.DrawRectangle(pen, 0, 0, _secret.Width - 1, _secret.Height - 1);
            };
            _copy = new IconButton { IconChar = IconChar.Copy, IconSize = 18, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, AccessibleName = "Copy temporary password", Margin = new Padding(0, 2, 0, 4) };
            _copy.Click += (_, _) => { try { Clipboard.SetText(temporaryPassword); _copy.IconChar = IconChar.Check; _copy.AccessibleName = "Password copied"; } catch (ExternalException) { _copy.AccessibleName = "Clipboard unavailable; click to retry"; } };
            credential.Controls.Add(_copy, 0, 0);
            credential.Controls.Add(_secret, 1, 0);
            content.Controls.Add(credential, 1, 1);
        }
        Controls.Add(root);
        Controls.Add(_accentStripe);
        _accentStripe.BringToFront();


        // Expand the dialog to fit longer messages.
        // Keeps short confirmation dialogs at their original size.

        int messageWidth = ClientSize.Width
            - _accentStripe.Width
            - root.Padding.Horizontal
            - (int)content.ColumnStyles[0].Width
            - 12;


        Size measured = TextRenderer.MeasureText(
            message,
            Theme.Body,
            new Size(Math.Max(200, messageWidth), int.MaxValue),
            TextFormatFlags.WordBreak |
            TextFormatFlags.NoPrefix);

        // Allow room for the header, footer, paddings,
        // and extra space between wrapped text lines.
        int requiredHeight = measured.Height + 170 + (temporaryPassword is null ? 0 : 46);

        int maxHeight = Math.Max(
            230,
            Screen.FromControl(this).WorkingArea.Height - 100);

        ClientSize = new Size(
            ClientSize.Width,
            Math.Clamp(requiredHeight, 230, maxHeight));



        // Destructive confirmations must not be accepted
        // accidentally by pressing Enter.
        AcceptButton = (confirmation && danger) || temporaryPassword is not null
            ? null
            : _confirm;

        CancelButton = confirmation
            ? _cancel
            : _confirm;

        ApplyTheme();

        Theme.Changed += HandleThemeChanged;
        Shown += (_, _) => { _window.Location = new Point(Math.Max(0, heading.Width - _window.Width), 0); if (_copy is not null) ActiveControl = _copy; };
    }

    public static bool Confirm(
        IWin32Window? owner,
        string title,
        string message,
        bool danger = false,
        string confirmText = "Confirm")
    {
        using var dialog = new ThemedMessageDialog(
            title,
            message,
            true,
            danger,
            confirmText);

        return dialog.ShowDialog(owner) == DialogResult.OK;
    }

    public static void ShowAlert(
        IWin32Window? owner,
        string title,
        string message,
        bool danger = false)
    {
        using var dialog = new ThemedMessageDialog(
            title,
            message,
            false,
            danger,
            "OK");

        dialog.ShowDialog(owner);
    }

    public static void ShowTemporaryPassword(IWin32Window? owner, string username, string password)
    {
        using var dialog = new ThemedMessageDialog("Staff account created",
            $"Username: {username}\nTemporary password (copy below):\n\nGive it securely to the account owner. It will not be shown again. A password change is required at first sign-in.", false, false, "OK", password);
        dialog.ShowDialog(owner);
    }

    private void HandleThemeChanged(
        object? sender,
        EventArgs e)
    {
        if (!IsDisposed)
            ApplyTheme();
    }

    private void ApplyTheme()
    {
        var c = Theme.Colors;

        BackColor = c.Background;
        ForeColor = c.Text;
        _window.SetSurfaceColor(c.Background);
        _accentStripe.BackColor = c.Accent;
        if (_secret is not null)
        {
            _secret.BackColor = c.SurfaceAlt;
            _secret.ForeColor = c.Text;
            _secret.Font = Theme.BodyStrong;
            _secret.Invalidate();
        }
        if (_copy is not null) { _copy.BackColor = c.SurfaceAlt; _copy.ForeColor = c.Text; _copy.IconColor = c.Text; _copy.FlatAppearance.BorderColor = c.Border; _copy.FlatAppearance.MouseOverBackColor = c.AccentSoft; }

        _titleLabel.BackColor = c.Background;
        _titleLabel.ForeColor = c.Text;

        _messageLabel.BackColor = c.Background;
        _messageLabel.ForeColor = c.Text;

        // No colored square behind the symbol.
        _symbol.BackColor = c.Background;
        _symbol.ForeColor = _danger
            ? c.Danger
            : c.Accent;

        _confirm.BackColor = _danger
            ? c.Danger
            : c.Accent;

        _confirm.ForeColor = _danger
            ? Color.White
            : c.OnAccent;

        _confirm.FlatAppearance.BorderSize = 0;

        _cancel.BackColor = c.Surface;
        _cancel.ForeColor = c.Text;
        _cancel.FlatAppearance.BorderColor = c.Border;
        _cancel.FlatAppearance.BorderSize = 1;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Theme.Changed -= HandleThemeChanged;

        base.Dispose(disposing);
    }
}

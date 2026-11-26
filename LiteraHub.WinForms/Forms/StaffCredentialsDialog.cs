using System.Runtime.InteropServices;
using FontAwesome.Sharp;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class StaffCredentialsDialog : Form
{
    private StaffCredentialsDialog(string username, string? password, string passkey)
    {
        var c = Theme.Colors;
        Text = password is null ? "Recovery Passkey Issued" : "Staff Account Created";
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = MinimizeBox = false;
        ClientSize = new Size(520, password is null ? 320 : 396);
        BackColor = c.Background;
        Font = Theme.Body;
        var title = new Label { Text = Text, Font = Theme.Title, ForeColor = c.Text, Bounds = new Rectangle(24, 16, 430, 32) };
        Controls.Add(title);
        var hint = new Label { Text = "Securely provide these credentials to the account owner. Secrets will not be displayed again.", ForeColor = c.TextMuted, Bounds = new Rectangle(24, 58, 472, 48) };
        Controls.Add(hint);
        int y = 110;
        AddCredential("Username", username, y); y += 76;
        if (password is not null) { AddCredential("Temporary password", password, y); y += 76; }
        AddCredential("Recovery passkey", passkey, y);
        var ok = new Button { Text = "OK", Font = Theme.BodyStrong, FlatStyle = FlatStyle.Flat, BackColor = c.Accent, ForeColor = c.OnAccent, DialogResult = DialogResult.OK, Bounds = new Rectangle(376, ClientSize.Height - 62, 120, 40), Cursor = Cursors.Hand };
        ok.FlatAppearance.BorderSize = 0;
        Controls.Add(ok);
        CancelButton = ok;
        ThemedDialogChrome.Attach(this, title, addCloseButton: true);
        Shown += (_, _) => ok.Focus();
    }

    private void AddCredential(string caption, string value, int y)
    {
        var c = Theme.Colors;
        Controls.Add(new Label { Text = caption, ForeColor = c.Text, Bounds = new Rectangle(24, y, 472, 22) });
        var text = new Label { Text = value, UseMnemonic = false, Font = Theme.BodyStrong, BackColor = c.SurfaceAlt, ForeColor = c.Text, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), Bounds = new Rectangle(72, y + 24, 424, 40), AutoEllipsis = true };
        text.Paint += (_, e) => { using var pen = new Pen(Theme.Colors.Border); e.Graphics.DrawRectangle(pen, 0, 0, text.Width - 1, text.Height - 1); };
        var copy = new IconButton { IconChar = IconChar.Copy, IconSize = 18, IconColor = c.Text, BackColor = c.SurfaceAlt, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, AccessibleName = "Copy " + caption, Bounds = new Rectangle(24, y + 24, 40, 40) };
        copy.FlatAppearance.BorderColor = c.Border;
        copy.FlatAppearance.MouseOverBackColor = c.AccentSoft;
        copy.Click += (_, _) => { try { Clipboard.SetText(value); copy.IconChar = IconChar.Check; } catch (ExternalException) { ThemedMessageDialog.ShowAlert(this, "Clipboard unavailable", "Please try copying again.", true); } };
        Controls.Add(text); Controls.Add(copy);
    }

    public static void ShowCredentials(IWin32Window? owner, string username, string? password, string passkey)
    {
        using var dialog = new StaffCredentialsDialog(username, password, passkey);
        dialog.ShowDialog(owner);
    }
}

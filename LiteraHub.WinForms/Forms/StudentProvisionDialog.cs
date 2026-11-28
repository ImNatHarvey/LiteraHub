using LiteraHub.Core.Security;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class StudentProvisionDialog : Form
{
    private readonly ThemedTextField _username = new() { MaxLength = 50 };
    private readonly CheckBox _verified = new() { Text = "I verified the student's identity and enrollment in person.", AutoSize = false };
    private readonly Button _save = new() { Text = "Issue Credentials", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Enabled = false };
    public string Username => _username.Text.Trim();
    public StudentProvisionDialog(string studentNo, string fullName, bool approval)
    {
        var c = Theme.Colors;
        Text = approval ? "Approve Registration" : "Create Student Account"; StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(510, 340); BackColor = c.Background; Font = Theme.Body;
        var title = new Label { Text = Text, Font = Theme.Title, ForeColor = c.Text, Bounds = new Rectangle(24, 16, 420, 38) };
        var identity = new Label { Text = $"{fullName} · {studentNo}", UseMnemonic = false, ForeColor = c.Text, Bounds = new Rectangle(24, 60, 462, 44), AutoEllipsis = true };
        var hint = new Label { Text = "A temporary password and a recovery passkey will be generated once. Give them only to the verified student.", ForeColor = c.TextMuted, Bounds = new Rectangle(24, 106, 462, 52) };
        var label = new Label { Text = "Choose Username (or sign in using Student ID)", ForeColor = c.Text, Bounds = new Rectangle(24, 162, 462, 23) };
        _username.Bounds = new Rectangle(24, 188, 462, 40); _username.Text = "";
        _verified.ForeColor = c.Text; _verified.Bounds = new Rectangle(24, 236, 462, 38);
        _save.Bounds = new Rectangle(320, 283, 166, 38); _save.BackColor = c.Accent; _save.ForeColor = c.OnAccent; _save.FlatAppearance.BorderSize = 0;
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat, BackColor = c.Surface, ForeColor = c.Text, Bounds = new Rectangle(24, 283, 106, 38) }; cancel.FlatAppearance.BorderColor = c.Border;
        Controls.AddRange(new Control[] { title, identity, hint, label, _username, _verified, _save, cancel });
        ThemedDialogChrome.Attach(this, title, addCloseButton: true);
        _verified.CheckedChanged += (_, _) => UpdateSave(); _username.TextChanged += (_, _) => UpdateSave();
        _save.Click += (_, _) => { if (_verified.Checked && StudentAccountPolicy.IsValidUsername(Username) && !StudentAccountPolicy.IsReservedStudentIdentifier(Username)) { DialogResult = DialogResult.OK; Close(); } };
        AcceptButton = _save; CancelButton = cancel;
    }
    private void UpdateSave() => _save.Enabled = _verified.Checked && StudentAccountPolicy.IsValidUsername(Username) && !StudentAccountPolicy.IsReservedStudentIdentifier(Username);
}

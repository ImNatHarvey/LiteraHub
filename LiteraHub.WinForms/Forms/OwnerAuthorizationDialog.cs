using FontAwesome.Sharp;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class OwnerAuthorizationDialog : Form
{
    private readonly ThemedTextField _password = new() { ShowPasswordToggle = true, MaxLength = 72 };
    private string? _approvedPassword;
    public OwnerAuthorizationDialog(string target, string action)
    {
        var c = Theme.Colors;
        Text = "System Owner Authorization";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 320);
        ShowInTaskbar = false; BackColor = c.Background;
        var title = new Label { Text = Text, Font = Theme.Title, ForeColor = c.Text, Bounds = new Rectangle(24, 16, 440, 32) };
        Controls.Add(title);
        ThemedDialogChrome.Attach(this, title, addCloseButton: true);
        var hint = new Label { Text = "The System Owner must personally approve this operation.\nTarget: " + target + "\nAction: " + action, Font = Theme.Body, ForeColor = c.TextMuted, Bounds = new Rectangle(24, 58, 472, 82) };
        var label = new Label { Text = "System Owner password", Font = Theme.Body, ForeColor = c.Text, Bounds = new Rectangle(24, 150, 472, 22) };
        _password.SetBounds(24, 176, 472, 40);
        var cancel = new Button { Text = "Cancel", Bounds = new Rectangle(24, 256, 112, 40), FlatStyle = FlatStyle.Flat, BackColor = c.Surface, ForeColor = c.Text, Font = Theme.BodyStrong };
        cancel.FlatAppearance.BorderColor = c.Border;
        var approve = new CenteredIconButton { Text = "Authorize", IconChar = IconChar.Lock, IconSize = 18, Bounds = new Rectangle(304, 256, 192, 40), FlatStyle = FlatStyle.Flat, BackColor = c.Accent, ForeColor = c.OnAccent, IconColor = c.OnAccent, Font = Theme.BodyStrong };
        approve.FlatAppearance.BorderSize = 0;
        Controls.AddRange(new Control[] { hint, label, _password, cancel, approve });
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        _password.TextChanged += (_, _) => { _password.HasValidationError = false; label.Text = "System Owner password"; label.ForeColor = c.Text; };
        approve.Click += (_, _) => {
            if (string.IsNullOrEmpty(_password.Text)) { _password.HasValidationError = true; label.Text = "System Owner password • Required"; label.ForeColor = c.Danger; _password.FocusInput(); return; }
            _approvedPassword = _password.Text; _password.Text = ""; DialogResult = DialogResult.OK; Close();
        };
        CancelButton = cancel; Shown += (_, _) => _password.FocusInput();
    }
    public string TakePassword()
    { var value = _approvedPassword ?? ""; _approvedPassword = null; return value; }
    protected override void Dispose(bool disposing)
    { if (disposing) _approvedPassword = null; base.Dispose(disposing); }
}

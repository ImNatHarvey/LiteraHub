using FontAwesome.Sharp;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class ChangeNameDialog : Form
{
    private readonly IProfileService _service;
    private readonly ThemedTextField[] _fields = Enumerable.Range(0, 4).Select(i => new ThemedTextField { MaxLength = i == 3 ? 72 : 100, ShowPasswordToggle = i == 3 }).ToArray();
    private readonly Label[] _labels = new Label[4];
    private readonly string[] _names = { "First name", "Middle name (optional)", "Last name", "Current password" };
    private readonly CenteredIconButton _save = new() { Text = "Save Name", IconChar = IconChar.UserAlt, IconSize = 18 };
    private readonly Button _cancel = new() { Text = "Cancel" };
    private bool _busy;
    public ChangeNameDialog(IProfileService service, ProfileInfo info)
    {
        _service = service;
        var c = Theme.Colors;
        FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false; ClientSize = new Size(480, 440); BackColor = c.Background;
        var title = new Label { Text = "Edit Name", Font = Theme.Title, ForeColor = c.Text, Bounds = new Rectangle(24, 16, 350, 32) };
        Controls.Add(title); ThemedDialogChrome.Attach(this, title);
        var close = new ThemedWindowControls { ShowMinimizeButton = false, ShowMaximizeButton = false };
        close.Bind(this); close.SetSurfaceColor(c.Background); close.Location = new Point(ClientSize.Width - close.Width - 12, 10); Controls.Add(close);
        _fields[0].Text = info.FirstName ?? ""; _fields[1].Text = info.MiddleName ?? ""; _fields[2].Text = info.LastName ?? "";
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            _labels[i] = new Label { Text = _names[i], Font = Theme.Body, ForeColor = c.Text, Bounds = new Rectangle(24, 58 + 76 * i, 432, 22) };
            _fields[i].SetBounds(24, 82 + 76 * i, 432, 40);
            Controls.Add(_labels[i]); Controls.Add(_fields[i]);
            _fields[i].TextChanged += (_, _) => Error(index, "");
        }
        _cancel.SetBounds(24, 376, 112, 40); _save.SetBounds(284, 376, 172, 40);
        foreach (Button b in new Button[] { _save, _cancel }) { b.FlatStyle = FlatStyle.Flat; b.Font = Theme.BodyStrong; b.Cursor = Cursors.Hand; b.FlatAppearance.BorderColor = c.Border; b.FlatAppearance.BorderSize = 1; }
        _save.BackColor = c.Accent; _save.ForeColor = _save.IconColor = c.OnAccent; _save.FlatAppearance.BorderSize = 0;
        _save.FlatAppearance.MouseOverBackColor = ControlPaint.Light(c.Accent, .08f); _save.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(c.Accent, .08f);
        _cancel.BackColor = c.Surface; _cancel.ForeColor = c.Text; _cancel.FlatAppearance.MouseOverBackColor = c.SurfaceAlt; _cancel.FlatAppearance.MouseDownBackColor = c.AccentSoft;
        Controls.Add(_cancel); Controls.Add(_save);
        _cancel.Click += (_, _) => { if (!_busy) { DialogResult = DialogResult.Cancel; Close(); } };
        _save.Click += async (_, _) => await SaveAsync();
        CancelButton = _cancel; Shown += (_, _) => _fields[0].FocusInput();
        FormClosing += (_, e) => { if (_busy) e.Cancel = true; };
    }
    private void Error(int index, string error)
    {
        _fields[index].HasValidationError = error.Length > 0;
        _labels[index].Text = _names[index] + (error.Length > 0 ? " • " + error : "");
        _labels[index].ForeColor = error.Length > 0 ? Theme.Colors.Danger : Theme.Colors.Text;
    }
    private async Task SaveAsync()
    {
        if (_busy) return;
        bool valid = true;
        foreach (int i in new[] { 0, 2, 3 })
        {
            bool empty = i == 3 ? _fields[i].Text.Length == 0 : string.IsNullOrWhiteSpace(_fields[i].Text);
            Error(i, empty ? "Required" : ""); valid &= !empty;
        }
        if (!valid) return;
        _busy = true; _save.Enabled = _cancel.Enabled = false;
        foreach (var f in _fields) f.Enabled = false;
        try
        {
            var result = await _service.ChangeNameAsync(_fields[3].Text, _fields[0].Text, _fields[1].Text, _fields[2].Text);
            if (!result.Success) { if (result.Error?.StartsWith("Current password", StringComparison.Ordinal) == true) Error(3, "Incorrect or unavailable"); ThemedMessageDialog.ShowAlert(this, "Edit Name", result.Error ?? "Unable to save name.", true); return; }
            _fields[3].Text = ""; _busy = false; DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); ThemedMessageDialog.ShowAlert(this, "Edit Name", "Unable to save name.", true); }
        finally { _busy = false; if (!IsDisposed) { _save.Enabled = _cancel.Enabled = true; foreach (var f in _fields) f.Enabled = true; } }
    }
}

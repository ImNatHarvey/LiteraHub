using LiteraHub.Core.Interfaces;
using LiteraHub.Services.Security;

namespace LiteraHub.WinForms.Forms;

public sealed class ChangePasswordForm : Form
{
    private readonly IAuthService _auth;
    private readonly int _userId;
    private readonly TextBox _current = new() { UseSystemPasswordChar = true };
    private readonly TextBox _new = new() { UseSystemPasswordChar = true };
    private readonly TextBox _confirm = new() { UseSystemPasswordChar = true };
    private readonly Button _save = new() { Text = "Save password", AutoSize = true, Height = 36 };
    private readonly Button _cancel = new() { Text = "Sign out", AutoSize = true, Height = 36, DialogResult = DialogResult.Cancel };
    private readonly Label _error = new() { AutoSize = true, MaximumSize = new Size(300, 0), Visible = false };

    public ChangePasswordForm(IAuthService auth, int userId, string username)
    {
        _auth = auth;
        _userId = userId;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "LiteraHub - Change password";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(24);

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));

        var title = new Label
        {
            Text = "Set a new password",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 14F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };
        var intro = new Label
        {
            Text = $"Signed in as {username}. Choose a new password before continuing.\n" +
                   $"At least {PasswordPolicy.MinLength} characters, with a letter and a number.",
            AutoSize = true,
            MaximumSize = new Size(300, 0),
            Margin = new Padding(0, 0, 0, 16)
        };

        foreach (var box in new[] { _current, _new, _confirm })
        {
            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(0, 0, 0, 12);
        }

        _error.ForeColor = Color.Firebrick; // temporary: moves to Theme in Step 4
        _error.Margin = new Padding(0, 0, 0, 12);

        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0) };
        buttons.Controls.Add(_save);
        buttons.Controls.Add(_cancel);

        layout.Controls.Add(title);
        layout.Controls.Add(intro);
        layout.Controls.Add(new Label { Text = "Current password", AutoSize = true, Margin = new Padding(0, 0, 0, 4) });
        layout.Controls.Add(_current);
        layout.Controls.Add(new Label { Text = "New password", AutoSize = true, Margin = new Padding(0, 0, 0, 4) });
        layout.Controls.Add(_new);
        layout.Controls.Add(new Label { Text = "Confirm new password", AutoSize = true, Margin = new Padding(0, 0, 0, 4) });
        layout.Controls.Add(_confirm);
        layout.Controls.Add(_error);
        layout.Controls.Add(buttons);
        Controls.Add(layout);

        AcceptButton = _save;
        CancelButton = _cancel;
        ActiveControl = _current;
        _save.Click += async (_, _) => await SaveAsync();

        ResumeLayout(performLayout: true);
    }

    private async Task SaveAsync()
    {
        if (_new.Text != _confirm.Text)
        {
            ShowError("The new passwords do not match.");
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _auth.ChangePasswordAsync(_userId, _current.Text, _new.Text);
            if (result.Success)
            {
                DialogResult = DialogResult.OK;
                return;
            }

            ShowError(result.Error ?? "Could not change the password.");
        }
        catch (Exception ex)
        {
            ShowError("Could not change the password: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowError(string message)
    {
        _error.Text = message;
        _error.Visible = true;
    }

    private void SetBusy(bool busy)
    {
        _save.Enabled = !busy;
        _cancel.Enabled = !busy;
        _current.Enabled = !busy;
        _new.Enabled = !busy;
        _confirm.Enabled = !busy;
        UseWaitCursor = busy;
    }
}
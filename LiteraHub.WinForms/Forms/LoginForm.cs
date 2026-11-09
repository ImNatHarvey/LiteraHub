using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;

namespace LiteraHub.WinForms.Forms;

public sealed class LoginForm : Form
{
    private readonly IAuthService _auth;
    private readonly TextBox _username = new();
    private readonly TextBox _password = new() { UseSystemPasswordChar = true };
    private readonly Button _signIn = new() { Text = "Sign in", Dock = DockStyle.Fill, Height = 36 };
    private readonly Label _error = new() { AutoSize = true, MaximumSize = new Size(300, 0), Visible = false };

    public AuthResult? Result { get; private set; }

    public LoginForm(IAuthService auth)
    {
        _auth = auth;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "LiteraHub - Sign in";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(24);

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));

        var title = new Label
        {
            Text = "LiteraHub",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 16)
        };
        var userLabel = new Label { Text = "Username", AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
        var passLabel = new Label { Text = "Password", AutoSize = true, Margin = new Padding(0, 0, 0, 4) };

        _username.Dock = DockStyle.Fill;
        _username.Margin = new Padding(0, 0, 0, 12);
        _password.Dock = DockStyle.Fill;
        _password.Margin = new Padding(0, 0, 0, 12);
        _error.ForeColor = Color.Firebrick; // temporary: moves to Theme in Step 4
        _error.Margin = new Padding(0, 0, 0, 12);

        layout.Controls.Add(title);
        layout.Controls.Add(userLabel);
        layout.Controls.Add(_username);
        layout.Controls.Add(passLabel);
        layout.Controls.Add(_password);
        layout.Controls.Add(_error);
        layout.Controls.Add(_signIn);
        Controls.Add(layout);

        AcceptButton = _signIn;
        ActiveControl = _username;
        _signIn.Click += async (_, _) => await SignInAsync();

        ResumeLayout(performLayout: true);
    }

    private async Task SignInAsync()
    {
        SetBusy(true);
        try
        {
            var result = await _auth.LoginAsync(_username.Text, _password.Text);
            if (result.Success)
            {
                Result = result;
                DialogResult = DialogResult.OK;
                return;
            }

            ShowError(result.Message);
        }
        catch (Exception ex)
        {
            ShowError("Sign-in failed: " + ex.Message);
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
        _password.Clear();
        _password.Focus();
    }

    private void SetBusy(bool busy)
    {
        _signIn.Enabled = !busy;
        _username.Enabled = !busy;
        _password.Enabled = !busy;
        _signIn.Text = busy ? "Signing in..." : "Sign in";
        UseWaitCursor = busy;
    }
}